using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class ComputePathToPoseActionFeedback : ActionFeedback<ComputePathToPoseFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputePathToPoseActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public ComputePathToPoseActionFeedback() : base()
        {
            this.feedback = new ComputePathToPoseFeedback();
        }

        public ComputePathToPoseActionFeedback(HeaderMsg header, GoalStatusMsg status, ComputePathToPoseFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static ComputePathToPoseActionFeedback Deserialize(MessageDeserializer deserializer) => new ComputePathToPoseActionFeedback(deserializer);

        ComputePathToPoseActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = ComputePathToPoseFeedback.Deserialize(deserializer);
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
