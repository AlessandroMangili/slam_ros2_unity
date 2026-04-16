using System;
using System.Collections;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;

/// <summary>
/// Simulates a 2D LiDAR by casting a 3D grid of rays and projecting
/// the hits onto a horizontal slice, then publishing the result as a
/// ROS2 LaserScan message.
///
/// The scan is built in two passes:
///   1. Rays are cast across the full vertical × horizontal FOV grid.
///   2. Each hit is converted to ROS FLU coordinates and checked against
///      a configurable height slice (laserSliceMinZ / laserSliceMaxZ).
///      Only points within the slice contribute to the 2D scan.
///   3. For each horizontal angle bin, the closest hit distance in the
///      XY plane is kept (min-distance per bin).
///
/// The resulting ranges array maps directly to a LaserScan message with
/// angle_min = 0 and angle_max = 2π.
///
/// Timestamp uses real Unix time (DateTimeOffset.UtcNow) to avoid TF
/// lookup failures caused by Time.time starting at 0 on Play.
/// </summary>
public class RosPointCloudToLaserScan : MonoBehaviour
{
    [Header("ROS Settings")]
    public string topicName = "/scan";
    public string frameId   = "base_scan";

    [Header("Scan Settings")]
    [Tooltip("Number of horizontal angle bins in the output LaserScan")]
    public int   numRaysHorizontal = 360;

    [Tooltip("Number of vertical layers used to populate each horizontal bin")]
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

    [Tooltip("Hits closer than this distance are discarded (removes robot body returns)")]
    public float minDistanceFilter = 0.3f;

    [Header("Laser Slice Settings")]
    [Tooltip("Minimum ROS z value (metres) for a hit to be included in the 2D slice")]
    public float laserSliceMinZ = -0.05f;

    [Tooltip("Maximum ROS z value (metres) for a hit to be included in the 2D slice")]
    public float laserSliceMaxZ =  0.05f;

    // ─── Privati ─────────────────────────────────────────────────────────────

    private ROSConnection ros;
    private float         hAngleIncrement;
    private float         vAngleIncrement;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<LaserScanMsg>(topicName);

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
        // Initialise all bins to maxDistance (no obstacle detected)
        float[] ranges = new float[numRaysHorizontal];
        for (int i = 0; i < numRaysHorizontal; i++)
            ranges[i] = maxDistance;

        for (int v = 0; v < numRaysVertical; v++)
        {
            float pitchRad = (verticalFovMin * Mathf.Deg2Rad) + v * vAngleIncrement;

            for (int h = 0; h < numRaysHorizontal; h++)
            {
                float yawRad = h * hAngleIncrement;

                // Ray direction in the sensor's local Unity space
                Vector3 dirUnity = new Vector3(
                    Mathf.Cos(pitchRad) * Mathf.Cos(yawRad),
                    Mathf.Sin(pitchRad),
                    Mathf.Cos(pitchRad) * Mathf.Sin(yawRad)
                );

                Ray ray = new Ray(transform.position, transform.TransformDirection(dirUnity));

                if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance)) continue;
                if (hit.distance < minDistanceFilter) continue;

                // Hit point in the sensor's local Unity frame
                Vector3 pUnity = dirUnity.normalized * hit.distance;

                // Convert Unity (right, up, forward) → ROS FLU (forward, left, up)
                float rx = pUnity.z;
                float ry = -pUnity.x;
                float rz = pUnity.y;

                // Discard points outside the horizontal height slice
                if (rz < laserSliceMinZ || rz > laserSliceMaxZ) continue;

                // Map the hit's horizontal angle to a range bin index
                float angleRos = Mathf.Atan2(ry, rx);
                if (angleRos < 0f) angleRos += 2f * Mathf.PI;

                int idx = Mathf.RoundToInt(angleRos / hAngleIncrement) % numRaysHorizontal;

                // Keep only the closest hit in the XY plane for this bin
                float distXY = Mathf.Sqrt(rx * rx + ry * ry);
                if (distXY < ranges[idx])
                    ranges[idx] = distXY;
            }
        }

        // Timestamp — uses real Unix time to avoid TF errors in Nav2
        long unixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var stamp = new TimeMsg
        {
            sec     = (int)(unixMs / 1000),
            nanosec = (uint)((unixMs % 1000) * 1_000_000)
        };

        var msg = new LaserScanMsg
        {
            header = new HeaderMsg
            {
                frame_id = frameId,
                stamp    = stamp
            },
            angle_min       = 0f,
            angle_max       = 2f * Mathf.PI,
            angle_increment = hAngleIncrement,
            time_increment  = 0f,
            scan_time       = 1f / scanRate,
            range_min       = minDistanceFilter,
            range_max       = maxDistance,
            ranges          = ranges,
            intensities     = new float[0]
        };

        ros.Publish(topicName, msg);
    }
}