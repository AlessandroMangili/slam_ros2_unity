using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class ComputePathToPoseActionGoal : ActionGoal<ComputePathToPoseGoal>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputePathToPoseActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public ComputePathToPoseActionGoal() : base()
        {
            this.goal = new ComputePathToPoseGoal();
        }

        public ComputePathToPoseActionGoal(HeaderMsg header, GoalIDMsg goal_id, ComputePathToPoseGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static ComputePathToPoseActionGoal Deserialize(MessageDeserializer deserializer) => new ComputePathToPoseActionGoal(deserializer);

        ComputePathToPoseActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = ComputePathToPoseGoal.Deserialize(deserializer);
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
