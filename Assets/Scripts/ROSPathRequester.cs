using System.Collections;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.Nav;

/// <summary>
/// Periodically publishes the active mission's goal pose to ROS2 so that
/// path_bridge.py can request a ComputePathToPose and return the planned
/// path for visual display via ROSPathVisualizer.
///
/// Two goal topics are used to separate concerns:
///   - goalTopic:       used in teleoperation mode — the bridge calls
///                      ComputePathToPose and publishes the result back.
///   - goalVisualTopic: used in autonomous mode — the bridge computes the
///                      visual path without interfering with the active
///                      NavigateToPose action already managed by ROSNavigator.
///
/// The path is refreshed every updateInterval seconds while a mission is active.
///
/// Coordinate conversion (Unity → ROS map):
///   rosX =  unityZ - offsetZ
///   rosY = -unityX + offsetX
/// These offsets must match those used in ROSNavigator and ROSPathVisualizer.
/// </summary>
public class ROSPathRequester : MonoBehaviour
{
    [Header("ROS Topics")]
    [Tooltip("Goal topic used in teleoperation mode")]
    public string goalTopic       = "/unity/path_goal";

    [Tooltip("Goal topic used in autonomous mode (does not interfere with Nav2)")]
    public string goalVisualTopic = "/unity/path_goal_visual";

    [Tooltip("Topic on which the bridge publishes the computed path")]
    public string pathTopic       = "/unity/path_result";

    [Tooltip("TF frame of the map — must match Nav2 configuration")]
    public string mapFrame        = "map";

    [Header("References")]
    public PathArrowGuide    pathArrowGuide;
    public ROSPathVisualizer pathVisualizer;

    [Header("Live Update")]
    [Tooltip("Seconds between consecutive path requests while a mission is active")]
    public float updateInterval = 1.5f;

    [Header("Map Origin Offset")]
    [Tooltip("X offset between the Unity world origin and the ROS map origin")]
    public float offsetX = 5.02f;

    [Tooltip("Z offset between the Unity world origin and the ROS map origin")]
    public float offsetZ = -10.02f;

    private ROSConnection ros;
    private MissionType   currentMission = MissionType.None;
    private Coroutine     updateRoutine;
    private bool          missionActive;
    private bool          autonomousMode;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(goalTopic);
        ros.RegisterPublisher<PoseStampedMsg>(goalVisualTopic);
        ros.Subscribe<PathMsg>(pathTopic, OnPathReceived);
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Switches between autonomous and teleoperation topic routing.
    /// Must be called before StartPathUpdates() to take effect immediately.
    /// </summary>
    public void SetAutonomousMode(bool value)
    {
        autonomousMode = value;
    }

    /// <summary>
    /// Starts the periodic goal publishing loop for the given mission.
    /// Any previously running loop is stopped first.
    /// </summary>
    public void StartPathUpdates(MissionType mission)
    {
        currentMission = mission;
        missionActive  = true;

        if (updateRoutine != null) StopCoroutine(updateRoutine);
        updateRoutine = StartCoroutine(PathUpdateLoop());
    }

    /// <summary>
    /// Stops the update loop, resets state, and clears the visual path.
    /// </summary>
    public void StopPathUpdates()
    {
        missionActive  = false;
        currentMission = MissionType.None;
        autonomousMode = false;

        if (updateRoutine != null) StopCoroutine(updateRoutine);
        if (pathVisualizer != null) pathVisualizer.ClearPath();
    }

    // ─── Update loop ─────────────────────────────────────────────────────────

    private IEnumerator PathUpdateLoop()
    {
        while (missionActive)
        {
            PublishGoal();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    // ─── Goal publishing ─────────────────────────────────────────────────────

    private void PublishGoal()
    {
        if (pathArrowGuide == null) return;

        Transform goalTransform = pathArrowGuide.GetLastWaypoint(currentMission);
        if (goalTransform == null) return;

        Vector3 uPos = goalTransform.position;

        // Convert Unity world position to ROS map coordinates
        float rosX =  uPos.z - offsetZ;
        float rosY = -uPos.x + offsetX;

        var msg = new PoseStampedMsg
        {
            header = new HeaderMsg { frame_id = mapFrame },
            pose   = new PoseMsg
            {
                position    = new PointMsg { x = rosX, y = rosY, z = 0.0 },

                // Convert Unity quaternion to ROS FLU convention
                orientation = new QuaternionMsg
                {
                    x =  (double)goalTransform.rotation.z,
                    y = -(double)goalTransform.rotation.x,
                    z =  (double)goalTransform.rotation.y,
                    w =  (double)goalTransform.rotation.w
                }
            }
        };

        // In autonomous mode use the visual topic so the bridge does not
        // interfere with the NavigateToPose action managed by ROSNavigator
        string topic = autonomousMode ? goalVisualTopic : goalTopic;
        ros.Publish(topic, msg);
    }

    // ─── ROS callback ────────────────────────────────────────────────────────

    /// <summary>
    /// Receives the planned path from the bridge and forwards it to
    /// ROSPathVisualizer for rendering.
    /// </summary>
    private void OnPathReceived(PathMsg msg)
    {
        if (msg?.poses == null || msg.poses.Length == 0)
        {
            Debug.LogWarning("[ROSPathRequester] Received empty path.");
            return;
        }

        Debug.Log($"[ROSPathRequester] Received {msg.poses.Length} poses.");

        if (pathVisualizer != null)
            pathVisualizer.UpdatePath(msg.poses);
        else
            Debug.LogError("[ROSPathRequester] pathVisualizer is not assigned.");
    }
}