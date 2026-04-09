using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class DummyBehaviorActionGoal : ActionGoal<DummyBehaviorGoal>
    {
        public const string k_RosMessageName = "nav2_msgs/DummyBehaviorActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public DummyBehaviorActionGoal() : base()
        {
            this.goal = new DummyBehaviorGoal();
        }

        public DummyBehaviorActionGoal(HeaderMsg header, GoalIDMsg goal_id, DummyBehaviorGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static DummyBehaviorActionGoal Deserialize(MessageDeserializer deserializer) => new DummyBehaviorActionGoal(deserializer);

        DummyBehaviorActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = DummyBehaviorGoal.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.goal_id);
            serializer.Write(this.goal);
        }


#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize);
        }
    }
}
