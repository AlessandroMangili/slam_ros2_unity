using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PathArrowGuide : MonoBehaviour
{
    [Header("Waypoints per missione")]
    public Transform[] waypointsA;
    public Transform[] waypointsB;
    public Transform[] waypointsC;

    [Header("Prefabs")]
    public GameObject arrowPrefab;
    public GameObject goalPrefab;
    public Transform arrowsParent;

    [Header("Arrow Layout")]
    public float arrowHeight = 0.15f;
    public float arrowScale = 0.4f;
    public float goalScale = 0.6f;

    [Header("Proximity Check")]
    public Transform robot;
    public float goalReachedDistance = 0.6f;
    public MissionMenuManager missionMenuManager;

    [Header("UI")]
    public TextMeshProUGUI distanceText;

    private readonly List<GameObject> spawnedArrows = new List<GameObject>();
    private Transform currentGoal;
    private bool missionCompleted;

    void Update()
    {
        if (missionCompleted)
        {
            if (distanceText != null)
                distanceText.gameObject.SetActive(false);
            return;
        }

        if (robot == null || currentGoal == null)
        {
            if (distanceText != null)
                distanceText.gameObject.SetActive(false);
            return;
        }

        float distance = Vector3.Distance(robot.position, currentGoal.position);

        if (distanceText != null)
        {
            distanceText.gameObject.SetActive(true);
            distanceText.text = $"Goal distance: {distance:0.00} m";
        }

        if (distance <= goalReachedDistance)
        {
            missionCompleted = true;

            if (missionMenuManager != null)
                missionMenuManager.OnMissionCompleted();

            ClearArrows();
        }
    }

    public void ShowMission(MissionType mission)
    {
        ClearArrows();
        missionCompleted = false;
        currentGoal = null;

        if (distanceText != null)
            distanceText.gameObject.SetActive(true);

        if (mission == MissionType.None) return;

        Transform[] waypoints = GetWaypoints(mission);
        if (waypoints == null || waypoints.Length < 1)
        {
            Debug.LogError($"Waypoints per {mission} non assegnati o insufficienti!");
            return;
        }

        SpawnPath(waypoints);
    }

    private Transform[] GetWaypoints(MissionType mission)
    {
        switch (mission)
        {
            case MissionType.MissionA: return waypointsA;
            case MissionType.MissionB: return waypointsB;
            case MissionType.MissionC: return waypointsC;
            default: return null;
        }
    }

    private void SpawnPath(Transform[] waypoints)
    {
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
            {
                Debug.LogWarning($"Waypoint {i} è null, saltato.");
                continue;
            }

            bool isLast = i == waypoints.Length - 1;

            if (isLast)
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

        GameObject goalToUse = goalPrefab != null ? goalPrefab : arrowPrefab;
        if (goalPrefab == null)
            Debug.LogWarning("Goal prefab non assegnato, spawno una freccia normale.");

        GameObject goal = Instantiate(goalToUse, pos, waypoint.rotation, arrowsParent);
        goal.transform.localScale = Vector3.one * (goalPrefab != null ? goalScale : arrowScale);
        spawnedArrows.Add(goal);

        currentGoal = goal.transform;
    }

    public void ClearArrows()
    {
        foreach (var arrow in spawnedArrows)
        {
            if (arrow != null)
                Destroy(arrow);
        }

        spawnedArrows.Clear();
        currentGoal = null;

        if (distanceText != null)
            distanceText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Restituisce l'ultimo waypoint della missione (il goal).
    /// Usato da ROSPathRequester per costruire la PoseStamped.
    /// </summary>
    public Transform GetLastWaypoint(MissionType mission)
    {
        Transform[] waypoints = GetWaypoints(mission);
        if (waypoints == null || waypoints.Length == 0) return null;
        // Cerca l'ultimo non-null
        for (int i = waypoints.Length - 1; i >= 0; i--)
            if (waypoints[i] != null) return waypoints[i];
        return null;
    }
}