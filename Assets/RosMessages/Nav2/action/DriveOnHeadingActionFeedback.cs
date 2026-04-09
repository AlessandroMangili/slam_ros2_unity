using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class DriveOnHeadingActionFeedback : ActionFeedback<DriveOnHeadingFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/DriveOnHeadingActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public DriveOnHeadingActionFeedback() : base()
        {
            this.feedback = new DriveOnHeadingFeedback();
        }

        public DriveOnHeadingActionFeedback(HeaderMsg header, GoalStatusMsg status, DriveOnHeadingFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static DriveOnHeadingActionFeedback Deserialize(MessageDeserializer deserializer) => new DriveOnHeadingActionFeedback(deserializer);

        DriveOnHeadingActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = DriveOnHeadingFeedback.Deserialize(deserializer);
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
