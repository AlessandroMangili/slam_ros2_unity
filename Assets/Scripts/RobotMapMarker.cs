using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;

/// <summary>
/// Displays the robot's current position and orientation on the minimap
/// as a red dot with an orange directional arrow.
///
/// Both UI elements are created at runtime as children of the map RawImage,
/// so they share the same coordinate space and zoom as MapSubscriber.
///
/// The marker position is derived from the robot's Unity Transform by
/// converting to ROS map coordinates and then projecting through the
/// texture UV space and the zoom crop applied by MapSubscriber.
///
/// Setup:
///   1. Add this script to any GameObject (e.g. MapBorder or the RawImage itself).
///   2. Assign mapImage       → the RawImage used by MapSubscriber.
///   3. Assign robotTransform → the robot's base_link Transform.
///   4. Set offsetX / offsetZ / zoom to the same values used in
///      MapSubscriber, ROSNavigator, and ROSPathVisualizer.
/// </summary>
public class RobotMapMarker : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The map RawImage used by MapSubscriber")]
    public RawImage mapImage;

    [Tooltip("The robot's base Transform (e.g. base_link or turtlebot3_waffle_pi)")]
    public Transform robotTransform;

    [Header("Map Origin Offset  <- must match ROSNavigator / ROSPathVisualizer")]
    public float offsetX = 5.02f;
    public float offsetZ = -10.02f;

    [Header("Zoom  <- must match MapSubscriber")]
    public float zoom = 2.5f;

    [Header("Marker Appearance")]
    [Tooltip("Diameter of the position dot in UI pixels")]
    public float dotSize     = 16f;

    [Tooltip("Width of the direction arrow in UI pixels")]
    public float arrowWidth  = 5f;

    [Tooltip("Length of the direction arrow in UI pixels")]
    public float arrowLength = 20f;

    public Color dotColor   = new Color(0.95f, 0.15f, 0.15f, 1f);   // red
    public Color arrowColor = new Color(1.00f, 0.55f, 0.00f, 1f);   // orange

    [Tooltip("Optional circular sprite for the dot. Falls back to a square if left empty.")]
    public Sprite dotSprite;

    [Header("ROS")]
    [Tooltip("Topic from which OccupancyGrid metadata is read")]
    public string mapTopic = "/map";

    // ─── OccupancyGrid metadata (populated by ROS callback) ──────────────────

    private float _resolution = 0.05f;
    private float _originX    = 0f;
    private float _originY    = 0f;
    private int   _mapWidth   = 1;
    private int   _mapHeight  = 1;
    private bool  _mapReady   = false;

    // Root RectTransform of the marker (dot + arrow), repositioned each Update()
    private RectTransform _dotRT;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        if (mapImage == null)
        {
            Debug.LogError("[RobotMapMarker] mapImage is not assigned.");
            enabled = false;
            return;
        }

        BuildMarker();

        var ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<OccupancyGridMsg>(mapTopic, OnMapReceived);
    }

    void Update()
    {
        if (!_mapReady || _dotRT == null || robotTransform == null) return;

        // ── 1. Unity world position → ROS map coordinates ────────────────────
        // Same conversion used by ROSNavigator and ROSPathRequester
        Vector3 uPos = robotTransform.position;
        float rosX   =  uPos.z - offsetZ;
        float rosY   = -uPos.x + offsetX;

        // ── 2. ROS map coordinates → normalised texture UV [0, 1] ────────────
        float uvX = (rosX - _originX) / (_resolution * _mapWidth);
        float uvY = (rosY - _originY) / (_resolution * _mapHeight);

        // MapSubscriber flips Y when building the texture:
        //   pixels[(height - 1 - y) * width + x]
        // ROS row 0 (bottom) maps to texture row height-1 (top).
        // Invert uvY to correct for this flip before applying the zoom crop.
        float texV = 1f - uvY;

        // ── 3. Apply zoom crop (uvRect) → normalised screen position [0, 1] ──
        // MapSubscriber sets uvRect = (0.5 - 0.5/zoom, 0.5 - 0.5/zoom, 1/zoom, 1/zoom)
        float uvSize = 1f / zoom;
        float uvMinU = 0.5f - uvSize * 0.5f;
        float uvMinV = 0.5f - uvSize * 0.5f;

        float normU = (uvX - uvMinU) / uvSize;
        float normV = (texV - uvMinV) / uvSize;

        // ── 4. Normalised screen position → anchoredPosition on the RawImage ─
        Rect  rect = mapImage.rectTransform.rect;
        float ax   = (normU - 0.5f) * rect.width;
        float ay   = (normV - 0.5f) * rect.height;

        _dotRT.anchoredPosition = new Vector2(ax, ay);

        // ── 5. Orientation — project the 3D forward vector onto the UV plane ──
        //
        // screen +U (right) proportional to fwd.z
        //   because rosX = uPos.z → dRosX = fwd.z
        // screen +V (up) proportional to fwd.x
        //   because texV = 1 - uvY and rosY = -uPos.x
        //   → dTexV = +fwd.x after the double sign flip
        //
        // Vector2.SignedAngle measures CCW rotation from Vector2.up to screenDir.
        Vector3 fwd       = robotTransform.forward;
        Vector2 screenDir = new Vector2(fwd.z, fwd.x);
        float   angle     = Vector2.SignedAngle(Vector2.up, screenDir);

        _dotRT.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ─── ROS callback ────────────────────────────────────────────────────────

    /// <summary>
    /// Caches the OccupancyGrid metadata needed to project world coordinates
    /// onto the texture. Called every time the map is updated by slam_toolbox.
    /// </summary>
    void OnMapReceived(OccupancyGridMsg msg)
    {
        _resolution = msg.info.resolution;
        _originX    = (float)msg.info.origin.position.x;
        _originY    = (float)msg.info.origin.position.y;
        _mapWidth   = (int)msg.info.width;
        _mapHeight  = (int)msg.info.height;
        _mapReady   = true;
    }

    // ─── UI construction ─────────────────────────────────────────────────────

    /// <summary>
    /// Creates the dot and arrow UI elements at runtime as children of the
    /// map RawImage so they share its coordinate space and zoom crop.
    /// </summary>
    void BuildMarker()
    {
        // Transparent full-size container anchored over the entire RawImage.
        // Children use anchoredPosition relative to the RawImage centre.
        GameObject    container = new GameObject("RobotMarkerContainer");
        container.transform.SetParent(mapImage.rectTransform, false);

        RectTransform cRT = container.AddComponent<RectTransform>();
        cRT.anchorMin     = Vector2.zero;
        cRT.anchorMax     = Vector2.one;
        cRT.offsetMin     = Vector2.zero;
        cRT.offsetMax     = Vector2.zero;

        // ── Position dot (pivot = centre) ────────────────────────────────────
        GameObject dotGO = new GameObject("RobotDot");
        dotGO.transform.SetParent(cRT, false);

        _dotRT           = dotGO.AddComponent<RectTransform>();
        _dotRT.sizeDelta = new Vector2(dotSize, dotSize);
        _dotRT.pivot     = new Vector2(0.5f, 0.5f);

        Image dotImg = dotGO.AddComponent<Image>();
        dotImg.color = dotColor;
        if (dotSprite != null) dotImg.sprite = dotSprite;

        // ── Direction arrow (child of dot, pivot = bottom-centre) ────────────
        // Pivot at the base lets the arrow rotate around the dot centre.
        GameObject    arrowGO = new GameObject("RobotArrow");
        arrowGO.transform.SetParent(dotGO.transform, false);

        RectTransform arRT    = arrowGO.AddComponent<RectTransform>();
        arRT.pivot            = new Vector2(0.5f, 0f);
        arRT.sizeDelta        = new Vector2(arrowWidth, arrowLength);
        arRT.anchoredPosition = new Vector2(0f, dotSize * 0.5f);   // base at dot centre

        Image arrowImg = arrowGO.AddComponent<Image>();
        arrowImg.color = arrowColor;

        // Override sorting so the marker always renders on top of the map texture
        Canvas canvas          = dotGO.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder    = 10;
    }
}