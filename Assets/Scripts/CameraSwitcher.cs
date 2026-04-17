using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

/// <summary>
/// Switches between the forward and backward camera views based on the
/// robot's linear velocity received on cmd_vel.
///
/// When the robot moves forward the forward camera is shown.
/// When it reverses the backward camera fades in smoothly.
/// The transition is driven by a Lerp on the alpha channel of each RawImage.
/// </summary>
public class CameraSwitcher : MonoBehaviour
{
    [Header("ROS")]
    [Tooltip("Topic from which linear velocity commands are read")]
    public string topicName = "/cmd_vel";

    [Header("UI Raw Images")]
    [Tooltip("RawImage displaying the front camera feed")]
    public RawImage forwardView;

    [Tooltip("RawImage displaying the rear camera feed")]
    public RawImage backwardView;

    [Header("Blend")]
    [Tooltip("Smoothing speed for the alpha transition between cameras")]
    public float smooth = 5f;

    private ROSConnection ros;

    // Target and current blend value:
    // 1 = forward camera fully visible, 0 = backward camera fully visible
    private float targetBlend  = 1f;
    private float currentBlend = 1f;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<TwistMsg>(topicName, OnCmdVel);
    }

    void Update()
    {
        // Smoothly interpolate toward the target blend value
        currentBlend = Mathf.Lerp(currentBlend, targetBlend, Time.deltaTime * smooth);

        SetAlpha(forwardView,  currentBlend);
        SetAlpha(backwardView, 1f - currentBlend);
    }

    // ─── ROS callback ────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the target blend based on the direction of linear motion.
    /// Negative linear.x means the robot is reversing → show rear camera.
    /// </summary>
    void OnCmdVel(TwistMsg msg)
    {
        float linear = (float)msg.linear.x;

        targetBlend = linear < -0.01f ? 0f : 1f;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Sets the alpha channel of a RawImage, clamped to [0, 1].</summary>
    void SetAlpha(RawImage img, float a)
    {
        if (img == null) return;

        Color c = img.color;
        c.a     = Mathf.Clamp01(a);
        img.color = c;
    }
}