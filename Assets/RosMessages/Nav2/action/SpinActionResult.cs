using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class SpinActionResult : ActionResult<SpinResult>
    {
        public const string k_RosMessageName = "nav2_msgs/SpinActionResult";
        public override string RosMessageName => k_RosMessageName;


        public SpinActionResult() : base()
        {
            this.result = new SpinResult();
        }

        public SpinActionResult(HeaderMsg header, GoalStatusMsg status, SpinResult result) : base(header, status)
        {
            this.result = result;
        }
        public static SpinActionResult Deserialize(MessageDeserializer deserializer) => new SpinActionResult(deserializer);

        SpinActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = SpinResult.Deserialize(deserializer);
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
