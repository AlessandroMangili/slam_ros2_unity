using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

public class CameraSwitcher : MonoBehaviour
{
    [Header("ROS")]
    public string topicName = "/cmd_vel";

    [Header("UI Raw Images")]
    public RawImage forwardView;
    public RawImage backwardView;

    [Header("Blend")]
    public float smooth = 5f;

    private ROSConnection ros;

    private float targetBlend = 1f;
    private float currentBlend = 1f;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<TwistMsg>(topicName, OnCmdVel);
    }

    void Update()
    {
        currentBlend = Mathf.Lerp(currentBlend, targetBlend, Time.deltaTime * smooth);

        SetAlpha(forwardView, currentBlend);
        SetAlpha(backwardView, 1f - currentBlend);
    }

    void OnCmdVel(TwistMsg msg)
    {
        // ROS → Unity fix (double → float)
        float linear = (float)msg.linear.x; 
        // oppure z se serve:
        // float linear = (float)msg.linear.z;

        if (linear < -0.01f)
            targetBlend = 0f;   // indietro
        else
            targetBlend = 1f;   // avanti
    }

    void SetAlpha(RawImage img, float a)
    {
        if (img == null) return;

        Color c = img.color;
        c.a = Mathf.Clamp01(a);
        img.color = c;
    }
}