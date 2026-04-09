using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class ComputeAndTrackRouteActionResult : ActionResult<ComputeAndTrackRouteResult>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputeAndTrackRouteActionResult";
        public override string RosMessageName => k_RosMessageName;


        public ComputeAndTrackRouteActionResult() : base()
        {
            this.result = new ComputeAndTrackRouteResult();
        }

        public ComputeAndTrackRouteActionResult(HeaderMsg header, GoalStatusMsg status, ComputeAndTrackRouteResult result) : base(header, status)
        {
            this.result = result;
        }
        public static ComputeAndTrackRouteActionResult Deserialize(MessageDeserializer deserializer) => new ComputeAndTrackRouteActionResult(deserializer);

        ComputeAndTrackRouteActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = ComputeAndTrackRouteResult.Deserialize(deserializer);
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
