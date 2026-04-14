using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;

public class CostmapOverlaySubscriber : MonoBehaviour
{
    [SerializeField] private string topicName = "/global_costmap/costmap_raw";
    [SerializeField] private float zoom = 2f;
    [SerializeField] private float maxAlpha = 0.45f;

    private ROSConnection ros;
    private RawImage overlayImage;
    private Texture2D overlayTexture;

    private OccupancyGridMsg pendingMsg = null;
    private bool hasNewCostmap = false;
    private readonly object lockObj = new object();

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<OccupancyGridMsg>(topicName, OnCostmapReceived);

        overlayImage = GetComponent<RawImage>();
        overlayImage.color = Color.white;
        overlayImage.raycastTarget = false;

        overlayTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        overlayTexture.filterMode = FilterMode.Point;
        overlayTexture.wrapMode = TextureWrapMode.Clamp;

        ClearTexture(overlayTexture, new Color32(0, 0, 0, 0));
        overlayImage.texture = overlayTexture;
        overlayImage.enabled = true;

        overlayImage.uvRect = new Rect(
            0.5f - 0.5f / zoom,
            0.5f - 0.5f / zoom,
            1f / zoom,
            1f / zoom
        );

        Debug.Log($"Costmap overlay in ascolto su {topicName}");
    }

    void OnCostmapReceived(OccupancyGridMsg msg)
    {
        lock (lockObj)
        {
            pendingMsg = msg;
            hasNewCostmap = true;
        }
    }

    void Update()
    {
        OccupancyGridMsg msg = null;

        lock (lockObj)
        {
            if (!hasNewCostmap) return;
            msg = pendingMsg;
            hasNewCostmap = false;
        }

        if (msg != null)
            ApplyCostmap(msg);
    }

    void ApplyCostmap(OccupancyGridMsg msg)
    {
        int width = (int)msg.info.width;
        int height = (int)msg.info.height;

        if (width <= 0 || height <= 0 || msg.data == null || msg.data.Length == 0)
            return;

        if (overlayTexture.width != width || overlayTexture.height != height)
        {
            overlayTexture.Reinitialize(width, height);
            overlayTexture.filterMode = FilterMode.Point;
            overlayTexture.wrapMode = TextureWrapMode.Clamp;
        }

        Color32[] pixels = new Color32[width * height];

        for (int i = 0; i < msg.data.Length && i < pixels.Length; i++)
        {
            sbyte cell = msg.data[i];
            Color32 c;

            if (cell <= 0)
            {
                c = new Color32(0, 0, 0, 0);
            }
            else
            {
                float t = Mathf.Clamp01(cell / 100f);
                byte r = 255;
                byte g = (byte)Mathf.Lerp(200f, 0f, t);
                byte b = 0;
                byte a = (byte)Mathf.Lerp(20f, maxAlpha * 255f, t);

                c = new Color32(r, g, b, a);
            }

            int x = i % width;
            int y = i / width;
            pixels[(height - 1 - y) * width + x] = c;
        }

        overlayTexture.SetPixels32(pixels);
        overlayTexture.Apply(false);
    }

    private void ClearTexture(Texture2D tex, Color32 color)
    {
        Color32[] pixels = new Color32[tex.width * tex.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        tex.SetPixels32(pixels);
        tex.Apply(false);
    }
}