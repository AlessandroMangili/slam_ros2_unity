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
        lr.useWorldSpace = true;
        lr.positionCount = 0;

        // Prova shader in ordine fino a trovarne uno valido
        Shader shader = Shader.Find("Unlit/Color")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Standard");

        if (shader != null)
        {
            lr.material       = new Material(shader);
            lr.material.color = lineColor;
        }
        else
        {
            Debug.LogError("[ROSPathVisualizer] Nessuno shader trovato!");
        }

        lr.startColor = lineColor;
        lr.endColor   = lineColor;
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

        Debug.Log($"[Visualizer] {points.Length} punti. " +
                  $"Primo: {points[0]}  Ultimo: {points[points.Length - 1]}");
    }

    public void ClearPath()
    {
        lr.positionCount = 0;
    }
}