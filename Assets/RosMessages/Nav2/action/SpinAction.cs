using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class SpinAction : Action<SpinActionGoal, SpinActionResult, SpinActionFeedback, SpinGoal, SpinResult, SpinFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/SpinAction";
        public override string RosMessageName => k_RosMessageName;


        public SpinAction() : base()
        {
            this.action_goal = new SpinActionGoal();
            this.action_result = new SpinActionResult();
            this.action_feedback = new SpinActionFeedback();
        }

        public static SpinAction Deserialize(MessageDeserializer deserializer) => new SpinAction(deserializer);

        SpinAction(MessageDeserializer deserializer)
        {
            this.action_goal = SpinActionGoal.Deserialize(deserializer);
            this.action_result = SpinActionResult.Deserialize(deserializer);
            this.action_feedback = SpinActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
