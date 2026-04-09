using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class AssistedTeleopActionResult : ActionResult<AssistedTeleopResult>
    {
        public const string k_RosMessageName = "nav2_msgs/AssistedTeleopActionResult";
        public override string RosMessageName => k_RosMessageName;


        public AssistedTeleopActionResult() : base()
        {
            this.result = new AssistedTeleopResult();
        }

        public AssistedTeleopActionResult(HeaderMsg header, GoalStatusMsg status, AssistedTeleopResult result) : base(header, status)
        {
            this.result = result;
        }
        public static AssistedTeleopActionResult Deserialize(MessageDeserializer deserializer) => new AssistedTeleopActionResult(deserializer);

        AssistedTeleopActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = AssistedTeleopResult.Deserialize(deserializer);
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
