using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Geometry;

[RequireComponent(typeof(LineRenderer))]
public class ROSPathVisualizer : MonoBehaviour
{
    [Header("Stile")]
    public float lineWidth    = 0.05f;
    public Color lineColor    = Color.cyan;
    public float heightOffset = 0.05f;

    [Header("Offset origine mappa (Unity - ROS)")]
    public float offsetX = 5.02f;   // = posizione X robot Unity
    public float offsetZ = -10.02f; // = posizione Z robot Unity

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.startWidth  = lineWidth;
        lr.endWidth    = lineWidth;
        lr.material    = new Material(Shader.Find("Sprites/Default"));
        lr.startColor  = lineColor;
        lr.endColor    = lineColor;
        lr.positionCount = 0;
    }

    // Riceve PoseStampedMsg[] da Nav.PathMsg.poses
    public void UpdatePath(PoseStampedMsg[] poses)
    {
        if (poses == null || poses.Length == 0) { ClearPath(); return; }

        lr.positionCount = poses.Length;

        for (int i = 0; i < poses.Length; i++)
        {
            float rx = (float)poses[i].pose.position.x; // ROS X → Unity Z
            float ry = (float)poses[i].pose.position.y; // ROS Y → Unity -X

            Vector3 unityPos = new Vector3(
                -ry + offsetX,   // Unity X
                heightOffset,    // Unity Y (altezza fissa)
                rx + offsetZ    // Unity Z
            );

            lr.SetPosition(i, unityPos);
        }

        Debug.Log($"[Visualizer] Path: {poses.Length} punti. " +
                $"Primo: {lr.GetPosition(0)}  Ultimo: {lr.GetPosition(poses.Length-1)}");
    }

    public void ClearPath()
    {
        lr.positionCount = 0;
    }
}