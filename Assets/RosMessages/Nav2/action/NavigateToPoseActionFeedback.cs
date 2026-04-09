using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class NavigateToPoseActionFeedback : ActionFeedback<NavigateToPoseFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/NavigateToPoseActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public NavigateToPoseActionFeedback() : base()
        {
            this.feedback = new NavigateToPoseFeedback();
        }

        public NavigateToPoseActionFeedback(HeaderMsg header, GoalStatusMsg status, NavigateToPoseFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static NavigateToPoseActionFeedback Deserialize(MessageDeserializer deserializer) => new NavigateToPoseActionFeedback(deserializer);

        NavigateToPoseActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = NavigateToPoseFeedback.Deserialize(deserializer);
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
