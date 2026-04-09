using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class ComputeAndTrackRouteActionGoal : ActionGoal<ComputeAndTrackRouteGoal>
    {
        public const string k_RosMessageName = "nav2_msgs/ComputeAndTrackRouteActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public ComputeAndTrackRouteActionGoal() : base()
        {
            this.goal = new ComputeAndTrackRouteGoal();
        }

        public ComputeAndTrackRouteActionGoal(HeaderMsg header, GoalIDMsg goal_id, ComputeAndTrackRouteGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static ComputeAndTrackRouteActionGoal Deserialize(MessageDeserializer deserializer) => new ComputeAndTrackRouteActionGoal(deserializer);

        ComputeAndTrackRouteActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = ComputeAndTrackRouteGoal.Deserialize(deserializer);
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
