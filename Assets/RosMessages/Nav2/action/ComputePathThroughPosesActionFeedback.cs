using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class ComputePathThroughPosesActionFeedback : ActionFeedback<ComputePathThroughPosesFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputePathThroughPosesActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public ComputePathThroughPosesActionFeedback() : base()
        {
            this.feedback = new ComputePathThroughPosesFeedback();
        }

        public ComputePathThroughPosesActionFeedback(HeaderMsg header, GoalStatusMsg status, ComputePathThroughPosesFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static ComputePathThroughPosesActionFeedback Deserialize(MessageDeserializer deserializer) => new ComputePathThroughPosesActionFeedback(deserializer);

        ComputePathThroughPosesActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = ComputePathThroughPosesFeedback.Deserialize(deserializer);
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
