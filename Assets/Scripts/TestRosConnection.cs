using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class TestRosConnection : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Unity: Script avviato");

        var ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<StringMsg>("/unity_test");

        ros.Publish("/unity_test", new StringMsg("Hello from Unity"));
        Debug.Log("Unity: Messaggio inviato a ROS");
    }
}
