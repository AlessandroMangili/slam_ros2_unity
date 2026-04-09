using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class BackUpActionResult : ActionResult<BackUpResult>
    {
        public const string k_RosMessageName = "nav2_msgs/BackUpActionResult";
        public override string RosMessageName => k_RosMessageName;


        public BackUpActionResult() : base()
        {
            this.result = new BackUpResult();
        }

        public BackUpActionResult(HeaderMsg header, GoalStatusMsg status, BackUpResult result) : base(header, status)
        {
            this.result = result;
        }
        public static BackUpActionResult Deserialize(MessageDeserializer deserializer) => new BackUpActionResult(deserializer);

        BackUpActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = BackUpResult.Deserialize(deserializer);
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
