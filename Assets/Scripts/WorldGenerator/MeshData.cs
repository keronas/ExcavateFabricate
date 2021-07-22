using UnityEngine;

public struct MeshData
{
    public MeshData(Vector3[] vertices, Vector3[] normals, int[] triangles, Color32[] colors)
    {
        Vertices = vertices;
        Normals = normals;
        Triangles = triangles;
        Colors = colors;
    }

    public MeshData(Mesh baseMesh)
    {
        Vertices = baseMesh.vertices;
        Normals = baseMesh.normals;
        Triangles = baseMesh.triangles;
        Colors = baseMesh.colors32;
    }

    public Vector3[] Vertices { get; }
    public Vector3[] Normals { get; }
    public int[] Triangles { get; }
    public Color32[] Colors { get; }
}
