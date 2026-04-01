using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using UnityEngine.UI;

public class RadarVisualizer : MonoBehaviour
{
    [Header("ROS")]
    public string topicName = "/scan";

    [Header("UI")]
    public RectTransform radarCenter;
    public GameObject pointPrefab;
    public RectTransform sweepLine;

    [Header("Radar Scaling")]
    public float minLidarRange = 0.2f;
    public float dynamicMaxDistance = 1f;

    [Header("Sweep")]
    public float sweepSpeed = 120f;

    [Header("Smoothing")]
    [Range(0f, 1f)]
    public float smoothing = 0.25f;

    [Header("Fade")]
    public bool enableFade = false;
    public float fadeSpeed = 0.2f;

    [Header("Pooling")]
    public int poolSize = 400;

    private GameObject[] pool;
    private int poolIndex = 0;

    private float[] lastScan;

    private ROSConnection ros;

    // 🔥 struttura per gestione stabile punti
    private class RadarPoint
    {
        public GameObject obj;
        public Image img;
        public float life;
    }

    private List<RadarPoint> activePoints = new List<RadarPoint>();

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<LaserScanMsg>(topicName, OnScanReceived);

        pool = new GameObject[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            pool[i] = Instantiate(pointPrefab, radarCenter);
            pool[i].SetActive(false);
        }
    }

    void Update()
    {
        // Sweep radar
        if (sweepLine != null)
            sweepLine.Rotate(0, 0, -sweepSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (!enableFade) return;

        for (int i = activePoints.Count - 1; i >= 0; i--)
        {
            var p = activePoints[i];

            p.life -= Time.deltaTime * fadeSpeed;

            if (p.life <= 0f)
            {
                p.obj.SetActive(false);
                activePoints.RemoveAt(i);
            }
            else
            {
                Color c = p.img.color;
                c.a = Mathf.Clamp01(p.life);
                p.img.color = c;
            }
        }
    }

    void OnScanReceived(LaserScanMsg msg)
    {
        int n = msg.ranges.Length;

        if (lastScan == null || lastScan.Length != n)
            lastScan = new float[n];

        float maxSeen = 0f;

        for (int i = 0; i < n; i++)
        {
            float raw = msg.ranges[i];

            if (raw <= msg.range_min || raw >= msg.range_max)
                continue;

            // smoothing
            lastScan[i] = Mathf.Lerp(lastScan[i], raw, smoothing);

            if (lastScan[i] > maxSeen)
                maxSeen = lastScan[i];

            float angle = msg.angle_min + i * msg.angle_increment;

            DrawPoint(angle, lastScan[i]);
        }

        // 🔥 auto scaling stabile
        if (maxSeen > minLidarRange)
            dynamicMaxDistance = Mathf.Lerp(dynamicMaxDistance, maxSeen, 0.1f);
    }

    void DrawPoint(float angle, float distance)
    {
        // 🔧 fix asse ROS → Unity
        angle += Mathf.PI / 2f;

        GameObject obj = GetPoint();

        RectTransform rt = obj.GetComponent<RectTransform>();
        Image img = obj.GetComponent<Image>();

        float radarRadius = radarCenter.rect.width * 0.5f;

        float normalized = Mathf.Clamp01(distance / dynamicMaxDistance);

        float radius = normalized * radarRadius;

        float x = Mathf.Cos(angle) * radius;
        float y = Mathf.Sin(angle) * radius;

        rt.anchoredPosition = new Vector2(x, y);

        // colore
        img.color = Color.Lerp(Color.red, Color.yellow, normalized);

        // dimensione
        float size = Mathf.Lerp(6f, 2f, normalized);
        rt.sizeDelta = new Vector2(size, size);

        obj.SetActive(true);

        // gestione stabile (senza reset)
        activePoints.Add(new RadarPoint
        {
            obj = obj,
            img = img,
            life = 1f
        });
    }

    GameObject GetPoint()
    {
        GameObject obj = pool[poolIndex];
        poolIndex = (poolIndex + 1) % poolSize;
        return obj;
    }
}