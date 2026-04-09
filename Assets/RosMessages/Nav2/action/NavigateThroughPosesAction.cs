using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class NavigateThroughPosesAction : Action<NavigateThroughPosesActionGoal, NavigateThroughPosesActionResult, NavigateThroughPosesActionFeedback, NavigateThroughPosesGoal, NavigateThroughPosesResult, NavigateThroughPosesFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/NavigateThroughPosesAction";
        public override string RosMessageName => k_RosMessageName;


        public NavigateThroughPosesAction() : base()
        {
            this.action_goal = new NavigateThroughPosesActionGoal();
            this.action_result = new NavigateThroughPosesActionResult();
            this.action_feedback = new NavigateThroughPosesActionFeedback();
        }

        public static NavigateThroughPosesAction Deserialize(MessageDeserializer deserializer) => new NavigateThroughPosesAction(deserializer);

        NavigateThroughPosesAction(MessageDeserializer deserializer)
        {
            this.action_goal = NavigateThroughPosesActionGoal.Deserialize(deserializer);
            this.action_result = NavigateThroughPosesActionResult.Deserialize(deserializer);
            this.action_feedback = NavigateThroughPosesActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
