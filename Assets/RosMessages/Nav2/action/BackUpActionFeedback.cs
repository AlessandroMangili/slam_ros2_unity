using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class BackUpActionFeedback : ActionFeedback<BackUpFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/BackUpActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public BackUpActionFeedback() : base()
        {
            this.feedback = new BackUpFeedback();
        }

        public BackUpActionFeedback(HeaderMsg header, GoalStatusMsg status, BackUpFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static BackUpActionFeedback Deserialize(MessageDeserializer deserializer) => new BackUpActionFeedback(deserializer);

        BackUpActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = BackUpFeedback.Deserialize(deserializer);
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
