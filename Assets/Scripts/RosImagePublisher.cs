using System.Collections;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class RosImagePublisher : MonoBehaviour
{
    [Header("Unity")]
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private int width = 640;
    [SerializeField] private int height = 480;
    [SerializeField] private int publishFps = 15;

    [Header("Flip")]
    [SerializeField] private bool flipHorizontal = true;
    [SerializeField] private bool flipVertical = false;

    [Header("ROS")]
    [SerializeField] private string topicName = "/camera/image_raw";
    [SerializeField] private string frameId = "camera_rgb_optical_frame";

    [Header("Texture")]
    [SerializeField] private RenderTexture renderTexture;

    private ROSConnection ros;
    private Texture2D readTexture;
    private WaitForSeconds wait;

    void Awake()
    {
        if (sourceCamera == null)
            sourceCamera = GetComponent<Camera>();
    }

    void Start()
    {
        if (sourceCamera == null)
        {
            Debug.LogError("[RosImagePublisher] Camera non trovata.");
            enabled = false;
            return;
        }

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ImageMsg>(topicName);

        sourceCamera.targetTexture = renderTexture;

        readTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        wait = new WaitForSeconds(1f / Mathf.Max(1, publishFps));

        StartCoroutine(PublishLoop());
    }

    IEnumerator PublishLoop()
    {
        while (true)
        {
            PublishFrame();
            yield return wait;
        }
    }

    void PublishFrame()
    {
        if (renderTexture == null || readTexture == null)
            return;

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = renderTexture;

        //sourceCamera.Render();
        readTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        readTexture.Apply();

        RenderTexture.active = prev;

        Color32[] src = readTexture.GetPixels32();
        Color32[] dst = new Color32[src.Length];

        for (int y = 0; y < height; y++)
        {
            int dstY = flipVertical ? (height - 1 - y) : y;

            for (int x = 0; x < width; x++)
            {
                int dstX = flipHorizontal ? (width - 1 - x) : x;

                int srcIndex = y * width + x;
                int dstIndex = dstY * width + dstX;

                dst[dstIndex] = src[srcIndex];
            }
        }

        byte[] data = new byte[dst.Length * 4];
        for (int i = 0; i < dst.Length; i++)
        {
            int j = i * 4;
            data[j]     = dst[i].r;
            data[j + 1] = dst[i].g;
            data[j + 2] = dst[i].b;
            data[j + 3] = dst[i].a;
        }

        double t = Time.time;
        var stamp = new TimeMsg
        {
            sec = (int)t,
            nanosec = (uint)((t - Mathf.Floor((float)t)) * 1_000_000_000.0)
        };

        var msg = new ImageMsg
        {
            header = new HeaderMsg
            {
                stamp = stamp,
                frame_id = frameId
            },
            height = (uint)height,
            width = (uint)width,
            encoding = "rgba8",
            is_bigendian = 0,
            step = (uint)(width * 4),
            data = data
        };

        ros.Publish(topicName, msg);
    }

    void OnDestroy()
    {
        if (sourceCamera != null)
            sourceCamera.targetTexture = null;

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (readTexture != null)
            Destroy(readTexture);
    }
}