using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class DriveOnHeadingActionGoal : ActionGoal<DriveOnHeadingGoal>
    {
        public const string k_RosMessageName = "nav2_msgs/DriveOnHeadingActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public DriveOnHeadingActionGoal() : base()
        {
            this.goal = new DriveOnHeadingGoal();
        }

        public DriveOnHeadingActionGoal(HeaderMsg header, GoalIDMsg goal_id, DriveOnHeadingGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static DriveOnHeadingActionGoal Deserialize(MessageDeserializer deserializer) => new DriveOnHeadingActionGoal(deserializer);

        DriveOnHeadingActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = DriveOnHeadingGoal.Deserialize(deserializer);
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
