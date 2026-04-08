using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ArrowMesh : MonoBehaviour
{
    [SerializeField] private Color color = new Color(0f, 1f, 0.2f);

    void OnEnable() => GenerateArrow();
    void OnValidate() => GenerateArrow();

    void GenerateArrow()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        mf.sharedMesh = CreateArrowMesh();

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = color;
        mr.sharedMaterial = mat;
    }

    Mesh CreateArrowMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ArrowMesh";

        float shaftWidth  = 0.15f;
        float shaftLength = 0.6f;
        float headWidth   = 0.4f;
        float headLength  = 0.4f;
        float thickness   = 0.08f;

        Vector3[] rawVerts = new Vector3[]
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

        Vector3[] verts = new Vector3[rawVerts.Length];

        Quaternion rot = Quaternion.Euler(0f, 0f, 90f);

        for (int i = 0; i < rawVerts.Length; i++)
        {
            verts[i] = rot * rawVerts[i];
        }

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

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}