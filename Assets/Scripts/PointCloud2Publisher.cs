using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;

/// <summary>
/// Simulates a 3D LiDAR sensor by casting a grid of rays in Unity and
/// publishing the hit points as a ROS2 PointCloud2 message.
///
/// Rays are cast in a configurable horizontal × vertical FOV grid.
/// Each hit point is converted from Unity space to ROS FLU convention
/// (x = forward, y = left, z = up) before being serialised as raw bytes.
///
/// Two filters are applied before a point is included:
///   - minHeightFilter:    removes floor returns (points below the sensor plane).
///   - minDistanceFilter:  removes returns from the robot's own body.
///
/// Each point is stored as four consecutive FLOAT32 values (x, y, z, intensity)
/// in little-endian byte order, giving a fixed POINT_STEP of 16 bytes.
/// </summary>
public class RosPointCloud2Publisher : MonoBehaviour
{
    [Header("ROS Settings")]
    public string topicName = "/point_cloud";
    public string frameId   = "base_scan";

    [Header("Scan Settings")]
    [Tooltip("Number of rays cast horizontally per scan")]
    public int   numRaysHorizontal = 360;

    [Tooltip("Number of vertical layers (rings) per scan")]
    public int   numRaysVertical   = 16;

    [Tooltip("Total horizontal field of view in degrees")]
    public float horizontalFov     = 360f;

    [Tooltip("Lowest vertical angle in degrees (negative = below horizon)")]
    public float verticalFovMin    = -15f;

    [Tooltip("Highest vertical angle in degrees")]
    public float verticalFovMax    =  15f;

    [Tooltip("Maximum raycast range in metres")]
    public float maxDistance       = 10f;

    [Tooltip("Number of full scans published per second")]
    public float scanRate          = 5f;

    [Header("Filters")]
    [Tooltip("Points with ROS z below this value are discarded (removes floor returns)")]
    public float minHeightFilter   = -0.05f;

    [Tooltip("Points closer than this distance are discarded (removes robot body returns)")]
    public float minDistanceFilter = 0.3f;

    // ─── Privati ─────────────────────────────────────────────────────────────

    private ROSConnection ros;
    private float         hAngleIncrement;
    private float         vAngleIncrement;

    // Each point is serialised as: x(4) + y(4) + z(4) + intensity(4) = 16 bytes
    private const int POINT_STEP = 16;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PointCloud2Msg>(topicName);

        // Pre-compute angular increments to avoid per-frame division
        hAngleIncrement = (horizontalFov * Mathf.Deg2Rad) / numRaysHorizontal;
        vAngleIncrement = ((verticalFovMax - verticalFovMin) * Mathf.Deg2Rad)
                          / Mathf.Max(1, numRaysVertical - 1);

        StartCoroutine(PublishLoop());
    }

    // ─── Publish loop ────────────────────────────────────────────────────────

    IEnumerator PublishLoop()
    {
        var wait = new WaitForSeconds(1f / scanRate);
        while (true)
        {
            Publish();
            yield return wait;
        }
    }

    // ─── Scan and publish ────────────────────────────────────────────────────

    void Publish()
    {
        var points = new List<byte[]>();

        for (int v = 0; v < numRaysVertical; v++)
        {
            float pitchRad = (verticalFovMin * Mathf.Deg2Rad) + v * vAngleIncrement;

            for (int h = 0; h < numRaysHorizontal; h++)
            {
                float yawRad = h * hAngleIncrement;

                // Ray direction in the sensor's local Unity space
                // Horizontal plane uses cos/sin on yaw; vertical uses sin on pitch
                Vector3 dirUnity = new Vector3(
                    Mathf.Cos(pitchRad) * Mathf.Cos(yawRad),
                    Mathf.Sin(pitchRad),
                    Mathf.Cos(pitchRad) * Mathf.Sin(yawRad)
                );

                Ray ray = new Ray(transform.position, transform.TransformDirection(dirUnity));

                // Skip rays with no hit — only real surfaces are published
                if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance))
                    continue;

                // Hit point in the sensor's local Unity frame
                Vector3 pUnity = dirUnity.normalized * hit.distance;

                // Convert Unity (right, up, forward) → ROS FLU (forward, left, up)
                float rx = pUnity.z;
                float ry = -pUnity.x;
                float rz = pUnity.y;

                // Filter 1: discard floor returns
                if (rz < minHeightFilter) continue;

                // Filter 2: discard returns from the robot body
                if (hit.distance < minDistanceFilter) continue;

                // Serialise as little-endian FLOAT32 values: x, y, z, intensity
                byte[] point = new byte[POINT_STEP];
                Buffer.BlockCopy(BitConverter.GetBytes(rx),   0, point,  0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(ry),   0, point,  4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(rz),   0, point,  8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(1.0f), 0, point, 12, 4);  // intensity = 1

                points.Add(point);
            }
        }

        // Flatten the per-point byte arrays into a single contiguous buffer
        byte[] data = new byte[points.Count * POINT_STEP];
        for (int i = 0; i < points.Count; i++)
            Buffer.BlockCopy(points[i], 0, data, i * POINT_STEP, POINT_STEP);

        // Timestamp — uses Time.time (relative to Play start)
        double t = Time.time;
        var stamp = new TimeMsg
        {
            sec     = (int)t,
            nanosec = (uint)((t - Math.Floor(t)) * 1e9)
        };

        // Field descriptors — one entry per FLOAT32 channel
        var fields = new PointFieldMsg[]
        {
            new PointFieldMsg { name = "x",         offset =  0, datatype = PointFieldMsg.FLOAT32, count = 1 },
            new PointFieldMsg { name = "y",         offset =  4, datatype = PointFieldMsg.FLOAT32, count = 1 },
            new PointFieldMsg { name = "z",         offset =  8, datatype = PointFieldMsg.FLOAT32, count = 1 },
            new PointFieldMsg { name = "intensity", offset = 12, datatype = PointFieldMsg.FLOAT32, count = 1 },
        };

        var msg = new PointCloud2Msg
        {
            header       = new HeaderMsg { frame_id = frameId, stamp = stamp },
            height       = 1,                               // unordered cloud → single row
            width        = (uint)points.Count,
            fields       = fields,
            is_bigendian = false,
            point_step   = POINT_STEP,
            row_step     = (uint)(points.Count * POINT_STEP),
            data         = data,
            is_dense     = true
        };

        ros.Publish(topicName, msg);
    }
}