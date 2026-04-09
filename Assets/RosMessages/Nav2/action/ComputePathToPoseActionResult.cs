using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class ComputePathToPoseActionResult : ActionResult<ComputePathToPoseResult>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputePathToPoseActionResult";
        public override string RosMessageName => k_RosMessageName;


        public ComputePathToPoseActionResult() : base()
        {
            this.result = new ComputePathToPoseResult();
        }

        public ComputePathToPoseActionResult(HeaderMsg header, GoalStatusMsg status, ComputePathToPoseResult result) : base(header, status)
        {
            this.result = result;
        }
        public static ComputePathToPoseActionResult Deserialize(MessageDeserializer deserializer) => new ComputePathToPoseActionResult(deserializer);

        ComputePathToPoseActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = ComputePathToPoseResult.Deserialize(deserializer);
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
