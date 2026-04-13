using System.Collections;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.Nav;

public class ROSPathRequester : MonoBehaviour
{
    [Header("ROS Topics")]
    public string goalTopic       = "/unity/path_goal";
    public string goalVisualTopic = "/unity/path_goal_visual";
    public string pathTopic       = "/unity/path_result";
    public string mapFrame        = "map";

    [Header("Riferimenti")]
    public PathArrowGuide    pathArrowGuide;
    public ROSPathVisualizer pathVisualizer;

    [Header("Aggiornamento live")]
    public float updateInterval = 1.5f;

    [Header("Offset origine mappa")]
    public float offsetX = 5.02f;
    public float offsetZ = -10.02f;

    private ROSConnection ros;
    private MissionType   currentMission = MissionType.None;
    private Coroutine     updateRoutine;
    private bool          missionActive;
    private bool          autonomousMode;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(goalTopic);
        ros.RegisterPublisher<PoseStampedMsg>(goalVisualTopic);
        ros.Subscribe<PathMsg>(pathTopic, OnPathReceived);
    }

    public void SetAutonomousMode(bool value)
    {
        autonomousMode = value;
    }

    public void StartPathUpdates(MissionType mission)
    {
        currentMission = mission;
        missionActive  = true;
        if (updateRoutine != null) StopCoroutine(updateRoutine);
        updateRoutine = StartCoroutine(PathUpdateLoop());
    }

    public void StopPathUpdates()
    {
        missionActive  = false;
        currentMission = MissionType.None;
        autonomousMode = false;
        if (updateRoutine != null) StopCoroutine(updateRoutine);
        if (pathVisualizer != null) pathVisualizer.ClearPath();
    }

    private IEnumerator PathUpdateLoop()
    {
        while (missionActive)
        {
            PublishGoal();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void PublishGoal()
    {
        if (pathArrowGuide == null) return;

        Transform goalTransform = pathArrowGuide.GetLastWaypoint(currentMission);
        if (goalTransform == null) return;

        Vector3 uPos = goalTransform.position;

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
                    x =  (double)goalTransform.rotation.z,
                    y = -(double)goalTransform.rotation.x,
                    z =  (double)goalTransform.rotation.y,
                    w =  (double)goalTransform.rotation.w
                }
            }
        };

        // In modalità autonoma usa il topic visivo separato
        // così il bridge non interferisce con Nav2 NavigateToPose
        string topic = autonomousMode ? goalVisualTopic : goalTopic;
        ros.Publish(topic, msg);
    }

    private void OnPathReceived(PathMsg msg)
    {
        if (msg?.poses == null || msg.poses.Length == 0)
        {
            Debug.LogWarning("[ROSPathRequester] Path vuoto.");
            return;
        }

        Debug.Log($"[ROSPathRequester] Ricevute {msg.poses.Length} pose.");

        if (pathVisualizer != null)
            pathVisualizer.UpdatePath(msg.poses);
        else
            Debug.LogError("[ROSPathRequester] pathVisualizer è NULL!");
    }
}