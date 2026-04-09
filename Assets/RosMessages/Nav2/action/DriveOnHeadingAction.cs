using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class DriveOnHeadingAction : Action<DriveOnHeadingActionGoal, DriveOnHeadingActionResult, DriveOnHeadingActionFeedback, DriveOnHeadingGoal, DriveOnHeadingResult, DriveOnHeadingFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/DriveOnHeadingAction";
        public override string RosMessageName => k_RosMessageName;


        public DriveOnHeadingAction() : base()
        {
            this.action_goal = new DriveOnHeadingActionGoal();
            this.action_result = new DriveOnHeadingActionResult();
            this.action_feedback = new DriveOnHeadingActionFeedback();
        }

        public static DriveOnHeadingAction Deserialize(MessageDeserializer deserializer) => new DriveOnHeadingAction(deserializer);

        DriveOnHeadingAction(MessageDeserializer deserializer)
        {
            this.action_goal = DriveOnHeadingActionGoal.Deserialize(deserializer);
            this.action_result = DriveOnHeadingActionResult.Deserialize(deserializer);
            this.action_feedback = DriveOnHeadingActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
