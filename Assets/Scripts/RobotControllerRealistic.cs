using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry; // Requires Unity Robotics message packages

[RequireComponent(typeof(ROSConnection))]
public class CmdVelSubscriber : MonoBehaviour
{
    [Header("ROS")]
    public string topicName = "/cmd_vel";
    public float queueSize = 10;

    [Header("Robot geometry")]
    [Tooltip("Raggio della ruota in metri")]
    public float wheelRadius = 0.1f;
    [Tooltip("Distanza tra le ruote (baselength) in metri")]
    public float wheelBase = 0.3f;

    [Header("Wheels (ArticulationBody)")]
    public UnityEngine.ArticulationBody leftWheel;
    public UnityEngine.ArticulationBody rightWheel;
    [Tooltip("Invertire segno della velocità per la ruota sinistra se necessario")]
    public bool invertLeft = false;
    [Tooltip("Invertire segno della velocità per la ruota destra se necessario")]
    public bool invertRight = true;

    [Header("ArticulationDrive defaults")]
    public float driveForceLimit = 1000f;
    public float driveStiffness = 10000f;
    public float driveDamping = 100f;

    [Header("Timeout")]
    [Tooltip("Se non arriva cmd_vel per più di questo tempo (s), ferma le ruote)")]
    public float cmdTimeout = 0.5f;

    // Internal thread-safe storage for the latest command
    private float lastLinear = 0f;   // m/s
    private float lastAngular = 0f;  // rad/s
    private double lastMsgTime = 0.0;
    private readonly object msgLock = new object();

    ROSConnection ros;

    void Start()
    {
        if (leftWheel == null || rightWheel == null)
        {
            Debug.LogError("CmdVelSubscriber: assegna leftWheel e rightWheel in Inspector.");
            enabled = false;
            return;
        }

        ros = ROSConnection.GetOrCreateInstance();
        // Subscribe: callback will be called on ROS thread -> we only store values there (thread-safe)
        ros.Subscribe<TwistMsg>(topicName, TwistCallback);

        // Ensure drive defaults applied initially
        ApplyDriveDefaults(leftWheel);
        ApplyDriveDefaults(rightWheel);
    }

    void TwistCallback(TwistMsg twistMsg)
    {
        // TwistMsg has linear and angular as Vector3Msg
        float linearX = (float)twistMsg.linear.x;
        float angularZ = (float)twistMsg.angular.z;

        lock (msgLock)
        {
            lastLinear = linearX;
            lastAngular = angularZ;
            lastMsgTime = UnityEngine.Time.timeAsDouble;
        }
    }

    void FixedUpdate()
    {
        float linear;
        float angular;
        double msgTime;
        lock (msgLock)
        {
            linear = lastLinear;
            angular = lastAngular;
            msgTime = lastMsgTime;
        }

        // Timeout: if no recent message, set to zero
        if (Time.timeAsDouble - msgTime > cmdTimeout)
        {
            linear = 0f;
            angular = 0f;
        }

        // Differential drive kinematics
        float vLeftLinear = linear - angular * (wheelBase / 2f);
        float vRightLinear = linear + angular * (wheelBase / 2f);

        float wLeftRad = vLeftLinear / Mathf.Max(1e-6f, wheelRadius);
        float wRightRad = vRightLinear / Mathf.Max(1e-6f, wheelRadius);

        float wLeftDeg = wLeftRad * Mathf.Rad2Deg;
        float wRightDeg = wRightRad * Mathf.Rad2Deg;

        if (invertLeft) wLeftDeg = -wLeftDeg;
        if (invertRight) wRightDeg = -wRightDeg;

        SetWheelVelocity(leftWheel, wLeftDeg);
        SetWheelVelocity(rightWheel, wRightDeg);
    }

    void ApplyDriveDefaults(UnityEngine.ArticulationBody wheel)
    {
        var drive = wheel.yDrive;
        drive.forceLimit = driveForceLimit;
        drive.stiffness = driveStiffness;
        drive.damping = driveDamping;
        wheel.yDrive = drive;
    }

    void SetWheelVelocity(UnityEngine.ArticulationBody wheel, float targetVelocityDegPerSec)
    {
        var drive = wheel.yDrive;
        drive.targetVelocity = targetVelocityDegPerSec;
        drive.forceLimit = driveForceLimit;
        drive.stiffness = driveStiffness;
        drive.damping = driveDamping;
        wheel.yDrive = drive;
    }

    void OnDisable()
    {
        if (leftWheel != null) SetWheelVelocity(leftWheel, 0f);
        if (rightWheel != null) SetWheelVelocity(rightWheel, 0f);
    }
}