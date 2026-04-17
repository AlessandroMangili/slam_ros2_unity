using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using Unity.Robotics.UrdfImporter.Control;

namespace RosSharp.Control
{
    public enum ControlMode { Keyboard, ROS }

    /// <summary>
    /// Controls the differential-drive robot (TurtleBot3) via keyboard input
    /// or ROS2 commands received on the cmd_vel topic.
    ///
    /// In ROS mode, incoming TwistMsg commands are converted to individual
    /// wheel velocities using differential kinematics.
    /// If no cmd_vel message is received within ROSTimeout seconds, the robot stops.
    /// </summary>
    public class AGVController : MonoBehaviour
    {
        [Header("Wheels")]
        public GameObject wheel1;
        public GameObject wheel2;

        [Header("Control Mode")]
        public ControlMode mode = ControlMode.ROS;

        [Header("Base Articulation Body")]
        [Tooltip("ArticulationBody of the base link. If null, it will be searched in the parent hierarchy.")]
        public ArticulationBody baseLink;

        // ArticulationBody references resolved at runtime
        private ArticulationBody rootBody;
        private ArticulationBody wA1;
        private ArticulationBody wA2;

        [Header("Robot Parameters")]
        [Tooltip("Maximum linear speed in m/s")]
        public float maxLinearSpeed = 2f;

        [Tooltip("Maximum rotational speed in rad/s")]
        public float maxRotationalSpeed = 1f;

        [Tooltip("Wheel radius in metres")]
        public float wheelRadius = 0.033f;

        [Tooltip("Distance between wheels (track width) in metres")]
        public float trackWidth = 0.288f;

        [Header("Articulation Drive")]
        public float forceLimit = 10f;
        public float damping    = 10f;

        [Header("Speed Scaling")]
        [Tooltip("Scales linear speed (1 = no reduction)")]
        public float speedScale = 0.3f;

        [Tooltip("Scales rotational speed (1 = no reduction)")]
        public float turnScale  = 0.3f;

        [Header("ROS")]
        [Tooltip("Seconds without cmd_vel before the robot is stopped")]
        public float ROSTimeout = 1.3f;

        private float lastCmdReceived = 0f;
        private ROSConnection ros;

        // Last values received from cmd_vel
        private float rosLinear  = 0f;
        private float rosAngular = 0f;

        // ─── Unity lifecycle ─────────────────────────────────────────────────

        void Start()
        {
            // Resolve the base link: use the one assigned in the Inspector,
            // otherwise walk up the hierarchy to find it
            rootBody = baseLink != null ? baseLink : GetComponentInParent<ArticulationBody>();

            if (rootBody == null)
            {
                Debug.LogError("[AGVController] No ArticulationBody found for the base link.");
                enabled = false;
                return;
            }

            wA1 = wheel1.GetComponent<ArticulationBody>();
            wA2 = wheel2.GetComponent<ArticulationBody>();

            SetParameters(wA1);
            SetParameters(wA2);

            ros = ROSConnection.GetOrCreateInstance();
            ros.Subscribe<TwistMsg>("cmd_vel", ReceiveROSCmd);
        }

        void FixedUpdate()
        {
            if (mode == ControlMode.Keyboard)
                KeyBoardUpdate();
            else if (mode == ControlMode.ROS)
                ROSUpdate();
        }

        // ─── ROS callback ────────────────────────────────────────────────────

        /// <summary>
        /// Receives velocity commands from Nav2 or teleop on /cmd_vel.
        /// Called on a separate thread by the ROS-TCP-Connector.
        /// </summary>
        void ReceiveROSCmd(TwistMsg cmdVel)
        {
            rosLinear       = (float)cmdVel.linear.x;
            rosAngular      = (float)cmdVel.angular.z;
            lastCmdReceived = Time.time;
        }

        // ─── Input handlers ──────────────────────────────────────────────────

        private void KeyBoardUpdate()
        {
            float inputSpeed    = Input.GetAxis("Vertical")    * maxLinearSpeed     * speedScale;
            float inputRotSpeed = Input.GetAxis("Horizontal")  * maxRotationalSpeed * turnScale;
            RobotInput(inputSpeed, inputRotSpeed);
        }

        private void ROSUpdate()
        {
            // Stop the robot if no cmd_vel has been received within the timeout window
            if (Time.time - lastCmdReceived > ROSTimeout)
            {
                rosLinear  = 0f;
                rosAngular = 0f;
            }

            RobotInput(rosLinear * speedScale, -rosAngular * turnScale);
        }

        // ─── Articulation drive helpers ──────────────────────────────────────

        /// <summary>Sets force limit and damping on the wheel drive.</summary>
        private void SetParameters(ArticulationBody joint)
        {
            ArticulationDrive drive = joint.xDrive;
            drive.forceLimit = forceLimit;
            drive.stiffness  = 0f;
            drive.damping    = damping;
            joint.xDrive     = drive;
        }

        /// <summary>Sets the drive target velocity in degrees per second.</summary>
        private void SetSpeed(ArticulationBody joint, float wheelSpeedDegPerSec)
        {
            ArticulationDrive drive  = joint.xDrive;
            drive.targetVelocity     = wheelSpeedDegPerSec;
            joint.xDrive             = drive;
        }

        // ─── Differential kinematics ─────────────────────────────────────────

        /// <summary>
        /// Converts the robot's linear and angular velocity into individual
        /// wheel velocities using differential drive kinematics, then applies
        /// the velocity directly to the root ArticulationBody to keep Unity
        /// physics consistent.
        ///
        /// Formulas:
        ///   v_left  = (v - ω * d/2) / r
        ///   v_right = (v + ω * d/2) / r
        /// where d = trackWidth, r = wheelRadius, ω = rotSpeed.
        /// </summary>
        private void RobotInput(float speed, float rotSpeed)
        {
            speed    = Mathf.Clamp(speed,    -maxLinearSpeed,     maxLinearSpeed);
            rotSpeed = Mathf.Clamp(rotSpeed, -maxRotationalSpeed, maxRotationalSpeed);

            // Differential kinematics → wheel speeds in rad/s → convert to deg/s
            float leftWheelRad  = (speed - (rotSpeed * trackWidth * 0.5f)) / wheelRadius;
            float rightWheelRad = (speed + (rotSpeed * trackWidth * 0.5f)) / wheelRadius;

            SetSpeed(wA1, leftWheelRad  * Mathf.Rad2Deg);
            SetSpeed(wA2, rightWheelRad * Mathf.Rad2Deg);

            // Apply linear velocity along the robot's forward axis
            Vector3 forward       = rootBody.transform.forward;
            Vector3 currentLinear = rootBody.linearVelocity;
            rootBody.linearVelocity = new Vector3(forward.x * speed, currentLinear.y, forward.z * speed);

            // Apply angular velocity around the Y axis (yaw)
            Vector3 currentAngular   = rootBody.angularVelocity;
            rootBody.angularVelocity = new Vector3(currentAngular.x, rotSpeed, currentAngular.z);
        }
    }
}