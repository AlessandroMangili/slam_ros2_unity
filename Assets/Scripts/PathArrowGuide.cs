using System.Collections.Generic;
using UnityEngine;

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
    public float arrowScale  = 0.4f;
    public float goalScale   = 0.6f;

    private readonly List<GameObject> spawnedArrows = new List<GameObject>();

    public void ShowMission(MissionType mission)
    {
        ClearArrows();
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
        if (goalPrefab == null)
        {
            Debug.LogWarning("Goal prefab non assegnato, spawno una freccia normale.");
            SpawnArrow(waypoint);
            return;
        }

        Vector3 pos = waypoint.position;
        pos.y += arrowHeight;

        GameObject goal = Instantiate(goalPrefab, pos, waypoint.rotation, arrowsParent);
        goal.transform.localScale = Vector3.one * goalScale;
        spawnedArrows.Add(goal);
    }

    public void ClearArrows()
    {
        foreach (var arrow in spawnedArrows)
            if (arrow != null)
                Destroy(arrow);
        spawnedArrows.Clear();
    }
}