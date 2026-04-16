using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;

/// <summary>
/// Subscribes to a ROS2 OccupancyGrid topic and renders the map
/// as a live texture on a UI RawImage.
///
/// Thread safety: the ROS callback runs on a separate thread, so the
/// incoming message is stored behind a lock and consumed on the main
/// thread inside Update() before being applied to the texture.
///
/// The uvRect is configured at startup to crop and zoom the texture
/// around its center, matching the zoom factor used by RobotMapMarker.
/// </summary>
public class MapSubscriber : MonoBehaviour
{
    [SerializeField] private string topicName = "/map";

    [Tooltip("Zoom factor applied to the map texture (must match RobotMapMarker.zoom)")]
    [SerializeField] private float zoom = 2.5f;

    private ROSConnection ros;
    private Texture2D     mapTexture;
    private RawImage      rawImage;

    // Pending message written by the ROS thread, read by the main thread
    private OccupancyGridMsg pendingMsg = null;
    private bool             hasNewMap  = false;
    private readonly object  lockObj    = new object();

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<OccupancyGridMsg>(topicName, OnMapReceived);

        rawImage   = GetComponent<RawImage>();
        mapTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);

        // Crop the texture around its centre based on the zoom level
        // uvRect = (centre - 0.5/zoom, centre - 0.5/zoom, 1/zoom, 1/zoom)
        rawImage.uvRect  = new Rect(
            0.5f - 0.5f / zoom,
            0.5f - 0.5f / zoom,
            1f / zoom,
            1f / zoom
        );
        rawImage.texture = mapTexture;
    }

    void Update()
    {
        OccupancyGridMsg msg = null;

        // Consume the pending message on the main thread
        lock (lockObj)
        {
            if (!hasNewMap) return;
            msg       = pendingMsg;
            hasNewMap = false;
        }

        ApplyMap(msg);
    }

    // ─── ROS callback ────────────────────────────────────────────────────────

    /// <summary>
    /// Stores the incoming map message so it can be processed on the main thread.
    /// Called on the ROS-TCP-Connector receive thread.
    /// </summary>
    void OnMapReceived(OccupancyGridMsg msg)
    {
        lock (lockObj)
        {
            pendingMsg = msg;
            hasNewMap  = true;
        }
    }

    // ─── Map rendering ───────────────────────────────────────────────────────

    /// <summary>
    /// Converts OccupancyGrid cell values to colours and uploads them to the texture.
    ///
    /// Cell encoding:
    ///   -1  → gray  (unknown)
    ///    0  → white (free)
    ///   >0  → black (occupied)
    ///
    /// The Y axis is flipped because ROS maps have row 0 at the bottom
    /// while Unity textures have row 0 at the top.
    /// </summary>
    void ApplyMap(OccupancyGridMsg msg)
    {
        int width  = (int)msg.info.width;
        int height = (int)msg.info.height;

        // Resize the texture only when the map dimensions change
        if (mapTexture.width != width || mapTexture.height != height)
            mapTexture.Reinitialize(width, height);

        Color[] pixels = new Color[width * height];

        for (int i = 0; i < msg.data.Length; i++)
        {
            sbyte cell = msg.data[i];

            Color c;
            if      (cell == -1) c = Color.gray;   // unknown
            else if (cell ==  0) c = Color.white;  // free
            else                 c = Color.black;  // occupied

            int x = i % width;
            int y = i / width;

            // Flip Y: ROS row 0 (bottom) → texture row height-1 (top)
            pixels[(height - 1 - y) * width + x] = c;
        }

        mapTexture.SetPixels(pixels);
        mapTexture.Apply();
    }
}