using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class ComputeAndTrackRouteAction : Action<ComputeAndTrackRouteActionGoal, ComputeAndTrackRouteActionResult, ComputeAndTrackRouteActionFeedback, ComputeAndTrackRouteGoal, ComputeAndTrackRouteResult, ComputeAndTrackRouteFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputeAndTrackRouteAction";
        public override string RosMessageName => k_RosMessageName;


        public ComputeAndTrackRouteAction() : base()
        {
            this.action_goal = new ComputeAndTrackRouteActionGoal();
            this.action_result = new ComputeAndTrackRouteActionResult();
            this.action_feedback = new ComputeAndTrackRouteActionFeedback();
        }

        public static ComputeAndTrackRouteAction Deserialize(MessageDeserializer deserializer) => new ComputeAndTrackRouteAction(deserializer);

        ComputeAndTrackRouteAction(MessageDeserializer deserializer)
        {
            this.action_goal = ComputeAndTrackRouteActionGoal.Deserialize(deserializer);
            this.action_result = ComputeAndTrackRouteActionResult.Deserialize(deserializer);
            this.action_feedback = ComputeAndTrackRouteActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
