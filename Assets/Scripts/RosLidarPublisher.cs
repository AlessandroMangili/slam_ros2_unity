using System;
using System.Collections;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;

public class RosPointCloudToLaserScan : MonoBehaviour
{
    [Header("ROS Settings")]
    public string topicName = "/scan";
    public string frameId = "base_scan";

    [Header("Scan Settings")]
    public int numRaysHorizontal = 360;
    public int numRaysVertical = 16;
    public float horizontalFov = 360f;
    public float verticalFovMin = -15f;
    public float verticalFovMax = 15f;
    public float maxDistance = 10f;
    public float scanRate = 5f;
    public float minDistanceFilter = 0.3f;

    [Header("LaserScan Slice Settings")]
    [Tooltip("Altezza minima in ROS (z) per considerare un punto nel laser slice")]
    public float laserSliceMinZ = -0.05f;
    [Tooltip("Altezza massima in ROS (z) per considerare un punto nel laser slice")]
    public float laserSliceMaxZ = 0.05f;

    private ROSConnection ros;
    private float hAngleIncrement;
    private float vAngleIncrement;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<LaserScanMsg>(topicName);

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
        // Array ranges inizializzato a maxDistance (nessun ostacolo)
        float[] ranges = new float[numRaysHorizontal];
        for (int i = 0; i < numRaysHorizontal; i++)
            ranges[i] = maxDistance;

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

                if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance))
                    continue;

                float dist = hit.distance;
                if (dist < minDistanceFilter)
                    continue;

                // Punto in frame locale Unity
                Vector3 pUnity = dirUnity.normalized * dist;

                // Conversione Unity -> ROS (FLU)
                float rx = pUnity.z;
                float ry = -pUnity.x;
                float rz = pUnity.y;

                // Filtra solo i punti nello slice orizzontale
                if (rz < laserSliceMinZ || rz > laserSliceMaxZ)
                    continue;

                // Calcola l'angolo orizzontale ROS (atan2 nel piano XY)
                float angleRos = Mathf.Atan2(ry, rx);
                if (angleRos < 0)
                    angleRos += 2f * Mathf.PI;

                // Mappa l'angolo all'indice del ray
                int idx = Mathf.RoundToInt(angleRos / hAngleIncrement) % numRaysHorizontal;

                // Prendi la distanza minima per quell'angolo
                float distXY = Mathf.Sqrt(rx * rx + ry * ry);
                if (distXY < ranges[idx])
                    ranges[idx] = distXY;
            }
        }

        // Timestamp
        double t = Time.time;
        var stamp = new TimeMsg
        {
            sec = (int)t,
            nanosec = (uint)((t - Math.Floor(t)) * 1e9)
        };

        var msg = new LaserScanMsg
        {
            header = new HeaderMsg
            {
                frame_id = frameId,
                stamp = stamp
            },
            angle_min = 0f,
            angle_max = 2f * Mathf.PI,
            angle_increment = hAngleIncrement,
            time_increment = 0f,
            scan_time = 1f / scanRate,
            range_min = minDistanceFilter,
            range_max = maxDistance,
            ranges = ranges,
            intensities = new float[0]
        };

        ros.Publish(topicName, msg);
    }
}