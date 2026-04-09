using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class DummyBehaviorActionResult : ActionResult<DummyBehaviorResult>
    {
        public const string k_RosMessageName = "nav2_msgs/DummyBehaviorActionResult";
        public override string RosMessageName => k_RosMessageName;


        public DummyBehaviorActionResult() : base()
        {
            this.result = new DummyBehaviorResult();
        }

        public DummyBehaviorActionResult(HeaderMsg header, GoalStatusMsg status, DummyBehaviorResult result) : base(header, status)
        {
            this.result = result;
        }
        public static DummyBehaviorActionResult Deserialize(MessageDeserializer deserializer) => new DummyBehaviorActionResult(deserializer);

        DummyBehaviorActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = DummyBehaviorResult.Deserialize(deserializer);
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
