using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class NavigateToPoseActionGoal : ActionGoal<NavigateToPoseGoal>
    {
        public const string k_RosMessageName = "nav2_msgs/NavigateToPoseActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public NavigateToPoseActionGoal() : base()
        {
            this.goal = new NavigateToPoseGoal();
        }

        public NavigateToPoseActionGoal(HeaderMsg header, GoalIDMsg goal_id, NavigateToPoseGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static NavigateToPoseActionGoal Deserialize(MessageDeserializer deserializer) => new NavigateToPoseActionGoal(deserializer);

        NavigateToPoseActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = NavigateToPoseGoal.Deserialize(deserializer);
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
