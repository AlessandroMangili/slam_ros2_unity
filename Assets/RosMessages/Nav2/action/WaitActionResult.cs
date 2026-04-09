using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class WaitActionResult : ActionResult<WaitResult>
    {
        public const string k_RosMessageName = "nav2_msgs/WaitActionResult";
        public override string RosMessageName => k_RosMessageName;


        public WaitActionResult() : base()
        {
            this.result = new WaitResult();
        }

        public WaitActionResult(HeaderMsg header, GoalStatusMsg status, WaitResult result) : base(header, status)
        {
            this.result = result;
        }
        public static WaitActionResult Deserialize(MessageDeserializer deserializer) => new WaitActionResult(deserializer);

        WaitActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = WaitResult.Deserialize(deserializer);
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
