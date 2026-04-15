using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;

/// <summary>
/// Visualizza la posizione e l'orientamento del robot sulla minimappa.
/// Aggiunge automaticamente a runtime un punto rosso + freccia arancione
/// come figli del RawImage della mappa.
///
/// Setup:
///  1. Aggiungi questo script su qualsiasi GameObject (es. MapBorder o il RawImage stesso).
///  2. Assegna mapImage   → il RawImage usato da MapSubscriber.
///  3. Assegna robotTransform → il Transform del turtlebot (es. base_link).
///  4. Imposta offsetX / offsetZ / zoom con gli stessi valori usati in
///     MapSubscriber, ROSNavigator e ROSPathVisualizer.
/// </summary>
public class RobotMapMarker : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Il RawImage della mappa (stesso di MapSubscriber)")]
    public RawImage mapImage;

    [Tooltip("Il Transform del robot nella scena (es. base_link o turtlebot3_waffle_pi)")]
    public Transform robotTransform;

    [Header("Map Offset  ← stessi valori di ROSNavigator / ROSPathVisualizer")]
    public float offsetX = 5.02f;
    public float offsetZ = -10.02f;

    [Header("Zoom  ← stesso valore di MapSubscriber")]
    public float zoom = 2.5f;

    [Header("Marker Appearance")]
    [Tooltip("Diametro del cerchio rosso in pixel UI")]
    public float dotSize = 16f;

    [Tooltip("Larghezza e lunghezza della freccia di direzione")]
    public float arrowWidth  = 5f;
    public float arrowLength = 20f;

    public Color dotColor   = new Color(0.95f, 0.15f, 0.15f, 1f);  // rosso
    public Color arrowColor = new Color(1.00f, 0.55f, 0.00f, 1f);  // arancione

    [Tooltip("Sprite circolare per il dot (opzionale, altrimenti quadrato)")]
    public Sprite dotSprite;

    [Header("ROS")]
    [Tooltip("Topic da cui leggere i metadati della mappa")]
    public string mapTopic = "/map";

    // ─── Privati ─────────────────────────────────────────────────────────────

    // Metadati della OccupancyGrid
    private float _resolution = 0.05f;
    private float _originX    = 0f;
    private float _originY    = 0f;
    private int   _mapWidth   = 1;
    private int   _mapHeight  = 1;
    private bool  _mapReady   = false;

    // Elementi UI creati a runtime
    private RectTransform _dotRT;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        if (mapImage == null)
        {
            Debug.LogError("[RobotMapMarker] mapImage non assegnato!");
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

        // ── 1. Posizione Unity → coordinate ROS map ──────────────────────────
        Vector3 uPos = robotTransform.position;
        float rosX =  uPos.z - offsetZ;   // stesso calcolo di ROSNavigator
        float rosY = -uPos.x + offsetX;

        // ── 2. ROS map coords → UV textura [0,1] ─────────────────────────────
        float uvX = (rosX - _originX) / (_resolution * _mapWidth);
        float uvY = (rosY - _originY) / (_resolution * _mapHeight);

        // MapSubscriber flips Y: pixels[(height-1-y)*width+x]
        // → cella y=0 (bottom ROS) finisce in row height-1 (top texture)
        // → per tornare "su" in display dobbiamo invertire
        float texV = 1f - uvY;

        // ── 3. Crop zoom (uvRect) → normalizzato [0,1] nello schermo ─────────
        // uvRect = (0.5 - 0.5/zoom,  0.5 - 0.5/zoom,  1/zoom, 1/zoom)
        float uvSize = 1f / zoom;
        float uvMinU = 0.5f - uvSize * 0.5f;
        float uvMinV = 0.5f - uvSize * 0.5f;

        float normU = (uvX - uvMinU) / uvSize;
        float normV = (texV - uvMinV) / uvSize;

        // ── 4. Normalizzato → anchoredPosition sul RawImage ──────────────────
        Rect rect = mapImage.rectTransform.rect;
        float ax = (normU - 0.5f) * rect.width;
        float ay = (normV - 0.5f) * rect.height;

        _dotRT.anchoredPosition = new Vector2(ax, ay);

        // ── 5. Orientamento: mappa il forward 3-D sul piano UV ───────────────
        //
        // Conversione forward Unity → componenti "schermo":
        //   screen +U (destra)  ∝  fwd.z   (perché rosX = uPos.z → dRosX = fwd.z)
        //   screen +V (su)      ∝  fwd.x   (perché texV = 1-uvY e rosY = -uPos.x
        //                                   → dTexV = +fwd.x dopo doppio cambio segno)
        //
        // Vector2.SignedAngle ruota CCW da Vector2.up verso la direzione target.
        Vector3 fwd   = robotTransform.forward;
        Vector2 screenDir = new Vector2(fwd.z, fwd.x);
        float   angle     = Vector2.SignedAngle(Vector2.up, screenDir);

        _dotRT.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ─── Callback ROS ────────────────────────────────────────────────────────

    void OnMapReceived(OccupancyGridMsg msg)
    {
        _resolution = msg.info.resolution;
        _originX    = (float)msg.info.origin.position.x;
        _originY    = (float)msg.info.origin.position.y;
        _mapWidth   = (int)msg.info.width;
        _mapHeight  = (int)msg.info.height;
        _mapReady   = true;
    }

    // ─── Costruzione UI ──────────────────────────────────────────────────────

    void BuildMarker()
    {
        // Container trasparente che copre esattamente il RawImage
        // → tutti i figli usano le stesse coordinate del RawImage
        GameObject container = new GameObject("RobotMarkerContainer");
        container.transform.SetParent(mapImage.rectTransform, false);

        RectTransform cRT = container.AddComponent<RectTransform>();
        cRT.anchorMin  = Vector2.zero;
        cRT.anchorMax  = Vector2.one;
        cRT.offsetMin  = Vector2.zero;
        cRT.offsetMax  = Vector2.zero;

        // ── Dot (pivot = center) ─────────────────────────────────────────────
        GameObject dotGO = new GameObject("RobotDot");
        dotGO.transform.SetParent(cRT, false);

        _dotRT             = dotGO.AddComponent<RectTransform>();
        _dotRT.sizeDelta   = new Vector2(dotSize, dotSize);
        _dotRT.pivot       = new Vector2(0.5f, 0.5f);

        Image dotImg   = dotGO.AddComponent<Image>();
        dotImg.color   = dotColor;
        if (dotSprite != null)
            dotImg.sprite = dotSprite;

        // ── Freccia di orientamento (figlia del dot, pivot = bottom-center) ──
        GameObject arrowGO = new GameObject("RobotArrow");
        arrowGO.transform.SetParent(dotGO.transform, false);

        RectTransform arRT  = arrowGO.AddComponent<RectTransform>();
        arRT.pivot          = new Vector2(0.5f, 0f);         // ruota attorno alla base
        arRT.sizeDelta      = new Vector2(arrowWidth, arrowLength);
        // Posiziona la base della freccia al centro del dot, puntando su
        arRT.anchoredPosition = new Vector2(0f, dotSize * 0.5f);

        Image arrowImg  = arrowGO.AddComponent<Image>();
        arrowImg.color  = arrowColor;

        // Layer sorting: assicura che il marker sia sopra la mappa
        Canvas canvas = dotGO.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder    = 10;
    }
}