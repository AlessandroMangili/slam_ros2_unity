using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using TMPro;

public class ROSLatencyMonitor : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI latencyLabel;

    [Header("Ping Settings")]
    [Tooltip("Intervallo tra un ping e il successivo (secondi)")]
    public float pingInterval = 0.5f;

    [Tooltip("Secondi senza pong prima di mostrare 'No signal'")]
    public float timeoutSeconds = 2f;

    [Header("Smoothing")]
    [Range(0.05f, 1f)]
    [Tooltip("Peso del nuovo campione nell'EMA (1 = nessuno smoothing)")]
    public float smoothFactor = 0.2f;

    // ─── Privati ─────────────────────────────────────────────────────────────

    private ROSConnection _ros;

    private readonly Stopwatch _sw = new Stopwatch();
    private bool  _waitingForPong = false;

    private float _lastPongTime  = -999f;
    private float _smoothedRttMs = -1f;

    private static readonly Color ColorActive   = new Color(0.90f, 0.90f, 0.90f);
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
        var msg = new HeaderMsg { frame_id = "ping" };
        _sw.Restart();
        _waitingForPong = true;
        _ros.Publish("/unity/ping", msg);
    }

    // ─── Callback pong ───────────────────────────────────────────────────────

    void OnPongReceived(HeaderMsg msg)
    {
        if (!_waitingForPong) return;

        _sw.Stop();
        _waitingForPong = false;

        float rttMs = (float)_sw.Elapsed.TotalMilliseconds;
        if (rttMs < 0f || rttMs > 5000f) return;

        _lastPongTime = Time.time;

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
        float oneWay = rtt * 0.5f;

        latencyLabel.text  = $"● RTT {rtt:0.0}ms  (~{oneWay:0.0}ms/way)";
        latencyLabel.color = ColorActive;
    }
}