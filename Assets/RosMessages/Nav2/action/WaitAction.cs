using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class WaitAction : Action<WaitActionGoal, WaitActionResult, WaitActionFeedback, WaitGoal, WaitResult, WaitFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/WaitAction";
        public override string RosMessageName => k_RosMessageName;


        public WaitAction() : base()
        {
            this.action_goal = new WaitActionGoal();
            this.action_result = new WaitActionResult();
            this.action_feedback = new WaitActionFeedback();
        }

        public static WaitAction Deserialize(MessageDeserializer deserializer) => new WaitAction(deserializer);

        WaitAction(MessageDeserializer deserializer)
        {
            this.action_goal = WaitActionGoal.Deserialize(deserializer);
            this.action_result = WaitActionResult.Deserialize(deserializer);
            this.action_feedback = WaitActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
