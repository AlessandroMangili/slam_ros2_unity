using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using TMPro;

/// <summary>
/// Measures the Round-Trip Time (RTT) between Unity and ROS2 using a
/// dedicated ping/pong mechanism and displays it on a UI label.
///
/// A HeaderMsg is published on /unity/ping at a fixed interval.
/// path_bridge.py echoes it back immediately on /unity/pong.
/// Unity measures the elapsed time with a Stopwatch (hardware performance
/// counter, sub-microsecond resolution) — no clock synchronisation needed.
///
/// The one-way latency estimate is RTT / 2, which is accurate for
/// symmetric connections (loopback or local LAN).
///
/// Smoothing is applied via an Exponential Moving Average (EMA) to reduce
/// the visual impact of occasional spikes.
///
/// Requires path_bridge.py to be running with ping/pong support.
///
/// Setup:
///   1. Add this script to any GameObject in the Canvas.
///   2. Assign latencyLabel → TextMeshProUGUI.
/// </summary>
public class ROSLatencyMonitor : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI latencyLabel;

    [Header("Ping Settings")]
    [Tooltip("Interval between consecutive pings in seconds")]
    public float pingInterval = 0.5f;

    [Tooltip("Seconds without a pong response before showing 'No signal'")]
    public float timeoutSeconds = 2f;

    [Header("Smoothing")]
    [Range(0.05f, 1f)]
    [Tooltip("Weight given to each new sample in the EMA (1 = no smoothing)")]
    public float smoothFactor = 0.2f;

    // ─── Private ─────────────────────────────────────────────────────────────

    private ROSConnection _ros;

    // Hardware performance counter — sub-microsecond resolution,
    // independent of the OS system clock (unlike DateTimeOffset.UtcNow
    // which has ~15 ms granularity on Windows/Mono).
    private readonly Stopwatch _sw = new Stopwatch();
    private bool _waitingForPong = false;

    private float _lastPongTime  = -999f;
    private float _smoothedRttMs = -1f;     // -1 = no sample yet

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
        // Message content is irrelevant — the Stopwatch measures all elapsed time locally.
        var msg = new HeaderMsg { frame_id = "ping" };

        _sw.Restart();          // reset and start the hardware counter
        _waitingForPong = true;
        _ros.Publish("/unity/ping", msg);
    }

    // ─── Pong callback ───────────────────────────────────────────────────────

    // Called on the ROS-TCP-Connector receive thread.
    // Only primitive types are written to avoid race conditions without locks.
    void OnPongReceived(HeaderMsg msg)
    {
        if (!_waitingForPong) return;

        _sw.Stop();
        _waitingForPong = false;

        float rttMs = (float)_sw.Elapsed.TotalMilliseconds;

        if (rttMs < 0f || rttMs > 5000f) return;   // discard anomalous readings

        _lastPongTime = Time.time;

        // Exponential Moving Average — smooths out occasional spikes
        _smoothedRttMs = _smoothedRttMs < 0f
            ? rttMs
            : _smoothedRttMs * (1f - smoothFactor) + rttMs * smoothFactor;
    }

    // ─── Label update ────────────────────────────────────────────────────────

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