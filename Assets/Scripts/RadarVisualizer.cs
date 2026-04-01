using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using UnityEngine.UI;

public class RadarVisualizer : MonoBehaviour
{
    [Header("ROS")]
    public string topicName = "/scan";

    [Header("Radar Settings")]
    public RectTransform radarCenter;
    public GameObject pointPrefab;

    public float radarRadius = 100f;
    public float maxDistance = 10f;

    public RectTransform sweepLine;
    public float sweepSpeed = 120f;

    private float[] lastScan;

    [Header("Pooling")]
    public int poolSize = 400;

    private GameObject[] pool;
    private int poolIndex = 0;

    private ROSConnection ros;

    void Start()
    {
        // ROS subscribe
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<LaserScanMsg>(topicName, OnScanReceived);

        // init pool
        pool = new GameObject[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            pool[i] = Instantiate(pointPrefab, radarCenter);
            pool[i].SetActive(false);
        }
    }

    void Update()
    {
        sweepLine.Rotate(0, 0, -sweepSpeed * Time.deltaTime);
    }

    void OnScanReceived(LaserScanMsg msg)
    {
        ResetPool();

        for (int i = 0; i < msg.ranges.Length; i++)
        {
            float distance = msg.ranges[i];

            // filtra valori invalidi
            if (distance <= msg.range_min || distance >= msg.range_max)
                continue;

            float angle = msg.angle_min + i * msg.angle_increment;

            DrawPoint(angle, distance);
        }
    }

    void DrawPoint(float angle, float distance)
    {
        angle += Mathf.PI / 2f;

        GameObject point = GetPoint();

        RectTransform rt = point.GetComponent<RectTransform>();
        Image img = point.GetComponent<Image>();

        // 🔵 normalizzazione distanza
        float normalized = Mathf.Clamp01(distance / maxDistance);

        // 📍 posizione nel radar
        float radius = normalized * radarRadius;

        float x = Mathf.Cos(angle) * radius;
        float y = Mathf.Sin(angle) * radius;

        rt.anchoredPosition = new Vector2(x, y);

        // 🎨 colore (vicino rosso, lontano giallo)
        img.color = Color.Lerp(Color.red, Color.yellow, normalized);

        // 📏 dimensione (vicino più grande)
        float size = Mathf.Lerp(6f, 2f, normalized);
        rt.sizeDelta = new Vector2(size, size);

        point.SetActive(true);
    }

    GameObject GetPoint()
    {
        GameObject obj = pool[poolIndex];
        poolIndex = (poolIndex + 1) % poolSize;
        return obj;
    }

    void ResetPool()
    {
        for (int i = 0; i < pool.Length; i++)
            pool[i].SetActive(false);

        poolIndex = 0;
    }
}