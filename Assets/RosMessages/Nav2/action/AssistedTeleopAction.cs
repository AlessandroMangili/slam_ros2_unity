using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class AssistedTeleopAction : Action<AssistedTeleopActionGoal, AssistedTeleopActionResult, AssistedTeleopActionFeedback, AssistedTeleopGoal, AssistedTeleopResult, AssistedTeleopFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/AssistedTeleopAction";
        public override string RosMessageName => k_RosMessageName;


        public AssistedTeleopAction() : base()
        {
            this.action_goal = new AssistedTeleopActionGoal();
            this.action_result = new AssistedTeleopActionResult();
            this.action_feedback = new AssistedTeleopActionFeedback();
        }

        public static AssistedTeleopAction Deserialize(MessageDeserializer deserializer) => new AssistedTeleopAction(deserializer);

        AssistedTeleopAction(MessageDeserializer deserializer)
        {
            this.action_goal = AssistedTeleopActionGoal.Deserialize(deserializer);
            this.action_result = AssistedTeleopActionResult.Deserialize(deserializer);
            this.action_feedback = AssistedTeleopActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
