using UnityEngine;

/// <summary>
/// Procedurally generates a 3D arrow mesh at edit and runtime.
///
/// The arrow is composed of two parts:
///   - A rectangular shaft with configurable width and length.
///   - A triangular arrowhead attached at the tip of the shaft.
///
/// Both parts have thickness so the arrow is visible from any angle.
/// The raw vertices are defined along the Z axis and then rotated 90°
/// around Z so the arrow points along the X axis in world space.
///
/// [ExecuteAlways] ensures the mesh is regenerated immediately when
/// any serialised field is changed in the Inspector.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ArrowMesh : MonoBehaviour
{
    [SerializeField] private Color color = new Color(0f, 1f, 0.2f);

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void OnEnable()   => GenerateArrow();
    void OnValidate() => GenerateArrow();

    // ─── Mesh generation ─────────────────────────────────────────────────────

    void GenerateArrow()
    {
        MeshFilter   mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        mf.sharedMesh = CreateArrowMesh();

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color         = color;
        mr.sharedMaterial = mat;
    }

    Mesh CreateArrowMesh()
    {
        Mesh mesh  = new Mesh { name = "ArrowMesh" };

        // Arrow dimensions
        float shaftWidth  = 0.15f;
        float shaftLength = 0.6f;
        float headWidth   = 0.4f;
        float headLength  = 0.4f;
        float thickness   = 0.08f;

        // Vertices are defined with the arrow pointing along +Z.
        // They are rotated 90° around Z at the end to align with +X.
        Vector3[] rawVerts = new Vector3[]
        {
            // Shaft — top face (y = +thickness/2)
            new Vector3(-shaftWidth / 2,  thickness / 2,  0),
            new Vector3( shaftWidth / 2,  thickness / 2,  0),
            new Vector3( shaftWidth / 2,  thickness / 2,  shaftLength),
            new Vector3(-shaftWidth / 2,  thickness / 2,  shaftLength),

            // Shaft — bottom face (y = -thickness/2)
            new Vector3(-shaftWidth / 2, -thickness / 2,  0),
            new Vector3( shaftWidth / 2, -thickness / 2,  0),
            new Vector3( shaftWidth / 2, -thickness / 2,  shaftLength),
            new Vector3(-shaftWidth / 2, -thickness / 2,  shaftLength),

            // Arrowhead — top face
            new Vector3(-headWidth / 2,  thickness / 2,  shaftLength),
            new Vector3( headWidth / 2,  thickness / 2,  shaftLength),
            new Vector3( 0,             thickness / 2,  shaftLength + headLength),

            // Arrowhead — bottom face
            new Vector3(-headWidth / 2, -thickness / 2,  shaftLength),
            new Vector3( headWidth / 2, -thickness / 2,  shaftLength),
            new Vector3( 0,            -thickness / 2,  shaftLength + headLength),
        };

        // Rotate all vertices 90° around Z so the arrow points along +X
        Quaternion rot   = Quaternion.Euler(0f, 0f, 90f);
        Vector3[]  verts = new Vector3[rawVerts.Length];
        for (int i = 0; i < rawVerts.Length; i++)
            verts[i] = rot * rawVerts[i];

        int[] tris = new int[]
        {
            // Shaft top and bottom faces
            0, 2, 1,   0, 3, 2,
            4, 5, 6,   4, 6, 7,

            // Arrowhead top and bottom faces
            8, 10, 9,
            11, 12, 13,

            // Shaft side faces
            1, 2, 6,   1, 6, 5,     // right
            3, 0, 4,   3, 4, 7,     // left
            0, 1, 5,   0, 5, 4,     // back
            2, 3, 7,   2, 7, 6,     // front

            // Arrowhead side faces
            8,  11, 10,   11, 13, 10,
            9,  10, 12,   10, 13, 12,
            8,  9,  12,   8,  12, 11,
        };

        mesh.vertices  = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}