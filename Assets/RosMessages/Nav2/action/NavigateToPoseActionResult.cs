using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Nav2
{
    public class NavigateToPoseActionResult : ActionResult<NavigateToPoseResult>
    {
        public const string k_RosMessageName = "nav2_msgs/NavigateToPoseActionResult";
        public override string RosMessageName => k_RosMessageName;


        public NavigateToPoseActionResult() : base()
        {
            this.result = new NavigateToPoseResult();
        }

        public NavigateToPoseActionResult(HeaderMsg header, GoalStatusMsg status, NavigateToPoseResult result) : base(header, status)
        {
            this.result = result;
        }
        public static NavigateToPoseActionResult Deserialize(MessageDeserializer deserializer) => new NavigateToPoseActionResult(deserializer);

        NavigateToPoseActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = NavigateToPoseResult.Deserialize(deserializer);
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
