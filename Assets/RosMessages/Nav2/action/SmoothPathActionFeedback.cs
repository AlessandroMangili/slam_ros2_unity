using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class SmoothPathActionFeedback : ActionFeedback<SmoothPathFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/SmoothPathActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public SmoothPathActionFeedback() : base()
        {
            this.feedback = new SmoothPathFeedback();
        }

        public SmoothPathActionFeedback(HeaderMsg header, GoalStatusMsg status, SmoothPathFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static SmoothPathActionFeedback Deserialize(MessageDeserializer deserializer) => new SmoothPathActionFeedback(deserializer);

        SmoothPathActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = SmoothPathFeedback.Deserialize(deserializer);
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
