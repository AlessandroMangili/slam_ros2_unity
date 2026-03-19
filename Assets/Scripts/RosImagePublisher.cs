using System.Collections;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;
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
    [SerializeField] private string frameId = "camera_rgb_optical_frame";

    [Header("ROS")]
    [SerializeField] private string topicName = "/camera/image_raw";

    private ROSConnection ros;
    private RenderTexture renderTexture;
    private Texture2D texture2D;
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
            Debug.LogError("[RosImagePublisher] Nessuna Camera trovata.");
            enabled = false;
            return;
        }

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ImageMsg>(topicName);

        renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        sourceCamera.targetTexture = renderTexture;

        texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
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
        if (sourceCamera == null || renderTexture == null || texture2D == null)
            return;

        var prev = RenderTexture.active;
        RenderTexture.active = renderTexture;

        sourceCamera.Render();
        texture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture2D.Apply(false);

        RenderTexture.active = prev;

        var pixels = texture2D.GetPixels32();
        var data = new byte[pixels.Length * 4];

        for (int i = 0; i < pixels.Length; i++)
        {
            int j = i * 4;
            data[j] = pixels[i].r;
            data[j + 1] = pixels[i].g;
            data[j + 2] = pixels[i].b;
            data[j + 3] = pixels[i].a;
        }

        double t = Time.time;
        var stamp = new TimeMsg
        {
            sec = (int)t,
            nanosec = (uint)((t - Mathf.Floor((float)t)) * 1e9)
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

        if (texture2D != null)
            Destroy(texture2D);
    }
}