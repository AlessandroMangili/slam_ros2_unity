using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;


public class RosSubscriber : MonoBehaviour
{
    ROSConnection ros;

    void Start()
    {
        // Ottieni o crea l'istanza ROS
        ros = ROSConnection.GetOrCreateInstance();

        // Sottoscrivi al topic /cmd_vel
        ros.Subscribe<TwistMsg>("/cmd_vel", ReceiveCmdVel);
    }

    void ReceiveCmdVel(TwistMsg msg)
    {
        // Esempio: stampa velocità lineare e angolare
        Debug.Log($"Ricevuto /cmd_vel: linear.x={msg.linear.x}, angular.z={msg.angular.z}");

        // Esempio semplice: muovere l'oggetto principale
        transform.Translate(Vector3.forward * (float)msg.linear.x * Time.deltaTime);
        transform.Rotate(Vector3.up * (float)msg.angular.z * Mathf.Rad2Deg * Time.deltaTime);
    }
}
