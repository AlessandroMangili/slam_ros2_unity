using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class SmoothPathActionResult : ActionResult<SmoothPathResult>
    {
        public const string k_RosMessageName = "nav2_msgs/SmoothPathActionResult";
        public override string RosMessageName => k_RosMessageName;


        public SmoothPathActionResult() : base()
        {
            this.result = new SmoothPathResult();
        }

        public SmoothPathActionResult(HeaderMsg header, GoalStatusMsg status, SmoothPathResult result) : base(header, status)
        {
            this.result = result;
        }
        public static SmoothPathActionResult Deserialize(MessageDeserializer deserializer) => new SmoothPathActionResult(deserializer);

        SmoothPathActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = SmoothPathResult.Deserialize(deserializer);
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
