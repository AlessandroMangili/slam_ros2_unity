using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

/// <summary>
/// Sends autonomous navigation goals to ROS2 Nav2 via the path_bridge.py node.
///
/// On StartNavigation(), the last waypoint of the selected mission is retrieved
/// from PathArrowGuide, converted from Unity space to ROS map coordinates, and
/// published as a PoseStamped on the nav goal topic.
///
/// The bridge listens on that topic and forwards the goal to Nav2's
/// NavigateToPose action server.
///
/// On CancelNavigation(), a Bool(true) is published on the cancel topic so
/// the bridge can abort the active NavigateToPose goal.
///
/// Coordinate conversion (Unity → ROS map):
///   rosX =  unityZ - offsetZ
///   rosY = -unityX + offsetX
/// These offsets must match those used in ROSPathRequester and ROSPathVisualizer.
/// </summary>
public class ROSNavigator : MonoBehaviour
{
    [Header("ROS Topics")]
    [Tooltip("Topic on which the navigation goal PoseStamped is published")]
    public string navGoalTopic   = "/unity/nav_goal";

    [Tooltip("Topic used to cancel the active Nav2 goal")]
    public string cancelNavTopic = "/unity/cancel_nav";

    [Tooltip("TF frame of the map — must match Nav2 configuration")]
    public string mapFrame       = "map";

    [Header("References")]
    public PathArrowGuide pathArrowGuide;

    [Header("Map Origin Offset")]
    [Tooltip("X offset between the Unity world origin and the ROS map origin")]
    public float offsetX = 5.02f;

    [Tooltip("Z offset between the Unity world origin and the ROS map origin")]
    public float offsetZ = -10.02f;

    private ROSConnection ros;
    private MissionType   currentMission = MissionType.None;
    private bool          navigating;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(navGoalTopic);
        ros.RegisterPublisher<BoolMsg>(cancelNavTopic);
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Converts the mission goal waypoint to ROS map coordinates and publishes
    /// it as a PoseStamped. The bridge forwards it to Nav2 NavigateToPose.
    /// </summary>
    public void StartNavigation(MissionType mission)
    {
        currentMission = mission;
        navigating     = true;

        PublishGoal();

        Debug.Log($"[ROSNavigator] Navigation started toward {mission}");
    }

    /// <summary>
    /// Publishes a cancel signal on the cancel topic so the bridge can abort
    /// the currently active Nav2 goal. Does nothing if not navigating.
    /// </summary>
    public void CancelNavigation()
    {
        if (!navigating) return;

        navigating     = false;
        currentMission = MissionType.None;

        ros.Publish(cancelNavTopic, new BoolMsg { data = true });

        Debug.Log("[ROSNavigator] Cancel signal sent to bridge.");
    }

    // ─── Goal publishing ─────────────────────────────────────────────────────

    private void PublishGoal()
    {
        if (pathArrowGuide == null) return;

        Transform goalTransform = pathArrowGuide.GetLastWaypoint(currentMission);
        if (goalTransform == null) return;

        Vector3    uPos = goalTransform.position;
        Quaternion uRot = goalTransform.rotation;

        // Convert Unity world position to ROS map coordinates
        float rosX =  uPos.z - offsetZ;
        float rosY = -uPos.x + offsetX;

        // Convert Unity quaternion to ROS FLU convention:
        //   rosX =  unityZ   (Unity forward → ROS left-hand flip)
        //   rosY = -unityX
        //   rosZ =  unityY
        //   rosW =  unityW
        var msg = new PoseStampedMsg
        {
            header = new HeaderMsg { frame_id = mapFrame },
            pose   = new PoseMsg
            {
                position    = new PointMsg { x = rosX, y = rosY, z = 0.0 },
                orientation = new QuaternionMsg
                {
                    x =  (double)uRot.z,
                    y = -(double)uRot.x,
                    z =  (double)uRot.y,
                    w =  (double)uRot.w
                }
            }
        };

        ros.Publish(navGoalTopic, msg);

        Debug.Log($"[ROSNavigator] Nav goal published: ROS({rosX:F2}, {rosY:F2})");
    }
}