using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class NavigateThroughPosesActionResult : ActionResult<NavigateThroughPosesResult>
    {
        public const string k_RosMessageName = "nav2_msgs/NavigateThroughPosesActionResult";
        public override string RosMessageName => k_RosMessageName;


        public NavigateThroughPosesActionResult() : base()
        {
            this.result = new NavigateThroughPosesResult();
        }

        public NavigateThroughPosesActionResult(HeaderMsg header, GoalStatusMsg status, NavigateThroughPosesResult result) : base(header, status)
        {
            this.result = result;
        }
        public static NavigateThroughPosesActionResult Deserialize(MessageDeserializer deserializer) => new NavigateThroughPosesActionResult(deserializer);

        NavigateThroughPosesActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = NavigateThroughPosesResult.Deserialize(deserializer);
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
