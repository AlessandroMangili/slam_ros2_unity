using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class ComputeRouteAction : Action<ComputeRouteActionGoal, ComputeRouteActionResult, ComputeRouteActionFeedback, ComputeRouteGoal, ComputeRouteResult, ComputeRouteFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputeRouteAction";
        public override string RosMessageName => k_RosMessageName;


        public ComputeRouteAction() : base()
        {
            this.action_goal = new ComputeRouteActionGoal();
            this.action_result = new ComputeRouteActionResult();
            this.action_feedback = new ComputeRouteActionFeedback();
        }

        public static ComputeRouteAction Deserialize(MessageDeserializer deserializer) => new ComputeRouteAction(deserializer);

        ComputeRouteAction(MessageDeserializer deserializer)
        {
            this.action_goal = ComputeRouteActionGoal.Deserialize(deserializer);
            this.action_result = ComputeRouteActionResult.Deserialize(deserializer);
            this.action_feedback = ComputeRouteActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
