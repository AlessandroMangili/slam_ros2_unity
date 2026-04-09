using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class NavigateThroughPosesActionGoal : ActionGoal<NavigateThroughPosesGoal>
    {
        public const string k_RosMessageName = "nav2_msgs/NavigateThroughPosesActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public NavigateThroughPosesActionGoal() : base()
        {
            this.goal = new NavigateThroughPosesGoal();
        }

        public NavigateThroughPosesActionGoal(HeaderMsg header, GoalIDMsg goal_id, NavigateThroughPosesGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static NavigateThroughPosesActionGoal Deserialize(MessageDeserializer deserializer) => new NavigateThroughPosesActionGoal(deserializer);

        NavigateThroughPosesActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = NavigateThroughPosesGoal.Deserialize(deserializer);
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
