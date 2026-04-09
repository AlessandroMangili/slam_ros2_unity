using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class SmoothPathAction : Action<SmoothPathActionGoal, SmoothPathActionResult, SmoothPathActionFeedback, SmoothPathGoal, SmoothPathResult, SmoothPathFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/SmoothPathAction";
        public override string RosMessageName => k_RosMessageName;


        public SmoothPathAction() : base()
        {
            this.action_goal = new SmoothPathActionGoal();
            this.action_result = new SmoothPathActionResult();
            this.action_feedback = new SmoothPathActionFeedback();
        }

        public static SmoothPathAction Deserialize(MessageDeserializer deserializer) => new SmoothPathAction(deserializer);

        SmoothPathAction(MessageDeserializer deserializer)
        {
            this.action_goal = SmoothPathActionGoal.Deserialize(deserializer);
            this.action_result = SmoothPathActionResult.Deserialize(deserializer);
            this.action_feedback = SmoothPathActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
