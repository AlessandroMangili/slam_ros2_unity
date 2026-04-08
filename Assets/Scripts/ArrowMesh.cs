using UnityEngine;

[ExecuteAlways]
public class ArrowMesh : MonoBehaviour
{
    [SerializeField] Color color = new Color(0f, 1f, 0.2f);

    void OnEnable() => GenerateArrow();
    void OnValidate() => GenerateArrow();

    void GenerateArrow()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) mf = gameObject.AddComponent<MeshFilter>();

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = CreateArrowMesh();
        mf.sharedMesh = mesh; // ← sharedMesh invece di mesh

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mr.sharedMaterial = mat; // ← sharedMaterial invece di material
    }

    Mesh CreateArrowMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ArrowMesh";

        float shaftWidth  = 0.075f;
        float shaftLength = 0.3f;
        float headWidth   = 0.2f;
        float headLength  = 0.2f;
        float thickness   = 0.04f;

        Vector3[] verts = new Vector3[]
        {
            new Vector3(-shaftWidth/2,  thickness/2,  0),
            new Vector3( shaftWidth/2,  thickness/2,  0),
            new Vector3( shaftWidth/2,  thickness/2,  shaftLength),
            new Vector3(-shaftWidth/2,  thickness/2,  shaftLength),
            new Vector3(-shaftWidth/2, -thickness/2,  0),
            new Vector3( shaftWidth/2, -thickness/2,  0),
            new Vector3( shaftWidth/2, -thickness/2,  shaftLength),
            new Vector3(-shaftWidth/2, -thickness/2,  shaftLength),
            new Vector3(-headWidth/2,  thickness/2,  shaftLength),
            new Vector3( headWidth/2,  thickness/2,  shaftLength),
            new Vector3( 0,            thickness/2,  shaftLength + headLength),
            new Vector3(-headWidth/2, -thickness/2,  shaftLength),
            new Vector3( headWidth/2, -thickness/2,  shaftLength),
            new Vector3( 0,           -thickness/2,  shaftLength + headLength),
        };

        int[] tris = new int[]
        {
            0, 2, 1,   0, 3, 2,
            4, 5, 6,   4, 6, 7,
            8, 10, 9,
            11, 12, 13,
            1, 2, 6,   1, 6, 5,
            3, 0, 4,   3, 4, 7,
            0, 1, 5,   0, 5, 4,
            2, 3, 7,   2, 7, 6,
            8, 11, 10,  11, 13, 10,
            9, 10, 12,  10, 13, 12,
            8, 9, 12,   8, 12, 11,
        };

        mesh.vertices  = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}