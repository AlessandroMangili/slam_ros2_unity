using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class ComputePathThroughPosesAction : Action<ComputePathThroughPosesActionGoal, ComputePathThroughPosesActionResult, ComputePathThroughPosesActionFeedback, ComputePathThroughPosesGoal, ComputePathThroughPosesResult, ComputePathThroughPosesFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputePathThroughPosesAction";
        public override string RosMessageName => k_RosMessageName;


        public ComputePathThroughPosesAction() : base()
        {
            this.action_goal = new ComputePathThroughPosesActionGoal();
            this.action_result = new ComputePathThroughPosesActionResult();
            this.action_feedback = new ComputePathThroughPosesActionFeedback();
        }

        public static ComputePathThroughPosesAction Deserialize(MessageDeserializer deserializer) => new ComputePathThroughPosesAction(deserializer);

        ComputePathThroughPosesAction(MessageDeserializer deserializer)
        {
            this.action_goal = ComputePathThroughPosesActionGoal.Deserialize(deserializer);
            this.action_result = ComputePathThroughPosesActionResult.Deserialize(deserializer);
            this.action_feedback = ComputePathThroughPosesActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
