using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class WaitActionGoal : ActionGoal<WaitGoal>
    {
        public const string k_RosMessageName = "nav2_msgs/WaitActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public WaitActionGoal() : base()
        {
            this.goal = new WaitGoal();
        }

        public WaitActionGoal(HeaderMsg header, GoalIDMsg goal_id, WaitGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static WaitActionGoal Deserialize(MessageDeserializer deserializer) => new WaitActionGoal(deserializer);

        WaitActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = WaitGoal.Deserialize(deserializer);
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
