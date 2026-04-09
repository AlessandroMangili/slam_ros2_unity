using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class FollowPathActionFeedback : ActionFeedback<FollowPathFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/FollowPathActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public FollowPathActionFeedback() : base()
        {
            this.feedback = new FollowPathFeedback();
        }

        public FollowPathActionFeedback(HeaderMsg header, GoalStatusMsg status, FollowPathFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static FollowPathActionFeedback Deserialize(MessageDeserializer deserializer) => new FollowPathActionFeedback(deserializer);

        FollowPathActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = FollowPathFeedback.Deserialize(deserializer);
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
