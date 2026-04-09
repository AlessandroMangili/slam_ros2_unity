using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class FollowPathAction : Action<FollowPathActionGoal, FollowPathActionResult, FollowPathActionFeedback, FollowPathGoal, FollowPathResult, FollowPathFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/FollowPathAction";
        public override string RosMessageName => k_RosMessageName;


        public FollowPathAction() : base()
        {
            this.action_goal = new FollowPathActionGoal();
            this.action_result = new FollowPathActionResult();
            this.action_feedback = new FollowPathActionFeedback();
        }

        public static FollowPathAction Deserialize(MessageDeserializer deserializer) => new FollowPathAction(deserializer);

        FollowPathAction(MessageDeserializer deserializer)
        {
            this.action_goal = FollowPathActionGoal.Deserialize(deserializer);
            this.action_result = FollowPathActionResult.Deserialize(deserializer);
            this.action_feedback = FollowPathActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
