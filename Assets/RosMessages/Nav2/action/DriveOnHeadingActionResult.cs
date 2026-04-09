using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class DriveOnHeadingActionResult : ActionResult<DriveOnHeadingResult>
    {
        public const string k_RosMessageName = "nav2_msgs/DriveOnHeadingActionResult";
        public override string RosMessageName => k_RosMessageName;


        public DriveOnHeadingActionResult() : base()
        {
            this.result = new DriveOnHeadingResult();
        }

        public DriveOnHeadingActionResult(HeaderMsg header, GoalStatusMsg status, DriveOnHeadingResult result) : base(header, status)
        {
            this.result = result;
        }
        public static DriveOnHeadingActionResult Deserialize(MessageDeserializer deserializer) => new DriveOnHeadingActionResult(deserializer);

        DriveOnHeadingActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = DriveOnHeadingResult.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.status);
            serializer.Write(this.result);
        }


#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize);
        }
    }
}
