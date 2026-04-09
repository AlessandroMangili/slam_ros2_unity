using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Geometry;

[RequireComponent(typeof(LineRenderer))]
public class ROSPathVisualizer : MonoBehaviour
{
    [Header("Linea")]
    public float lineWidth = 0.05f;
    public Color lineColor = Color.cyan;

    [Header("Offset mappa")]
    public float offsetX = 5.02f;
    public float offsetZ = -10.02f;

    [Header("Altezza")]
    public float floorY = -2.8f;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.startWidth    = lineWidth;
        lr.endWidth      = lineWidth;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.startColor    = lineColor;
        lr.endColor      = lineColor;
        lr.positionCount = 0;
        lr.useWorldSpace = true;
    }

    public void UpdatePath(PoseStampedMsg[] poses)
    {
        if (poses == null || poses.Length == 0) { ClearPath(); return; }

        var points = new Vector3[poses.Length];

        for (int i = 0; i < poses.Length; i++)
        {
            float rx = (float)poses[i].pose.position.x;
            float ry = (float)poses[i].pose.position.y;

            points[i] = new Vector3(
                -ry + offsetX,
                floorY,
                 rx + offsetZ
            );
        }

        lr.positionCount = points.Length;
        lr.SetPositions(points);
    }

    public void ClearPath()
    {
        lr.positionCount = 0;
    }
}