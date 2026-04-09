using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class ComputeAndTrackRouteActionFeedback : ActionFeedback<ComputeAndTrackRouteFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputeAndTrackRouteActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public ComputeAndTrackRouteActionFeedback() : base()
        {
            this.feedback = new ComputeAndTrackRouteFeedback();
        }

        public ComputeAndTrackRouteActionFeedback(HeaderMsg header, GoalStatusMsg status, ComputeAndTrackRouteFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static ComputeAndTrackRouteActionFeedback Deserialize(MessageDeserializer deserializer) => new ComputeAndTrackRouteActionFeedback(deserializer);

        ComputeAndTrackRouteActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = ComputeAndTrackRouteFeedback.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.status);
            serializer.Write(this.feedback);
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
