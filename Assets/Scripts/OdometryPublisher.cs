using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Nav;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Tf2;

/// <summary>
/// Publishes the robot's odometry on /odom and the corresponding TF transform
/// (odom → base_link) on /tf at every physics step.
///
/// Pose and velocity are computed from the tracked Transform in Unity space
/// and converted to ROS FLU (Forward-Left-Up) convention before publishing.
///
/// The timestamp uses real Unix time (DateTimeOffset.UtcNow) instead of
/// Time.time because Time.time starts at 0 on Play and would cause TF
/// lookup failures in Nav2.
/// </summary>
public class OdometryPublisher : MonoBehaviour
{
    [Header("ROS Settings")]
    [SerializeField] private string topicName    = "/odom";
    [SerializeField] private string tfTopicName  = "/tf";
    [SerializeField] private string frameId      = "odom";
    [SerializeField] private string childFrameId = "base_link";

    [Header("Robot Frame to Track")]
    [Tooltip("Transform of the robot's base link. Defaults to this GameObject if left empty.")]
    [SerializeField] private Transform robotFrame;

    private ROSConnection ros;

    // Pose at the moment Start() is called — used as the odometry origin
    private Vector3    startPositionUnity;
    private Quaternion startRotationUnity;

    // Pose from the previous FixedUpdate — used to compute velocities
    private Vector3    lastPositionUnity;
    private Quaternion lastRotationUnity;
    private double     lastPublishTime;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    private void Start()
    {
        if (robotFrame == null)
            robotFrame = transform;

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<OdometryMsg>(topicName);
        ros.RegisterPublisher<TFMessageMsg>(tfTopicName);

        // Record the initial pose as the odometry frame origin
        startPositionUnity = robotFrame.position;
        startRotationUnity = robotFrame.rotation;

        lastPositionUnity = startPositionUnity;
        lastRotationUnity = startRotationUnity;
        lastPublishTime   = Time.time;
    }

    private void FixedUpdate()
    {
        if (robotFrame == null) return;

        double now = Time.time;
        double dt  = now - lastPublishTime;
        if (dt <= 0.0) return;

        Vector3    currentPositionUnity = robotFrame.position;
        Quaternion currentRotationUnity = robotFrame.rotation;

        // ── Pose relative to the odometry origin ─────────────────────────────
        Vector3    relPosUnity = Quaternion.Inverse(startRotationUnity) * (currentPositionUnity - startPositionUnity);
        Quaternion relRotUnity = Quaternion.Inverse(startRotationUnity) * currentRotationUnity;

        // ── Linear velocity in the robot's local frame ───────────────────────
        Vector3 worldDelta      = currentPositionUnity - lastPositionUnity;
        Vector3 localDeltaUnity = Quaternion.Inverse(currentRotationUnity) * worldDelta;
        Vector3 localLinearUnity = localDeltaUnity / (float)dt;

        // ── Angular velocity — yaw only (2D robot) ───────────────────────────
        float  deltaYawDeg = Mathf.DeltaAngle(lastRotationUnity.eulerAngles.y, currentRotationUnity.eulerAngles.y);
        double yawRate     = (deltaYawDeg * Mathf.Deg2Rad) / dt;

        // ── Convert Unity → ROS FLU ──────────────────────────────────────────
        // Position and linear velocity: swap axes (Unity x→ROS y-neg, Unity z→ROS x, Unity y→ROS z)
        Vector3 rosPos    = UnityToRosPosition(relPosUnity);
        Vector3 rosLinear = UnityToRosPosition(localLinearUnity);

        // Rotation: manual component remap to match FLU convention
        // (equivalent to relRotUnity.To<FLU>() but avoids the extension method)
        Quaternion rosRot = new Quaternion(-relRotUnity.z, relRotUnity.x, -relRotUnity.y, relRotUnity.w);

        // ── Timestamp — real Unix time in seconds + nanoseconds ──────────────
        long unixMs  = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int  sec     = (int)(unixMs / 1000);
        uint nanosec = (uint)((unixMs % 1000) * 1_000_000);

        // ── Build shared header ───────────────────────────────────────────────
        var header = new HeaderMsg { frame_id = frameId };
        header.stamp.sec     = sec;
        header.stamp.nanosec = nanosec;

        // ── Publish TF transform (odom → base_link) ───────────────────────────
        var tfMsg = new TFMessageMsg
        {
            transforms = new TransformStampedMsg[]
            {
                new TransformStampedMsg
                {
                    header         = header,
                    child_frame_id = childFrameId,
                    transform      = new TransformMsg
                    {
                        translation = new Vector3Msg   { x = rosPos.x, y = rosPos.y, z = rosPos.z },
                        rotation    = new QuaternionMsg { x = rosRot.x, y = rosRot.y, z = rosRot.z, w = rosRot.w }
                    }
                }
            }
        };
        ros.Publish(tfTopicName, tfMsg);

        // ── Publish Odometry message ──────────────────────────────────────────
        var odom = new OdometryMsg
        {
            header         = header,
            child_frame_id = childFrameId,
            pose           = new PoseWithCovarianceMsg(),
            twist          = new TwistWithCovarianceMsg()
        };

        odom.pose.pose = new PoseMsg
        {
            position    = new PointMsg    { x = rosPos.x, y = rosPos.y, z = rosPos.z },
            orientation = new QuaternionMsg { x = rosRot.x, y = rosRot.y, z = rosRot.z, w = rosRot.w }
        };
        odom.pose.covariance = MakePoseCovariance();

        odom.twist.twist = new TwistMsg
        {
            linear  = new Vector3Msg { x = rosLinear.x, y = rosLinear.y, z = rosLinear.z },
            angular = new Vector3Msg { x = 0.0, y = 0.0, z = yawRate }
        };
        odom.twist.covariance = MakeTwistCovariance();

        ros.Publish(topicName, odom);

        // ── Store current state for next step ─────────────────────────────────
        lastPositionUnity = currentPositionUnity;
        lastRotationUnity = currentRotationUnity;
        lastPublishTime   = now;
    }

    // ─── Coordinate conversion ───────────────────────────────────────────────

    /// <summary>
    /// Converts a position vector from Unity space to ROS FLU space.
    /// Unity: x = right,   y = up,   z = forward
    /// ROS:   x = forward, y = left, z = up
    /// </summary>
    private static Vector3 UnityToRosPosition(Vector3 v)
    {
        return new Vector3(v.z, -v.x, v.y);
    }

    // ─── Covariance matrices ─────────────────────────────────────────────────

    /// <summary>
    /// Returns a 6×6 diagonal pose covariance matrix (row-major, 36 elements).
    /// Low values reflect high confidence in the simulated pose.
    /// </summary>
    private static double[] MakePoseCovariance()
    {
        var c = new double[36];
        c[0]  = 0.01;   // x
        c[7]  = 0.01;   // y
        c[14] = 0.01;   // z
        c[21] = 0.01;   // roll
        c[28] = 0.01;   // pitch
        c[35] = 0.05;   // yaw
        return c;
    }

    /// <summary>
    /// Returns a 6×6 diagonal twist covariance matrix (row-major, 36 elements).
    /// Slightly higher values than pose to account for velocity estimation noise.
    /// </summary>
    private static double[] MakeTwistCovariance()
    {
        var c = new double[36];
        c[0]  = 0.1;    // vx
        c[7]  = 0.1;    // vy
        c[14] = 0.1;    // vz
        c[21] = 0.1;    // wx
        c[28] = 0.1;    // wy
        c[35] = 0.2;    // wz (yaw rate — slightly higher uncertainty)
        return c;
    }
}