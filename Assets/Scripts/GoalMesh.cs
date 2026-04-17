using UnityEngine;

/// <summary>
/// Procedurally generates a flat 3D ring mesh to mark the mission goal.
///
/// The ring is built by subdividing a full circle into <segments> trapezoid
/// slices, each composed of two triangles on the top face and two on the
/// bottom face. The result is a torus-like disc with a hollow centre.
///
/// The material is rendered with culling disabled so the ring is visible
/// from both above and below.
///
/// Mesh is regenerated automatically in Awake() and whenever a serialised
/// field is changed in the Inspector via OnValidate().
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GoalMesh : MonoBehaviour
{
    [SerializeField] private Color color = new Color(1f, 0.8f, 0f);

    [Header("Dimensions")]
    [Tooltip("Outer radius of the ring in metres")]
    [SerializeField] private float radius      = 0.3f;

    [Tooltip("Inner radius (hole) of the ring in metres")]
    [SerializeField] private float innerRadius = 0.15f;

    [Tooltip("Vertical thickness of the ring in metres")]
    [SerializeField] private float thickness   = 0.06f;

    [Tooltip("Number of subdivisions around the circumference")]
    [SerializeField] private int   segments    = 32;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Awake()      => GenerateGoal();
    void OnValidate() => GenerateGoal();

    // ─── Mesh generation ─────────────────────────────────────────────────────

    void GenerateGoal()
    {
        MeshFilter   mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        mf.sharedMesh = CreateRingMesh();

        // Resolve shader in priority order to support all render pipelines
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError("[GoalMesh] No valid shader found.");
            return;
        }

        Material mat = new Material(shader);
        mat.color = color;

        // Disable back-face culling so the ring is visible from both sides
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        mr.sharedMaterial = mat;
    }

    Mesh CreateRingMesh()
    {
        Mesh mesh  = new Mesh { name = "GoalMesh" };
        float halfT = thickness / 2f;

        // 8 vertices per segment: 4 top + 4 bottom
        Vector3[] verts = new Vector3[segments * 8];

        // 12 triangle indices per segment: 6 top (2 tris) + 6 bottom (2 tris)
        int[] tris = new int[segments * 12];

        for (int i = 0; i < segments; i++)
        {
            float a0 = (i       / (float)segments) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;

            int vi = i * 8;

            // Top face vertices (y = +halfT)
            verts[vi + 0] = new Vector3(Mathf.Cos(a0) * radius,       +halfT, Mathf.Sin(a0) * radius);
            verts[vi + 1] = new Vector3(Mathf.Cos(a0) * innerRadius,  +halfT, Mathf.Sin(a0) * innerRadius);
            verts[vi + 2] = new Vector3(Mathf.Cos(a1) * radius,       +halfT, Mathf.Sin(a1) * radius);
            verts[vi + 3] = new Vector3(Mathf.Cos(a1) * innerRadius,  +halfT, Mathf.Sin(a1) * innerRadius);

            // Bottom face vertices (y = -halfT)
            verts[vi + 4] = new Vector3(Mathf.Cos(a0) * radius,       -halfT, Mathf.Sin(a0) * radius);
            verts[vi + 5] = new Vector3(Mathf.Cos(a0) * innerRadius,  -halfT, Mathf.Sin(a0) * innerRadius);
            verts[vi + 6] = new Vector3(Mathf.Cos(a1) * radius,       -halfT, Mathf.Sin(a1) * radius);
            verts[vi + 7] = new Vector3(Mathf.Cos(a1) * innerRadius,  -halfT, Mathf.Sin(a1) * innerRadius);

            int ti = i * 12;

            // Top face — winding order: CCW when viewed from above (+Y)
            tris[ti + 0] = vi + 0; tris[ti + 1] = vi + 2; tris[ti + 2] = vi + 1;
            tris[ti + 3] = vi + 1; tris[ti + 4] = vi + 2; tris[ti + 5] = vi + 3;

            // Bottom face — winding order reversed so normals point down (-Y)
            tris[ti + 6]  = vi + 4; tris[ti + 7]  = vi + 5; tris[ti + 8]  = vi + 6;
            tris[ti + 9]  = vi + 5; tris[ti + 10] = vi + 7; tris[ti + 11] = vi + 6;
        }

        mesh.vertices  = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}