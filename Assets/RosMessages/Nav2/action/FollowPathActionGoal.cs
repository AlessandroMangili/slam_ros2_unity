using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class FollowPathActionGoal : ActionGoal<FollowPathGoal>
    {
        public const string k_RosMessageName = "nav2_msgs/FollowPathActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public FollowPathActionGoal() : base()
        {
            this.goal = new FollowPathGoal();
        }

        public FollowPathActionGoal(HeaderMsg header, GoalIDMsg goal_id, FollowPathGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static FollowPathActionGoal Deserialize(MessageDeserializer deserializer) => new FollowPathActionGoal(deserializer);

        FollowPathActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = FollowPathGoal.Deserialize(deserializer);
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
