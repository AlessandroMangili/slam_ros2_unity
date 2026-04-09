using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class WaitActionFeedback : ActionFeedback<WaitFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/WaitActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public WaitActionFeedback() : base()
        {
            this.feedback = new WaitFeedback();
        }

        public WaitActionFeedback(HeaderMsg header, GoalStatusMsg status, WaitFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static WaitActionFeedback Deserialize(MessageDeserializer deserializer) => new WaitActionFeedback(deserializer);

        WaitActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = WaitFeedback.Deserialize(deserializer);
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
