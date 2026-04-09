using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class ComputeRouteActionResult : ActionResult<ComputeRouteResult>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputeRouteActionResult";
        public override string RosMessageName => k_RosMessageName;


        public ComputeRouteActionResult() : base()
        {
            this.result = new ComputeRouteResult();
        }

        public ComputeRouteActionResult(HeaderMsg header, GoalStatusMsg status, ComputeRouteResult result) : base(header, status)
        {
            this.result = result;
        }
        public static ComputeRouteActionResult Deserialize(MessageDeserializer deserializer) => new ComputeRouteActionResult(deserializer);

        ComputeRouteActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = ComputeRouteResult.Deserialize(deserializer);
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
