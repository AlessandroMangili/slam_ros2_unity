using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class ComputePathThroughPosesActionResult : ActionResult<ComputePathThroughPosesResult>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputePathThroughPosesActionResult";
        public override string RosMessageName => k_RosMessageName;


        public ComputePathThroughPosesActionResult() : base()
        {
            this.result = new ComputePathThroughPosesResult();
        }

        public ComputePathThroughPosesActionResult(HeaderMsg header, GoalStatusMsg status, ComputePathThroughPosesResult result) : base(header, status)
        {
            this.result = result;
        }
        public static ComputePathThroughPosesActionResult Deserialize(MessageDeserializer deserializer) => new ComputePathThroughPosesActionResult(deserializer);

        ComputePathThroughPosesActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = ComputePathThroughPosesResult.Deserialize(deserializer);
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
