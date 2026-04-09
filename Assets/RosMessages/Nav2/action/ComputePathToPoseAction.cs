using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class ComputePathToPoseAction : Action<ComputePathToPoseActionGoal, ComputePathToPoseActionResult, ComputePathToPoseActionFeedback, ComputePathToPoseGoal, ComputePathToPoseResult, ComputePathToPoseFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputePathToPoseAction";
        public override string RosMessageName => k_RosMessageName;


        public ComputePathToPoseAction() : base()
        {
            this.action_goal = new ComputePathToPoseActionGoal();
            this.action_result = new ComputePathToPoseActionResult();
            this.action_feedback = new ComputePathToPoseActionFeedback();
        }

        public static ComputePathToPoseAction Deserialize(MessageDeserializer deserializer) => new ComputePathToPoseAction(deserializer);

        ComputePathToPoseAction(MessageDeserializer deserializer)
        {
            this.action_goal = ComputePathToPoseActionGoal.Deserialize(deserializer);
            this.action_result = ComputePathToPoseActionResult.Deserialize(deserializer);
            this.action_feedback = ComputePathToPoseActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
