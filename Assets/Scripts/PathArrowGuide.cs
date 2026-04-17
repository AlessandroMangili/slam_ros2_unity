using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Spawns directional arrow prefabs and a goal marker along the waypoints
/// of the active mission, and monitors the robot's proximity to the goal.
///
/// When the robot comes within goalReachedDistance of the final waypoint,
/// MissionMenuManager.OnMissionCompleted() is called and all arrows are cleared.
///
/// Each mission has its own set of waypoints assigned in the Inspector.
/// The last waypoint of each set is treated as the goal and uses goalPrefab
/// instead of arrowPrefab. If goalPrefab is not assigned, arrowPrefab is used
/// as a fallback.
/// </summary>
public class PathArrowGuide : MonoBehaviour
{
    [Header("Waypoints per Mission")]
    public Transform[] waypointsA;
    public Transform[] waypointsB;
    public Transform[] waypointsC;

    [Header("Prefabs")]
    [Tooltip("Prefab instantiated at each intermediate waypoint")]
    public GameObject arrowPrefab;

    [Tooltip("Prefab instantiated at the final waypoint (goal). Falls back to arrowPrefab if null.")]
    public GameObject goalPrefab;

    [Tooltip("Parent Transform used to keep spawned arrows organised in the hierarchy")]
    public Transform arrowsParent;

    [Header("Arrow Layout")]
    [Tooltip("Vertical offset applied to every spawned arrow and goal marker")]
    public float arrowHeight = 0.15f;
    public float arrowScale  = 0.4f;
    public float goalScale   = 0.6f;

    [Header("Proximity Check")]
    [Tooltip("Transform of the robot used to measure distance to the goal")]
    public Transform robot;

    [Tooltip("Distance in metres at which the goal is considered reached")]
    public float goalReachedDistance = 0.15f;

    public MissionMenuManager missionMenuManager;

    [Header("UI")]
    [Tooltip("Label that displays the remaining distance to the goal")]
    public TextMeshProUGUI distanceText;

    // All currently spawned arrows and the goal marker
    private readonly List<GameObject> spawnedArrows = new List<GameObject>();
    private Transform currentGoal;
    private bool      missionCompleted;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Update()
    {
        // Hide distance label once the mission is complete
        if (missionCompleted)
        {
            if (distanceText != null) distanceText.gameObject.SetActive(false);
            return;
        }

        if (robot == null || currentGoal == null)
        {
            if (distanceText != null) distanceText.gameObject.SetActive(false);
            return;
        }

        float distance = Vector3.Distance(robot.position, currentGoal.position);

        if (distanceText != null)
        {
            distanceText.gameObject.SetActive(true);
            distanceText.text = $"Goal distance: {distance:0.00} m";
        }

        // Trigger mission completion when the robot is close enough
        if (distance <= goalReachedDistance)
        {
            missionCompleted = true;

            if (missionMenuManager != null)
                missionMenuManager.OnMissionCompleted();

            ClearArrows();
        }
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Clears any existing arrows and spawns the path for the given mission.
    /// Does nothing (beyond clearing) if mission is MissionType.None.
    /// </summary>
    public void ShowMission(MissionType mission)
    {
        ClearArrows();
        missionCompleted = false;
        currentGoal      = null;

        if (distanceText != null) distanceText.gameObject.SetActive(true);
        if (mission == MissionType.None) return;

        Transform[] waypoints = GetWaypoints(mission);
        if (waypoints == null || waypoints.Length < 1)
        {
            Debug.LogError($"[PathArrowGuide] Waypoints for {mission} are not assigned or empty.");
            return;
        }

        SpawnPath(waypoints);
    }

    /// <summary>
    /// Destroys all spawned arrows and the goal marker, and hides the distance label.
    /// </summary>
    public void ClearArrows()
    {
        foreach (var arrow in spawnedArrows)
            if (arrow != null) Destroy(arrow);

        spawnedArrows.Clear();
        currentGoal = null;

        if (distanceText != null) distanceText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Returns the last non-null waypoint of the given mission.
    /// Used by ROSPathRequester and ROSNavigator to build the PoseStamped goal.
    /// </summary>
    public Transform GetLastWaypoint(MissionType mission)
    {
        Transform[] waypoints = GetWaypoints(mission);
        if (waypoints == null || waypoints.Length == 0) return null;

        for (int i = waypoints.Length - 1; i >= 0; i--)
            if (waypoints[i] != null) return waypoints[i];

        return null;
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private Transform[] GetWaypoints(MissionType mission)
    {
        switch (mission)
        {
            case MissionType.MissionA: return waypointsA;
            case MissionType.MissionB: return waypointsB;
            case MissionType.MissionC: return waypointsC;
            default:                   return null;
        }
    }

    /// <summary>
    /// Iterates the waypoint array and spawns an arrow at each intermediate
    /// waypoint and a goal marker at the last one.
    /// </summary>
    private void SpawnPath(Transform[] waypoints)
    {
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
            {
                Debug.LogWarning($"[PathArrowGuide] Waypoint {i} is null, skipping.");
                continue;
            }

            if (i == waypoints.Length - 1)
                SpawnGoal(waypoints[i]);
            else
                SpawnArrow(waypoints[i]);
        }
    }

    private void SpawnArrow(Transform waypoint)
    {
        Vector3 pos = waypoint.position;
        pos.y += arrowHeight;

        GameObject arrow = Instantiate(arrowPrefab, pos, waypoint.rotation, arrowsParent);
        arrow.transform.localScale = Vector3.one * arrowScale;
        spawnedArrows.Add(arrow);
    }

    private void SpawnGoal(Transform waypoint)
    {
        Vector3 pos = waypoint.position;
        pos.y += arrowHeight;

        // Fall back to arrowPrefab if goalPrefab is not assigned
        GameObject prefabToUse = goalPrefab != null ? goalPrefab : arrowPrefab;
        if (goalPrefab == null)
            Debug.LogWarning("[PathArrowGuide] Goal prefab not assigned, using arrow prefab as fallback.");

        float scale = goalPrefab != null ? goalScale : arrowScale;

        GameObject goal = Instantiate(prefabToUse, pos, waypoint.rotation, arrowsParent);
        goal.transform.localScale = Vector3.one * scale;
        spawnedArrows.Add(goal);

        currentGoal = goal.transform;
    }
}