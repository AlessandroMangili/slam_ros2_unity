using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class NavigateThroughPosesActionFeedback : ActionFeedback<NavigateThroughPosesFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/NavigateThroughPosesActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public NavigateThroughPosesActionFeedback() : base()
        {
            this.feedback = new NavigateThroughPosesFeedback();
        }

        public NavigateThroughPosesActionFeedback(HeaderMsg header, GoalStatusMsg status, NavigateThroughPosesFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static NavigateThroughPosesActionFeedback Deserialize(MessageDeserializer deserializer) => new NavigateThroughPosesActionFeedback(deserializer);

        NavigateThroughPosesActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = NavigateThroughPosesFeedback.Deserialize(deserializer);
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
