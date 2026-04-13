using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

public class ROSNavigator : MonoBehaviour
{
    [Header("ROS Topics")]
    public string goalTopic      = "/goal_pose";
    public string cancelNavTopic = "/unity/cancel_nav";
    public string mapFrame       = "map";

    [Header("Riferimenti")]
    public PathArrowGuide pathArrowGuide;

    [Header("Offset mappa")]
    public float offsetX = 5.02f;
    public float offsetZ = -10.02f;

    private ROSConnection ros;
    private MissionType   currentMission = MissionType.None;
    private bool          navigating;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(goalTopic);
        ros.RegisterPublisher<BoolMsg>(cancelNavTopic);
    }

    public void StartNavigation(MissionType mission)
    {
        currentMission = mission;
        navigating     = true;
        PublishGoal();
        Debug.Log($"[ROSNavigator] Navigazione avviata verso {mission}");
    }

    public void CancelNavigation()
    {
        if (!navigating) return;
        navigating     = false;
        currentMission = MissionType.None;

        ros.Publish(cancelNavTopic, new BoolMsg { data = true });
        Debug.Log("[ROSNavigator] Cancel inviato a path_bridge.");
    }

    private void PublishGoal()
    {
        if (pathArrowGuide == null)
        {
            Debug.LogError("[ROSNavigator] pathArrowGuide è NULL!");
            return;
        }

        Transform goalTransform = pathArrowGuide.GetLastWaypoint(currentMission);
        if (goalTransform == null)
        {
            Debug.LogWarning("[ROSNavigator] Nessun waypoint goal trovato.");
            return;
        }

        Vector3    uPos = goalTransform.position;
        Quaternion uRot = goalTransform.rotation;

        float rosX =  uPos.z - offsetZ;
        float rosY = -uPos.x + offsetX;

        var msg = new PoseStampedMsg
        {
            header = new HeaderMsg { frame_id = mapFrame },
            pose   = new PoseMsg
            {
                position = new PointMsg { x = rosX, y = rosY, z = 0.0 },
                orientation = new QuaternionMsg
                {
                    x =  (double)uRot.z,
                    y = -(double)uRot.x,
                    z =  (double)uRot.y,
                    w =  (double)uRot.w
                }
            }
        };

        ros.Publish(goalTopic, msg);
        Debug.Log($"[ROSNavigator] Goal pubblicato: ROS({rosX:F2},{rosY:F2})");
    }
}