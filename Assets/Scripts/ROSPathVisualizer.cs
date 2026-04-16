using UnityEngine;
using RosMessageTypes.Geometry;

/// <summary>
/// Renders the path planned by Nav2 as a 3D line in the Unity scene
/// using a LineRenderer component.
///
/// Path poses arrive in ROS map coordinates and are converted to Unity
/// world space using the same origin offsets used by ROSPathRequester
/// and ROSNavigator.
///
/// Coordinate conversion (ROS map → Unity world):
///   unityX = -rosY + offsetX
///   unityY =  floorY          (constant — path is drawn flat on the floor)
///   unityZ =  rosX + offsetZ
///
/// The shader is resolved at runtime in priority order:
///   Unlit/Color → Sprites/Default → Standard
/// so the line colour is always visible regardless of the render pipeline.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ROSPathVisualizer : MonoBehaviour
{
    [Header("Line")]
    public float lineWidth = 0.05f;
    public Color lineColor = Color.cyan;

    [Header("Map Origin Offset")]
    [Tooltip("X offset between the ROS map origin and the Unity world origin")]
    public float offsetX = 5.02f;

    [Tooltip("Z offset between the ROS map origin and the Unity world origin")]
    public float offsetZ = -10.02f;

    [Header("Height")]
    [Tooltip("Fixed Y coordinate at which the path line is drawn in Unity world space")]
    public float floorY = -2.8f;

    private LineRenderer lr;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.startWidth    = lineWidth;
        lr.endWidth      = lineWidth;
        lr.useWorldSpace = true;
        lr.positionCount = 0;

        // Resolve a shader in priority order to support all render pipelines
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
            Debug.LogError("[ROSPathVisualizer] No valid shader found — path line will not be visible.");
        }

        lr.startColor = lineColor;
        lr.endColor   = lineColor;
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Converts the array of ROS PoseStamped poses to Unity world positions
    /// and updates the LineRenderer. Clears the line if the array is empty.
    /// </summary>
    public void UpdatePath(PoseStampedMsg[] poses)
    {
        if (poses == null || poses.Length == 0)
        {
            ClearPath();
            return;
        }

        var points = new Vector3[poses.Length];

        for (int i = 0; i < poses.Length; i++)
        {
            float rx = (float)poses[i].pose.position.x;
            float ry = (float)poses[i].pose.position.y;

            // Convert ROS map (x = forward, y = left) → Unity world (x, y, z)
            points[i] = new Vector3(
                -ry + offsetX,   // ROS y (left)    → Unity -X + offsetX
                 floorY,         // constant height
                 rx + offsetZ    // ROS x (forward) → Unity  Z + offsetZ
            );
        }

        lr.positionCount = points.Length;
        lr.SetPositions(points);

        Debug.Log($"[ROSPathVisualizer] {points.Length} points drawn. " +
                  $"First: {points[0]}  Last: {points[points.Length - 1]}");
    }

    /// <summary>Hides the path line by setting the point count to zero.</summary>
    public void ClearPath()
    {
        lr.positionCount = 0;
    }
}