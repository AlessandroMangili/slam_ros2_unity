using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Nav2
{
    public class DummyBehaviorAction : Action<DummyBehaviorActionGoal, DummyBehaviorActionResult, DummyBehaviorActionFeedback, DummyBehaviorGoal, DummyBehaviorResult, DummyBehaviorFeedback>
    {
        public const string k_RosMessageName = "nav2_msgs/DummyBehaviorAction";
        public override string RosMessageName => k_RosMessageName;


        public DummyBehaviorAction() : base()
        {
            this.action_goal = new DummyBehaviorActionGoal();
            this.action_result = new DummyBehaviorActionResult();
            this.action_feedback = new DummyBehaviorActionFeedback();
        }

        public static DummyBehaviorAction Deserialize(MessageDeserializer deserializer) => new DummyBehaviorAction(deserializer);

        DummyBehaviorAction(MessageDeserializer deserializer)
        {
            this.action_goal = DummyBehaviorActionGoal.Deserialize(deserializer);
            this.action_result = DummyBehaviorActionResult.Deserialize(deserializer);
            this.action_feedback = DummyBehaviorActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
