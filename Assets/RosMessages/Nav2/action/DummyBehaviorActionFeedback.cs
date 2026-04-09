using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class DummyBehaviorActionFeedback : ActionFeedback<DummyBehaviorFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/DummyBehaviorActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public DummyBehaviorActionFeedback() : base()
        {
            this.feedback = new DummyBehaviorFeedback();
        }

        public DummyBehaviorActionFeedback(HeaderMsg header, GoalStatusMsg status, DummyBehaviorFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static DummyBehaviorActionFeedback Deserialize(MessageDeserializer deserializer) => new DummyBehaviorActionFeedback(deserializer);

        DummyBehaviorActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = DummyBehaviorFeedback.Deserialize(deserializer);
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
