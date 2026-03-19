using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using Unity.Robotics.UrdfImporter.Control;

namespace RosSharp.Control
{
    public enum ControlMode { Keyboard, ROS }

    public class AGVController : MonoBehaviour
    {
        public GameObject wheel1;
        public GameObject wheel2;
        public ControlMode mode = ControlMode.ROS;

        [Header("Base articulation body")]
        public ArticulationBody baseLink;   // assegnalo in Inspector se il root non è questo GameObject

        private ArticulationBody rootBody;
        private ArticulationBody wA1;
        private ArticulationBody wA2;

        public float maxLinearSpeed = 2f;      // m/s
        public float maxRotationalSpeed = 1f;   // rad/s
        public float wheelRadius = 0.033f;      // m
        public float trackWidth = 0.288f;       // m
        public float forceLimit = 10f;
        public float damping = 10f;

        public float speedScale = 0.3f;   // rallenta tutto
        public float turnScale = 0.3f;    // rallenta anche la rotazione

        public float ROSTimeout = 0.5f;
        private float lastCmdReceived = 0f;

        private ROSConnection ros;
        private float rosLinear = 0f;
        private float rosAngular = 0f;

        void Start()
        {
            rootBody = baseLink != null ? baseLink : GetComponentInParent<ArticulationBody>();

            if (rootBody == null)
            {
                Debug.LogError("[AGVController] Nessuna ArticulationBody trovata per il base link.");
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

        void ReceiveROSCmd(TwistMsg cmdVel)
        {
            rosLinear = (float)cmdVel.linear.x;
            rosAngular = (float)cmdVel.angular.z;
            lastCmdReceived = Time.time;

            Debug.Log($"[ROS] Received cmd_vel: linear={rosLinear}, angular={rosAngular}");
        }

        void FixedUpdate()
        {
            if (mode == ControlMode.Keyboard)
            {
                KeyBoardUpdate();
            }
            else if (mode == ControlMode.ROS)
            {
                ROSUpdate();
            }
        }

        private void SetParameters(ArticulationBody joint)
        {
            ArticulationDrive drive = joint.xDrive;
            drive.forceLimit = forceLimit;
            drive.stiffness = 0f;
            drive.damping = damping;
            joint.xDrive = drive;
        }

        private void SetSpeed(ArticulationBody joint, float wheelSpeedDegPerSec)
        {
            ArticulationDrive drive = joint.xDrive;
            drive.targetVelocity = wheelSpeedDegPerSec;
            joint.xDrive = drive;
        }

        private void KeyBoardUpdate()
        {
            float moveDirection = Input.GetAxis("Vertical");
            float turnDirection = Input.GetAxis("Horizontal");

            float inputSpeed = moveDirection * maxLinearSpeed * speedScale;
            float inputRotationSpeed = turnDirection * maxRotationalSpeed * turnScale;

            RobotInput(inputSpeed, inputRotationSpeed);
        }

        private void ROSUpdate()
        {
            if (Time.time - lastCmdReceived > ROSTimeout)
            {
                rosLinear = 0f;
                rosAngular = 0f;
            }

            RobotInput(rosLinear * speedScale, -rosAngular * turnScale);
        }

        private void RobotInput(float speed, float rotSpeed)
        {
            speed = Mathf.Clamp(speed, -maxLinearSpeed, maxLinearSpeed);
            rotSpeed = Mathf.Clamp(rotSpeed, -maxRotationalSpeed, maxRotationalSpeed);

            float leftWheelRad = (speed - (rotSpeed * trackWidth * 0.5f)) / wheelRadius;
            float rightWheelRad = (speed + (rotSpeed * trackWidth * 0.5f)) / wheelRadius;

            SetSpeed(wA1, leftWheelRad * Mathf.Rad2Deg);
            SetSpeed(wA2, rightWheelRad * Mathf.Rad2Deg);

            Vector3 forward = rootBody.transform.forward;
            Vector3 currentLinear = rootBody.linearVelocity;
            rootBody.linearVelocity = new Vector3(forward.x * speed, currentLinear.y, forward.z * speed);

            Vector3 currentAngular = rootBody.angularVelocity;
            rootBody.angularVelocity = new Vector3(currentAngular.x, rotSpeed, currentAngular.z);
        }
    }
}