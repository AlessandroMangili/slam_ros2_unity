using System;
using System.Collections;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;

public class RosPointCloud2Publisher : MonoBehaviour
{
    [Header("ROS Settings")]
    public string topicName = "/point_cloud";
    public string frameId = "base_scan";

    [Header("Scan Settings")]
    public int numRaysHorizontal = 360;
    public int numRaysVertical = 16;
    public float horizontalFov = 360f;
    public float verticalFovMin = -15f;
    public float verticalFovMax = 15f;
    public float maxDistance = 10f;
    public float scanRate = 5f;

    public float minHeightFilter = -0.05f;
    public float minDistanceFilter = 0.3f; // regola in base alle dimensioni del robot

    private ROSConnection ros;
    private float hAngleIncrement;
    private float vAngleIncrement;

    // Costanti PointCloud2
    private const int POINT_STEP = 16; // x(4) + y(4) + z(4) + intensity(4)

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PointCloud2Msg>(topicName);

        hAngleIncrement = (horizontalFov * Mathf.Deg2Rad) / numRaysHorizontal;
        vAngleIncrement = ((verticalFovMax - verticalFovMin) * Mathf.Deg2Rad) / Mathf.Max(1, numRaysVertical - 1);

        StartCoroutine(PublishLoop());
    }

    IEnumerator PublishLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(1f / scanRate);
        while (true)
        {
            Publish();
            yield return wait;
        }
    }

    void Publish()
    {
        var points = new System.Collections.Generic.List<byte[]>();

        for (int v = 0; v < numRaysVertical; v++)
        {
            float pitchRad = (verticalFovMin * Mathf.Deg2Rad) + v * vAngleIncrement;

            for (int h = 0; h < numRaysHorizontal; h++)
            {
                float yawRad = h * hAngleIncrement;

                // Direzione nel frame locale Unity
                Vector3 dirUnity = new Vector3(
                    Mathf.Cos(pitchRad) * Mathf.Cos(yawRad),
                    Mathf.Sin(pitchRad),
                    Mathf.Cos(pitchRad) * Mathf.Sin(yawRad)
                );

                Ray ray = new Ray(transform.position, transform.TransformDirection(dirUnity));
                float dist = maxDistance;
                float intensity = 0f;

                if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
                {
                    dist = hit.distance;
                    intensity = 1f;
                }
                else
                {
                    continue; // salta i punti senza hit (opzionale)
                }

                // Punto in frame locale Unity
                Vector3 pUnity = dirUnity.normalized * dist;

                // Conversione Unity -> ROS (FLU)
                // Unity: x=right, y=up, z=forward
                // ROS:   x=forward, y=left, z=up
                float rx = pUnity.z;
                float ry = -pUnity.x;
                float rz = pUnity.y;

                // Filtro altezza minima (pavimento)
                if (rz < minHeightFilter) continue;

                // Filtro distanza minima (corpo del robot)
                if (dist < minDistanceFilter) continue;

                // Serializzazione little-endian
                byte[] point = new byte[POINT_STEP];
                Buffer.BlockCopy(BitConverter.GetBytes(rx),        0, point, 0,  4);
                Buffer.BlockCopy(BitConverter.GetBytes(ry),        0, point, 4,  4);
                Buffer.BlockCopy(BitConverter.GetBytes(rz),        0, point, 8,  4);
                Buffer.BlockCopy(BitConverter.GetBytes(intensity), 0, point, 12, 4);

                points.Add(point);
            }
        }

        // Concatena tutti i punti in un unico array
        byte[] data = new byte[points.Count * POINT_STEP];
        for (int i = 0; i < points.Count; i++)
            Buffer.BlockCopy(points[i], 0, data, i * POINT_STEP, POINT_STEP);

        // Timestamp
        double t = Time.time;
        var stamp = new TimeMsg
        {
            sec = (int)t,
            nanosec = (uint)((t - Math.Floor(t)) * 1e9)
        };

        // Campi (field descriptors)
        var fields = new PointFieldMsg[]
        {
            new PointFieldMsg { name = "x",         offset = 0,  datatype = PointFieldMsg.FLOAT32, count = 1 },
            new PointFieldMsg { name = "y",         offset = 4,  datatype = PointFieldMsg.FLOAT32, count = 1 },
            new PointFieldMsg { name = "z",         offset = 8,  datatype = PointFieldMsg.FLOAT32, count = 1 },
            new PointFieldMsg { name = "intensity", offset = 12, datatype = PointFieldMsg.FLOAT32, count = 1 },
        };

        var msg = new PointCloud2Msg
        {
            header = new HeaderMsg
            {
                frame_id = frameId,
                stamp = stamp
            },
            height = 1,
            width = (uint)points.Count,
            fields = fields,
            is_bigendian = false,
            point_step = POINT_STEP,
            row_step = (uint)(points.Count * POINT_STEP),
            data = data,
            is_dense = true
        };

        ros.Publish(topicName, msg);
    }
}