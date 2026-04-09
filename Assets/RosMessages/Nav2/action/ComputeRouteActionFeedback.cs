using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class ComputeRouteActionFeedback : ActionFeedback<ComputeRouteFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputeRouteActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public ComputeRouteActionFeedback() : base()
        {
            this.feedback = new ComputeRouteFeedback();
        }

        public ComputeRouteActionFeedback(HeaderMsg header, GoalStatusMsg status, ComputeRouteFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static ComputeRouteActionFeedback Deserialize(MessageDeserializer deserializer) => new ComputeRouteActionFeedback(deserializer);

        ComputeRouteActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = ComputeRouteFeedback.Deserialize(deserializer);
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
