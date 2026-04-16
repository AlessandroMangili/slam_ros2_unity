using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using UnityEngine.UI;

/// <summary>
/// Subscribes to a ROS2 LaserScan topic and renders the scan data
/// as a 2D radar display on a UI RawImage canvas.
///
/// Features:
///   - Object pooling to avoid per-frame allocations.
///   - Per-ray smoothing via Lerp to reduce visual jitter.
///   - Dynamic max-distance scaling that adapts to the environment.
///   - Optional fade effect that gradually dims older scan points.
///   - Rotating sweep line for a classic radar aesthetic.
///
/// Points are coloured from red (close) to yellow (far) and scaled
/// inversely with distance so near obstacles appear larger.
/// </summary>
public class RadarVisualizer : MonoBehaviour
{
    [Header("ROS")]
    public string topicName = "/scan";

    [Header("UI")]
    [Tooltip("Center of the radar display — points are positioned relative to this RectTransform")]
    public RectTransform radarCenter;
    public GameObject    pointPrefab;
    public RectTransform sweepLine;

    [Header("Radar Scaling")]
    [Tooltip("Distances below this value are ignored (filters out noise near the sensor)")]
    public float minLidarRange = 0.2f;

    [Tooltip("Initial max distance used for normalisation — auto-adjusts at runtime")]
    public float dynamicMaxDistance = 1f;

    [Header("Sweep")]
    [Tooltip("Degrees per second the sweep line rotates")]
    public float sweepSpeed = 120f;

    [Header("Smoothing")]
    [Range(0f, 1f)]
    [Tooltip("Lerp factor applied to each ray per scan (0 = frozen, 1 = no smoothing)")]
    public float smoothing = 0.25f;

    [Header("Fade")]
    [Tooltip("When enabled, points fade out over time instead of being recycled immediately")]
    public bool  enableFade = false;

    [Tooltip("Alpha reduction per second when fade is enabled")]
    public float fadeSpeed  = 0.2f;

    [Header("Pooling")]
    [Tooltip("Total number of pooled point GameObjects — must be >= number of scan rays")]
    public int poolSize = 400;

    // ─── Privati ─────────────────────────────────────────────────────────────

    private GameObject[] pool;
    private int          poolIndex = 0;

    private float[] lastScan;   // Smoothed range values from the previous scan

    private ROSConnection ros;

    // Used only when fade is enabled to track point lifetime
    private class RadarPoint
    {
        public GameObject obj;
        public Image      img;
        public float      life;
    }

    private readonly List<RadarPoint> activePoints = new List<RadarPoint>();

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<LaserScanMsg>(topicName, OnScanReceived);

        // Pre-allocate the point pool
        pool = new GameObject[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            pool[i] = Instantiate(pointPrefab, radarCenter);
            pool[i].SetActive(false);
        }
    }

    void Update()
    {
        // Rotate the sweep line clockwise
        if (sweepLine != null)
            sweepLine.Rotate(0f, 0f, -sweepSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        // Fade logic runs only when the feature is enabled
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
                c.a     = Mathf.Clamp01(p.life);
                p.img.color = c;
            }
        }
    }

    // ─── ROS callback ────────────────────────────────────────────────────────

    void OnScanReceived(LaserScanMsg msg)
    {
        int n = msg.ranges.Length;

        // Reallocate the smoothing buffer only when the ray count changes
        if (lastScan == null || lastScan.Length != n)
            lastScan = new float[n];

        float maxSeen = 0f;

        for (int i = 0; i < n; i++)
        {
            float raw = msg.ranges[i];

            // Discard out-of-range readings
            if (raw <= msg.range_min || raw >= msg.range_max) continue;

            // Smooth the reading to reduce visual jitter
            lastScan[i] = Mathf.Lerp(lastScan[i], raw, smoothing);

            if (lastScan[i] > maxSeen)
                maxSeen = lastScan[i];

            float angle = msg.angle_min + i * msg.angle_increment;
            DrawPoint(angle, lastScan[i]);
        }

        // Slowly adapt the max distance to the current environment
        if (maxSeen > minLidarRange)
            dynamicMaxDistance = Mathf.Lerp(dynamicMaxDistance, maxSeen, 0.1f);
    }

    // ─── Rendering ───────────────────────────────────────────────────────────

    void DrawPoint(float angle, float distance)
    {
        // ROS angle 0 points right (+X) but Unity UI +Y is up,
        // so rotate 90° CCW to align forward with the top of the radar
        angle += Mathf.PI * 0.5f;

        GameObject    obj = GetPoint();
        RectTransform rt  = obj.GetComponent<RectTransform>();
        Image         img = obj.GetComponent<Image>();

        float radarRadius = radarCenter.rect.width * 0.5f;
        float normalized  = Mathf.Clamp01(distance / dynamicMaxDistance);
        float radius      = normalized * radarRadius;

        rt.anchoredPosition = new Vector2(Mathf.Cos(angle) * radius,
                                          Mathf.Sin(angle) * radius);

        // Red = close, yellow = far
        img.color = Color.Lerp(Color.red, Color.yellow, normalized);

        // Larger dot for nearby obstacles, smaller for distant ones
        float size = Mathf.Lerp(6f, 2f, normalized);
        rt.sizeDelta = new Vector2(size, size);

        obj.SetActive(true);

        // Only track the point for fading if the feature is enabled,
        // otherwise the list would grow unboundedly and never be cleared
        if (enableFade)
        {
            activePoints.Add(new RadarPoint { obj = obj, img = img, life = 1f });
        }
    }

    /// <summary>
    /// Returns the next pooled point using a circular index.
    /// If the pool is exhausted the oldest point is reused automatically.
    /// </summary>
    GameObject GetPoint()
    {
        GameObject obj = pool[poolIndex];
        poolIndex = (poolIndex + 1) % poolSize;
        return obj;
    }
}