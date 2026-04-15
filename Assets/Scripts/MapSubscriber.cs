using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;

public class MapSubscriber : MonoBehaviour
{
    ROSConnection ros;
    Texture2D mapTexture;
    RawImage rawImage;

    OccupancyGridMsg pendingMsg = null;
    bool hasNewMap = false;
    readonly object lockObj = new object();

    [SerializeField] string topicName = "/map";
    [SerializeField] float zoom = 2.5f;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<OccupancyGridMsg>(topicName, OnMapReceived);

        rawImage = GetComponent<RawImage>();
        mapTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        rawImage.uvRect = new Rect(
            0.5f - 0.5f / zoom,
            0.5f - 0.5f / zoom,
            1f / zoom,
            1f / zoom
        );
        rawImage.texture = mapTexture;        
    }

    void OnMapReceived(OccupancyGridMsg msg)
    {
        lock (lockObj)
        {
            pendingMsg = msg;
            hasNewMap = true;
        }
    }

    void Update()
    {
        OccupancyGridMsg msg = null;

        lock (lockObj)
        {
            if (!hasNewMap) return;
            msg = pendingMsg;
            hasNewMap = false;
        }

        ApplyMap(msg);
    }

    void ApplyMap(OccupancyGridMsg msg)
    {
        int width  = (int)msg.info.width;
        int height = (int)msg.info.height;

        if (mapTexture.width != width || mapTexture.height != height)
            mapTexture.Reinitialize(width, height);

        Color[] pixels = new Color[width * height];

        for (int i = 0; i < msg.data.Length; i++)
        {
            sbyte cell = msg.data[i];
            Color c;

            if (cell == -1)     c = Color.gray;   // sconosciuto
            else if (cell == 0) c = Color.white;  // libero
            else                c = Color.black;  // occupato

            int x = i % width;
            int y = i / width;
            pixels[(height - 1 - y) * width + x] = c;
        }

        mapTexture.SetPixels(pixels);
        mapTexture.Apply();
    }
}