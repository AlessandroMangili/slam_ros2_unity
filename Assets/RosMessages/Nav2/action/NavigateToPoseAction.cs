using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class NavigateToPoseAction : Action<NavigateToPoseActionGoal, NavigateToPoseActionResult, NavigateToPoseActionFeedback, NavigateToPoseGoal, NavigateToPoseResult, NavigateToPoseFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/NavigateToPoseAction";
        public override string RosMessageName => k_RosMessageName;


        public NavigateToPoseAction() : base()
        {
            this.action_goal = new NavigateToPoseActionGoal();
            this.action_result = new NavigateToPoseActionResult();
            this.action_feedback = new NavigateToPoseActionFeedback();
        }

        public static NavigateToPoseAction Deserialize(MessageDeserializer deserializer) => new NavigateToPoseAction(deserializer);

        NavigateToPoseAction(MessageDeserializer deserializer)
        {
            this.action_goal = NavigateToPoseActionGoal.Deserialize(deserializer);
            this.action_result = NavigateToPoseActionResult.Deserialize(deserializer);
            this.action_feedback = NavigateToPoseActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
