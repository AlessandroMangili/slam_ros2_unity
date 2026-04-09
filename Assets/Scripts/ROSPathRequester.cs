using System.Collections;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.Nav;

public class ROSPathRequester : MonoBehaviour
{
    [Header("ROS Topics")]
    public string goalTopic = "/unity/path_goal";
    public string pathTopic = "/unity/path_result";
    public string mapFrame  = "map";

    [Header("Riferimenti")]
    public PathArrowGuide    pathArrowGuide;
    public ROSPathVisualizer pathVisualizer;

    [Header("Aggiornamento live")]
    public float updateInterval = 1.5f;

    [Header("Offset origine mappa (stesso del Visualizer)")]
    public float offsetX = 5.02f;
    public float offsetZ = -10.02f;

    private ROSConnection ros;
    private MissionType   currentMission = MissionType.None;
    private Coroutine     updateRoutine;
    private bool          missionActive;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(goalTopic);
        ros.Subscribe<PathMsg>(pathTopic, OnPathReceived);
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

        // Sottrai l'offset prima di convertire Unity → ROS
        float rosX =  uPos.z - offsetZ;  // offsetZ = -10.02 → sottrai -10.02 = aggiungi 10.02
        float rosY = -uPos.x + offsetX;  // offsetX =  5.02  → sottrai 5.02

        // Equivalente esplicito:
        // rosX =  uPos.z + 10.02f
        // rosY = -uPos.x - 5.02f

        Debug.Log($"[Goal] Unity({uPos.x:F2},{uPos.z:F2}) → ROS({rosX:F2},{rosY:F2})");

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

        ros.Publish(goalTopic, msg);
    }

    private void OnPathReceived(PathMsg msg)
    {
        if (msg?.poses == null || msg.poses.Length == 0)
        {
            Debug.LogWarning("[ROSPathRequester] Path vuoto.");
            return;
        }

        //Debug.Log($"[ROSPathRequester] Ricevute {msg.poses.Length} pose.");
        // Aggiungi questo log temporaneo
        Debug.Log($"[ROSPathRequester] Ricevute {msg.poses.Length} pose. " +
            $"Prima posa ROS: x={msg.poses[0].pose.position.x:F2} " +
            $"y={msg.poses[0].pose.position.y:F2}");

        // Rimuove le frecce statiche di PathArrowGuide
        if (pathArrowGuide != null)
            pathArrowGuide.ClearArrows();
    }
}