using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GoalMesh : MonoBehaviour
{
    [SerializeField] private Color color = new Color(1f, 0.8f, 0f);

    [Header("Dimensioni")]
    [SerializeField] private float radius      = 0.3f;
    [SerializeField] private float innerRadius = 0.15f;
    [SerializeField] private float thickness   = 0.06f;
    [SerializeField] private int   segments    = 32;

    void Awake()      => GenerateGoal();
    void OnValidate() => GenerateGoal();

    void GenerateGoal()
    {
        MeshFilter   mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        mf.sharedMesh = CreateRingMesh();

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError("Nessuno shader trovato!");
            return;
        }

        Material mat = new Material(shader);
        mat.color = color;

        // Disabilita il culling: visibile da entrambi i lati
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        mr.sharedMaterial = mat;
    }

    Mesh CreateRingMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "GoalMesh";

        float halfT = thickness / 2f;

        // 4 vertici per segmento per il top, 4 per il bottom
        int vertCount = segments * 8;
        Vector3[] verts = new Vector3[vertCount];
        // Top: 6 indici, Bottom: 6 indici per segmento
        int[] tris = new int[segments * 12];

        for (int i = 0; i < segments; i++)
        {
            float a0 = (i       / (float)segments) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;

            // --- TOP (y = +halfT) ---
            int vi = i * 8;
            verts[vi + 0] = new Vector3(Mathf.Cos(a0) * radius,      +halfT, Mathf.Sin(a0) * radius);
            verts[vi + 1] = new Vector3(Mathf.Cos(a0) * innerRadius,  +halfT, Mathf.Sin(a0) * innerRadius);
            verts[vi + 2] = new Vector3(Mathf.Cos(a1) * radius,      +halfT, Mathf.Sin(a1) * radius);
            verts[vi + 3] = new Vector3(Mathf.Cos(a1) * innerRadius,  +halfT, Mathf.Sin(a1) * innerRadius);

            // --- BOTTOM (y = -halfT) ---
            verts[vi + 4] = new Vector3(Mathf.Cos(a0) * radius,      -halfT, Mathf.Sin(a0) * radius);
            verts[vi + 5] = new Vector3(Mathf.Cos(a0) * innerRadius,  -halfT, Mathf.Sin(a0) * innerRadius);
            verts[vi + 6] = new Vector3(Mathf.Cos(a1) * radius,      -halfT, Mathf.Sin(a1) * radius);
            verts[vi + 7] = new Vector3(Mathf.Cos(a1) * innerRadius,  -halfT, Mathf.Sin(a1) * innerRadius);

            int ti = i * 12;

            // Top face
            tris[ti + 0] = vi + 0; tris[ti + 1] = vi + 2; tris[ti + 2] = vi + 1;
            tris[ti + 3] = vi + 1; tris[ti + 4] = vi + 2; tris[ti + 5] = vi + 3;

            // Bottom face (triangoli invertiti per normali verso il basso)
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