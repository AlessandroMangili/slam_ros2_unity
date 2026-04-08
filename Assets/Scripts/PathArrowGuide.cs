using System.Collections.Generic;
using UnityEngine;

public class PathArrowGuide : MonoBehaviour
{
    [Header("Waypoints per missione")]
    public Transform[] waypointsA;
    public Transform[] waypointsB;
    public Transform[] waypointsC;

    [Header("Arrow Prefab")]
    public GameObject arrowPrefab;
    public Transform arrowsParent;

    [Header("Arrow Layout")]
    public float arrowSpacing = 0.6f;
    public float arrowHeight  = 0.05f;

    private readonly List<GameObject> spawnedArrows = new List<GameObject>();

    public void ShowMission(MissionType mission)
    {
        ClearArrows();

        if (mission == MissionType.None) return;

        Transform[] waypoints = GetWaypoints(mission);

        if (waypoints == null || waypoints.Length < 2)
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
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] == null || waypoints[i + 1] == null)
            {
                Debug.LogWarning($"Waypoint {i} o {i+1} è null, saltato.");
                continue;
            }

            SpawnSegment(waypoints[i].position, waypoints[i + 1].position);
        }
    }

    private void SpawnSegment(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        direction.y = 0f;
        float length = direction.magnitude;
        if (length < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(direction.normalized, Vector3.up);
        int count = Mathf.Max(1, Mathf.FloorToInt(length / arrowSpacing));

        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;
            Vector3 pos = Vector3.Lerp(start, end, t);

            pos.y = start.y + 0.15f;  // ← più in alto (era 0.01f)

            GameObject arrow = Instantiate(arrowPrefab, pos, rot, arrowsParent);

            arrow.transform.localScale = Vector3.one * 0.4f;  // ← doppio (era 0.2f)

            spawnedArrows.Add(arrow);
        }
    }

    public void ClearArrows()
    {
        foreach (var arrow in spawnedArrows)
            if (arrow != null)
                Destroy(arrow);

        spawnedArrows.Clear();
    }
}