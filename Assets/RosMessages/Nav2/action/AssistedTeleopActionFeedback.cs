using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class AssistedTeleopActionFeedback : ActionFeedback<AssistedTeleopFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/AssistedTeleopActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public AssistedTeleopActionFeedback() : base()
        {
            this.feedback = new AssistedTeleopFeedback();
        }

        public AssistedTeleopActionFeedback(HeaderMsg header, GoalStatusMsg status, AssistedTeleopFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static AssistedTeleopActionFeedback Deserialize(MessageDeserializer deserializer) => new AssistedTeleopActionFeedback(deserializer);

        AssistedTeleopActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = AssistedTeleopFeedback.Deserialize(deserializer);
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
