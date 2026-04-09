using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class SpinActionGoal : ActionGoal<SpinGoal>
    {
        public const string k_RosMessageName = "nav2_msgs/SpinActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public SpinActionGoal() : base()
        {
            this.goal = new SpinGoal();
        }

        public SpinActionGoal(HeaderMsg header, GoalIDMsg goal_id, SpinGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static SpinActionGoal Deserialize(MessageDeserializer deserializer) => new SpinActionGoal(deserializer);

        SpinActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = SpinGoal.Deserialize(deserializer);
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
