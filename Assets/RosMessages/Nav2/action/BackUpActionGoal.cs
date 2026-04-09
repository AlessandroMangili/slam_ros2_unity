using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class BackUpActionGoal : ActionGoal<BackUpGoal>
    {
        public const string k_RosMessageName = "nav2_msgs/BackUpActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public BackUpActionGoal() : base()
        {
            this.goal = new BackUpGoal();
        }

        public BackUpActionGoal(HeaderMsg header, GoalIDMsg goal_id, BackUpGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static BackUpActionGoal Deserialize(MessageDeserializer deserializer) => new BackUpActionGoal(deserializer);

        BackUpActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = BackUpGoal.Deserialize(deserializer);
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
