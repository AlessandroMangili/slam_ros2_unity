using System.Collections;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

/// <summary>
/// Captures frames from a Unity Camera via a RenderTexture and publishes
/// them as ROS2 sensor_msgs/Image messages at a configurable frame rate.
///
/// The pixel buffer is read back from the GPU each frame and optionally
/// flipped horizontally and/or vertically before being serialised into
/// a raw RGBA8 byte array.
///
/// The encoding is always "rgba8" (4 bytes per pixel, row-major).
/// step = width * 4 bytes.
///
/// Note: sourceCamera.Render() is intentionally not called manually —
/// Unity renders the camera automatically each frame via its RenderTexture.
/// </summary>
[RequireComponent(typeof(Camera))]
public class RosImagePublisher : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private int    width      = 640;
    [SerializeField] private int    height     = 480;
    [SerializeField] private int    publishFps = 15;

    [Header("Flip")]
    [Tooltip("Mirror the image horizontally before publishing")]
    [SerializeField] private bool flipHorizontal = true;

    [Tooltip("Mirror the image vertically before publishing")]
    [SerializeField] private bool flipVertical   = false;

    [Header("ROS")]
    [SerializeField] private string topicName = "/camera/image_raw";
    [SerializeField] private string frameId   = "camera_rgb_optical_frame";

    [Header("Texture")]
    [Tooltip("RenderTexture the camera renders into — must match width × height")]
    [SerializeField] private RenderTexture renderTexture;

    private ROSConnection  ros;
    private Texture2D      readTexture;
    private WaitForSeconds wait;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Awake()
    {
        if (sourceCamera == null)
            sourceCamera = GetComponent<Camera>();
    }

    void Start()
    {
        if (sourceCamera == null)
        {
            Debug.LogError("[RosImagePublisher] Camera not found.");
            enabled = false;
            return;
        }

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ImageMsg>(topicName);

        sourceCamera.targetTexture = renderTexture;

        // CPU-side texture used to read pixels back from the RenderTexture
        readTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        wait = new WaitForSeconds(1f / Mathf.Max(1, publishFps));

        StartCoroutine(PublishLoop());
    }

    void OnDestroy()
    {
        // Detach the RenderTexture so the camera reverts to screen output
        if (sourceCamera != null)
            sourceCamera.targetTexture = null;

        if (renderTexture != null)
        {
            renderTexture.Release();
            renderTexture = null;
        }

        if (readTexture != null)
            Destroy(readTexture);
    }

    // ─── Publish loop ────────────────────────────────────────────────────────

    IEnumerator PublishLoop()
    {
        while (true)
        {
            PublishFrame();
            yield return wait;
        }
    }

    // ─── Frame capture and publish ───────────────────────────────────────────

    void PublishFrame()
    {
        if (renderTexture == null || readTexture == null) return;

        // Read pixels from the RenderTexture into the CPU-side Texture2D
        RenderTexture prev       = RenderTexture.active;
        RenderTexture.active     = renderTexture;
        readTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        readTexture.Apply();
        RenderTexture.active     = prev;

        Color32[] src = readTexture.GetPixels32();
        Color32[] dst = new Color32[src.Length];

        // Apply optional horizontal and/or vertical flip
        for (int y = 0; y < height; y++)
        {
            int dstY = flipVertical ? (height - 1 - y) : y;

            for (int x = 0; x < width; x++)
            {
                int dstX     = flipHorizontal ? (width - 1 - x) : x;
                int srcIndex = y    * width + x;
                int dstIndex = dstY * width + dstX;
                dst[dstIndex] = src[srcIndex];
            }
        }

        // Serialise Color32 array to a raw RGBA8 byte buffer
        byte[] data = new byte[dst.Length * 4];
        for (int i = 0; i < dst.Length; i++)
        {
            int j      = i * 4;
            data[j]    = dst[i].r;
            data[j + 1] = dst[i].g;
            data[j + 2] = dst[i].b;
            data[j + 3] = dst[i].a;
        }

        // Timestamp relative to Play start (Nav2 is not involved here)
        double t   = Time.time;
        var stamp  = new TimeMsg
        {
            sec     = (int)t,
            nanosec = (uint)((t - Mathf.Floor((float)t)) * 1_000_000_000.0)
        };

        var msg = new ImageMsg
        {
            header       = new HeaderMsg { stamp = stamp, frame_id = frameId },
            height       = (uint)height,
            width        = (uint)width,
            encoding     = "rgba8",
            is_bigendian = 0,
            step         = (uint)(width * 4),   // bytes per row = width * 4 channels
            data         = data
        };

        ros.Publish(topicName, msg);
    }
}