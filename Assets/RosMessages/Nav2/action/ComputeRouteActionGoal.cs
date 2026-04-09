using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class ComputeRouteActionGoal : ActionGoal<ComputeRouteGoal>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputeRouteActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public ComputeRouteActionGoal() : base()
        {
            this.goal = new ComputeRouteGoal();
        }

        public ComputeRouteActionGoal(HeaderMsg header, GoalIDMsg goal_id, ComputeRouteGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static ComputeRouteActionGoal Deserialize(MessageDeserializer deserializer) => new ComputeRouteActionGoal(deserializer);

        ComputeRouteActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = ComputeRouteGoal.Deserialize(deserializer);
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
