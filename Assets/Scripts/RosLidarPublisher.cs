using System.Collections;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;

public class RosLidarPublisher : MonoBehaviour
{
    public string topicName = "/scan";
    public string frameId = "base_scan";

    public int numRays = 360;
    public float maxDistance = 10f;
    public float scanRate = 5f;

    private ROSConnection ros;
    private float angleIncrement;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<LaserScanMsg>(topicName);

        angleIncrement = 2 * Mathf.PI / numRays;

        StartCoroutine(PublishScan());
    }

    IEnumerator PublishScan()
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
        float[] ranges = new float[numRays];

        for (int i = 0; i < numRays; i++)
        {
            float angle = i * angleIncrement;

            Vector3 direction = new Vector3(
                Mathf.Cos(angle),
                0,
                Mathf.Sin(angle)
            );

            Ray ray = new Ray(transform.position, transform.TransformDirection(direction));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxDistance))
                ranges[i] = hit.distance;
            else
                ranges[i] = maxDistance;
        }

        double t = Time.time;
        var stamp = new TimeMsg
        {
            sec = (int)t,
            nanosec = (uint)((t - Mathf.Floor((float)t)) * 1e9)
        };

        var msg = new LaserScanMsg
        {
            header = new HeaderMsg
            {
                frame_id = frameId,
                stamp = stamp
            },
            angle_min = 0f,
            angle_max = 2 * Mathf.PI,
            angle_increment = angleIncrement,
            range_min = 0.12f,
            range_max = maxDistance,
            ranges = ranges
        };

        ros.Publish(topicName, msg);
    }
}