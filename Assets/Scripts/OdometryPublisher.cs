using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Nav;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Tf2;

public class OdometryPublisher : MonoBehaviour
{
    [Header("ROS Settings")]
    [SerializeField] private string topicName = "/odom";
    [SerializeField] private string tfTopicName = "/tf";
    [SerializeField] private string frameId = "odom";
    [SerializeField] private string childFrameId = "base_link";

    [Header("Robot frame to track")]
    [SerializeField] private Transform robotFrame;

    private ROSConnection ros;

    private Vector3 startPositionUnity;
    private Quaternion startRotationUnity;

    private Vector3 lastPositionUnity;
    private Quaternion lastRotationUnity;
    private double lastPublishTime;

    private void Start()
    {
        if (robotFrame == null)
            robotFrame = transform;

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<OdometryMsg>(topicName);
        ros.RegisterPublisher<TFMessageMsg>(tfTopicName); 

        startPositionUnity = robotFrame.position;
        startRotationUnity = robotFrame.rotation;

        lastPositionUnity = startPositionUnity;
        lastRotationUnity = startRotationUnity;

        lastPublishTime = Time.time;
    }

    private void FixedUpdate()
    {
        if (robotFrame == null)
            return;

        double now = Time.time;
        double dt = now - lastPublishTime;
        if (dt <= 0.0)
            return;

        Vector3 currentPositionUnity = robotFrame.position;
        Quaternion currentRotationUnity = robotFrame.rotation;

        // Pose relativa all'origine dell'odom
        Vector3 relPosUnity = Quaternion.Inverse(startRotationUnity) * (currentPositionUnity - startPositionUnity);
        Quaternion relRotUnity = Quaternion.Inverse(startRotationUnity) * currentRotationUnity;

        // Velocità lineare nel frame locale
        Vector3 worldDelta = currentPositionUnity - lastPositionUnity;
        Vector3 localDeltaUnity = Quaternion.Inverse(currentRotationUnity) * worldDelta;
        Vector3 localLinearUnity = localDeltaUnity / (float)dt;

        // Velocità angolare (yaw) 2D
        float deltaYawDeg = Mathf.DeltaAngle(lastRotationUnity.eulerAngles.y, currentRotationUnity.eulerAngles.y);
        double yawRate = (deltaYawDeg * Mathf.Deg2Rad) / dt;

        // Conversione Unity -> ROS (FLU)
        Vector3 rosPos = UnityToRosPosition(relPosUnity);
        Vector3 rosLinear = UnityToRosPosition(localLinearUnity);
        //var rosRot = relRotUnity.To<FLU>();
        Quaternion rosReadyRot = new Quaternion(-relRotUnity.z, relRotUnity.x, -relRotUnity.y, relRotUnity.w);
        var rosRot = rosReadyRot;

        // Timestamp ROS — usa il tempo Unix reale, non Time.time
        // Time.time parte da 0 all'avvio di Unity e causa errori TF in Nav2
        long unixMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int  sec     = (int)(unixMs / 1000);
        uint nanosec = (uint)((unixMs % 1000) * 1_000_000);

        // Costruzione header
        var header = new HeaderMsg { frame_id = frameId };
        header.stamp.sec = sec;
        header.stamp.nanosec = nanosec;

        var tfMsg = new TFMessageMsg
        {
            transforms = new TransformStampedMsg[]
            {
                new TransformStampedMsg
                {
                    header = header,
                    child_frame_id = childFrameId,
                    transform = new TransformMsg
                    {
                        translation = new Vector3Msg { x = rosPos.x, y = rosPos.y, z = rosPos.z },
                        rotation = new QuaternionMsg { x = rosRot.x, y = rosRot.y, z = rosRot.z, w = rosRot.w }
                    }
                }
            }
        };
        ros.Publish(tfTopicName, tfMsg);

        // Costruzione messaggio Odometry
        var odom = new OdometryMsg
        {
            header = header,
            child_frame_id = childFrameId,
            pose = new PoseWithCovarianceMsg(),
            twist = new TwistWithCovarianceMsg()
        };

        odom.pose.pose = new PoseMsg
        {
            position = new PointMsg { x = rosPos.x, y = rosPos.y, z = rosPos.z },
            orientation = new QuaternionMsg { x = rosRot.x, y = rosRot.y, z = rosRot.z, w = rosRot.w }
        };
        odom.pose.covariance = MakePoseCovariance();

        odom.twist.twist = new TwistMsg
        {
            linear = new Vector3Msg { x = rosLinear.x, y = rosLinear.y, z = rosLinear.z },
            angular = new Vector3Msg { x = 0.0, y = 0.0, z = yawRate }
        };
        odom.twist.covariance = MakeTwistCovariance();

        ros.Publish(topicName, odom);

        lastPositionUnity = currentPositionUnity;
        lastRotationUnity = currentRotationUnity;
        lastPublishTime = now;
    }

    private static Vector3 UnityToRosPosition(Vector3 v)
    {
        // Unity: x=right, y=up, z=forward
        // ROS FLU: x=forward, y=left, z=up
        return new Vector3(v.z, -v.x, v.y);
    }

    private static double[] MakePoseCovariance()
    {
        var c = new double[36];
        c[0] = 0.01;  c[7] = 0.01;  c[14] = 0.01;
        c[21] = 0.01; c[28] = 0.01; c[35] = 0.05;
        return c;
    }

    private static double[] MakeTwistCovariance()
    {
        var c = new double[36];
        c[0] = 0.1;  c[7] = 0.1;  c[14] = 0.1;
        c[21] = 0.1; c[28] = 0.1; c[35] = 0.2;
        return c;
    }
}