using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class SpinActionFeedback : ActionFeedback<SpinFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/SpinActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public SpinActionFeedback() : base()
        {
            this.feedback = new SpinFeedback();
        }

        public SpinActionFeedback(HeaderMsg header, GoalStatusMsg status, SpinFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static SpinActionFeedback Deserialize(MessageDeserializer deserializer) => new SpinActionFeedback(deserializer);

        SpinActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = SpinFeedback.Deserialize(deserializer);
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
