using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class AssistedTeleopActionGoal : ActionGoal<AssistedTeleopGoal>
    {
        public const string k_RosMessageName = "nav2_msgs/AssistedTeleopActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public AssistedTeleopActionGoal() : base()
        {
            this.goal = new AssistedTeleopGoal();
        }

        public AssistedTeleopActionGoal(HeaderMsg header, GoalIDMsg goal_id, AssistedTeleopGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static AssistedTeleopActionGoal Deserialize(MessageDeserializer deserializer) => new AssistedTeleopActionGoal(deserializer);

        AssistedTeleopActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = AssistedTeleopGoal.Deserialize(deserializer);
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
