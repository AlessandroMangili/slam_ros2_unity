using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using TMPro;

/// <summary>
/// Misura la latenza Round-Trip Time (RTT) tra Unity e ROS2
/// tramite un meccanismo ping/pong dedicato.
///
/// Usa System.Diagnostics.Stopwatch (performance counter hardware, precisione
/// sub-microsecondo) per misurare il tempo esclusivamente lato Unity,
/// senza dipendere dal clock di sistema né dalla sincronizzazione degli orologi.
///
/// Flusso:
///   Unity  →  /unity/ping  →  bridge Python  →  /unity/pong  →  Unity
///   Stopwatch.Restart() al send, Stopwatch.Stop() al receive → RTT esatto.
///
/// Richiede path_bridge.py con supporto ping/pong (on_ping fa eco immediato).
///
/// Setup:
///   1. Aggiungi questo script su un GameObject nel Canvas.
///   2. Assegna latencyLabel → TextMeshProUGUI.
/// </summary>
public class ROSLatencyMonitor : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────────────────────

    [Header("UI")]
    public TextMeshProUGUI latencyLabel;

    [Header("Ping Settings")]
    [Tooltip("Intervallo tra un ping e il successivo (secondi)")]
    public float pingInterval = 0.5f;

    [Tooltip("Secondi senza pong prima di mostrare 'No signal'")]
    public float timeoutSeconds = 2f;

    [Header("Soglie colore RTT (ms)")]
    [Tooltip("Sotto questa soglia → verde (good)")]
    public float greenThreshold  = 20f;

    [Tooltip("Sotto questa soglia → giallo (fair)  |  sopra → rosso (poor)")]
    public float yellowThreshold = 60f;

    [Header("Smoothing")]
    [Range(0.05f, 1f)]
    [Tooltip("Peso del nuovo campione nell'EMA (1 = nessuno smoothing)")]
    public float smoothFactor = 0.2f;

    // ─── Privati ─────────────────────────────────────────────────────────────

    private ROSConnection _ros;

    // Performance counter hardware: risoluzione < 1 µs, indipendente dal clock OS.
    private readonly Stopwatch _sw = new Stopwatch();
    private bool _waitingForPong = false;

    private float _lastPongTime  = -999f;
    private float _smoothedRttMs = -1f;   // -1 = nessun campione ancora

    private static readonly Color ColorGood     = new Color(0.20f, 0.90f, 0.30f);
    private static readonly Color ColorMedium   = new Color(1.00f, 0.85f, 0.00f);
    private static readonly Color ColorBad      = new Color(1.00f, 0.25f, 0.20f);
    private static readonly Color ColorNoSignal = new Color(0.55f, 0.55f, 0.55f);

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        _ros = ROSConnection.GetOrCreateInstance();
        _ros.RegisterPublisher<HeaderMsg>("/unity/ping");
        _ros.Subscribe<HeaderMsg>("/unity/pong", OnPongReceived);

        if (latencyLabel != null)
        {
            latencyLabel.text  = "ROS: connecting...";
            latencyLabel.color = ColorNoSignal;
        }

        StartCoroutine(PingLoop());
    }

    void Update()
    {
        UpdateLabel();
    }

    // ─── Ping loop ───────────────────────────────────────────────────────────

    IEnumerator PingLoop()
    {
        var wait = new WaitForSeconds(pingInterval);
        while (true)
        {
            SendPing();
            yield return wait;
        }
    }

    void SendPing()
    {
        // Il contenuto del messaggio non è rilevante per la misura:
        // il tempo è interamente gestito dallo Stopwatch locale.
        var msg = new HeaderMsg { frame_id = "ping" };

        _sw.Restart();          // azzera e avvia il cronometro hardware
        _waitingForPong = true;
        _ros.Publish("/unity/ping", msg);
    }

    // ─── Callback pong ───────────────────────────────────────────────────────

    // Questo callback arriva su un thread separato (ROS-TCP-Connector).
    // Usiamo solo tipi primitivi per evitare race conditions senza lock.
    void OnPongReceived(HeaderMsg msg)
    {
        if (!_waitingForPong) return;

        _sw.Stop();
        _waitingForPong = false;

        float rttMs = (float)_sw.Elapsed.TotalMilliseconds;

        if (rttMs < 0f || rttMs > 5000f) return;   // sanity check

        _lastPongTime = Time.time;

        // Exponential Moving Average: smorza i picchi occasionali
        _smoothedRttMs = _smoothedRttMs < 0f
            ? rttMs
            : _smoothedRttMs * (1f - smoothFactor) + rttMs * smoothFactor;
    }

    // ─── Label ───────────────────────────────────────────────────────────────

    void UpdateLabel()
    {
        if (latencyLabel == null) return;

        bool timedOut = (Time.time - _lastPongTime) > timeoutSeconds;

        if (timedOut || _smoothedRttMs < 0f)
        {
            latencyLabel.text  = "● ROS: no signal";
            latencyLabel.color = ColorNoSignal;
            return;
        }

        float rtt    = _smoothedRttMs;

        Color  c;
        string quality;

        if (rtt < greenThreshold)
        {
            c       = ColorGood;
            quality = "good";
        }
        else if (rtt < yellowThreshold)
        {
            c       = ColorMedium;
            quality = "fair";
        }
        else
        {
            c       = ColorBad;
            quality = "poor";
        }

        latencyLabel.text  = $"● RTT {rtt:0.0}ms  {quality}";
        latencyLabel.color = c;
    }
}