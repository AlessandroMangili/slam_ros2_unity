using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class ComputePathThroughPosesActionGoal : ActionGoal<ComputePathThroughPosesGoal>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputePathThroughPosesActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public ComputePathThroughPosesActionGoal() : base()
        {
            this.goal = new ComputePathThroughPosesGoal();
        }

        public ComputePathThroughPosesActionGoal(HeaderMsg header, GoalIDMsg goal_id, ComputePathThroughPosesGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static ComputePathThroughPosesActionGoal Deserialize(MessageDeserializer deserializer) => new ComputePathThroughPosesActionGoal(deserializer);

        ComputePathThroughPosesActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = ComputePathThroughPosesGoal.Deserialize(deserializer);
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
