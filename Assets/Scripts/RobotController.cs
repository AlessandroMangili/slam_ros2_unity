using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

public class RobotController : MonoBehaviour
{
    [Header("Velocità robot")]
    public float linearSpeed = 1.0f;    // m/s
    public float angularSpeed = 90.0f;  // gradi/sec

    ROSConnection ros;

    // Ultimo comando ricevuto da ROS
    private TwistMsg lastTwist;

    // Altezza fissa per bloccare Y
    private float fixedY;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<TwistMsg>("/cmd_vel", ReceiveTwist);

        // Memorizza altezza iniziale
        fixedY = transform.position.y;

        // Inizializza lastTwist a zero
        lastTwist = new TwistMsg();
    }

    void ReceiveTwist(TwistMsg msg)
    {
        lastTwist = msg; // memorizza l'ultimo comando ricevuto
    }

    void Update()
    {
        // Movimento lineare (X=0, Y bloccata, Z=forward)
        Vector3 linear = new Vector3(0, 0, (float)lastTwist.linear.x);
        transform.Translate(linear * linearSpeed * Time.deltaTime, Space.Self);

        // Rotazione attorno a Y
        float angular = (float)lastTwist.angular.z;
        transform.Rotate(Vector3.up, angular * angularSpeed * Time.deltaTime, Space.Self);

        // Blocca altezza Y per evitare cadute
        Vector3 pos = transform.position;
        pos.y = fixedY;
        transform.position = pos;

        // Blocca rotazioni indesiderate (X/Z)
        Vector3 rot = transform.eulerAngles;
        rot.x = 0f;
        rot.z = 0f;
        transform.eulerAngles = rot;
    }
}