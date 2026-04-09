using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class BackUpAction : Action<BackUpActionGoal, BackUpActionResult, BackUpActionFeedback, BackUpGoal, BackUpResult, BackUpFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/BackUpAction";
        public override string RosMessageName => k_RosMessageName;


        public BackUpAction() : base()
        {
            this.action_goal = new BackUpActionGoal();
            this.action_result = new BackUpActionResult();
            this.action_feedback = new BackUpActionFeedback();
        }

        public static BackUpAction Deserialize(MessageDeserializer deserializer) => new BackUpAction(deserializer);

        BackUpAction(MessageDeserializer deserializer)
        {
            this.action_goal = BackUpActionGoal.Deserialize(deserializer);
            this.action_result = BackUpActionResult.Deserialize(deserializer);
            this.action_feedback = BackUpActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
