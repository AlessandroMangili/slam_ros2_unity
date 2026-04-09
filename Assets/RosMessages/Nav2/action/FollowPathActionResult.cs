using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class FollowPathActionResult : ActionResult<FollowPathResult>
    {
        public const string k_RosMessageName = "nav2_msgs/FollowPathActionResult";
        public override string RosMessageName => k_RosMessageName;


        public FollowPathActionResult() : base()
        {
            this.result = new FollowPathResult();
        }

        public FollowPathActionResult(HeaderMsg header, GoalStatusMsg status, FollowPathResult result) : base(header, status)
        {
            this.result = result;
        }
        public static FollowPathActionResult Deserialize(MessageDeserializer deserializer) => new FollowPathActionResult(deserializer);

        FollowPathActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = FollowPathResult.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.status);
            serializer.Write(this.result);
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
