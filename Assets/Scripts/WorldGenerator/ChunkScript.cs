using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibNoise.Unity;
using LibNoise.Unity.Generator;
using UnityEngine;

[RequireComponent(typeof (MeshFilter))]
[RequireComponent(typeof (MeshRenderer))]
[RequireComponent(typeof (MeshCollider))]
public class ChunkScript : MonoBehaviour
{
    public Mesh BlockMesh;
    public Color32[] BlockColors;
    public TimeSpan[] BlockDestroyDurations;
    public uint ChunkSize;
    public double PerlinWeight;
    public double HeightWeight;
    public double GroundLevel;
    public uint LayerHeight;
    public bool OptimizeBlocks;
    public Perlin PerlinGenerator;

    private MeshData blockMeshData;
    private byte[][][] data { get; set; }
    private List<GameObject> objects { get; set; } = new List<GameObject>();

    public void SetBlock(Vector3Int worldPosition, byte value)
    {
        var localPosition = worldPosition - transform.position;
        data[(int)localPosition.x][(int)localPosition.y][(int)localPosition.z] = value;
        AssignMeshDataToFilter(CreateMeshData(blockMeshData));
        AssignMeshToCollider();
    }

    public TimeSpan GetBlockDestroyDuration(Vector3Int worldPosition)
    {
        var localPosition = worldPosition - transform.position;
        var blockValue = data[(int)localPosition.x][(int)localPosition.y][(int)localPosition.z];
        return BlockDestroyDurations[blockValue - 1];
    }

    // Start is called before the first frame update
    async void Start()  
    {
        blockMeshData = new MeshData(BlockMesh);
        await InitializeDataAsync(transform.position);
        var meshData = await Task.Run(() => CreateMeshData(blockMeshData));
        var meshId = AssignMeshDataToFilter(meshData);
        await BakeMeshAsync(meshId);
        AssignMeshToCollider();
    }

    private async Task InitializeDataAsync(Vector3 currentPosition)
    {
        await Task.Run(() =>
        {
            data = new byte[ChunkSize][][];
            for (var x = 0; x < ChunkSize; x++)
            {
                data[x] = new byte[ChunkSize][];
                for (var y = 0; y < ChunkSize; y++)
                {
                    data[x][y] = new byte[ChunkSize];
                    for (var z = 0; z < ChunkSize; z++)
                    {
                        var localPosition = new Vector3(x, y, z);
                        var worldPosition = currentPosition + localPosition;
                        var perlinPositionMultiplier = 0.04f;
                        var perlinValue = PerlinGenerator.GetValue(worldPosition * perlinPositionMultiplier);
                        var heightValue = GroundLevel - worldPosition.y;

                        if (perlinValue * PerlinWeight + heightValue * HeightWeight > 1)
                        {
                            data[x][y][z] = (byte)(Mathf.Clamp(worldPosition.y / LayerHeight, 0, BlockColors.Length - 1) + 1);
                        }
                        else
                        {
                            data[x][y][z] = 0;
                        }
                    }
                }
            }
        });
    }

    private MeshData CreateMeshData(MeshData blockMeshData)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        var colors = new List<Color32>();

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                for (var z = 0; z < ChunkSize; z++)
                {
                    var blockValue = data[x][y][z];
                    if (blockValue != 0)
                    {
                        // If any neighbouring space outside chunk or doesn't contain any block, then current block may be visible
                        if (!OptimizeBlocks ||
                            (x - 1 < 0 || data[x - 1][y][z] == 0) ||
                            (y - 1 < 0 || data[x][y - 1][z] == 0) ||
                            (z - 1 < 0 || data[x][y][z - 1] == 0) ||
                            (x + 1 >= ChunkSize || data[x + 1][y][z] == 0) ||
                            (y + 1 >= ChunkSize || data[x][y + 1][z] == 0) ||
                            (z + 1 >= ChunkSize || data[x][y][z + 1] == 0))
                        {
                            var existingVerticesCount = vertices.Count;
                            vertices.AddRange(blockMeshData.Vertices.Select(vertex => vertex + new Vector3(x, y, z)));
                            normals.AddRange(blockMeshData.Normals);
                            triangles.AddRange(blockMeshData.Triangles.Select(vertexId => vertexId + existingVerticesCount));
                            colors.AddRange(Enumerable.Repeat(BlockColors[blockValue - 1], blockMeshData.Vertices.Length));
                        }
                    }
                }
            }
        }

        return new MeshData(vertices.ToArray(), normals.ToArray(), triangles.ToArray(), colors.ToArray());
    }

    /// <returns>Mesh ID</returns>
    private int AssignMeshDataToFilter(MeshData meshData)
    {
        var mesh = new Mesh();
        mesh.vertices = meshData.Vertices;
        mesh.normals = meshData.Normals;
        mesh.triangles = meshData.Triangles;
        mesh.colors32 = meshData.Colors;
        var meshFilter = GetComponent<MeshFilter>();
        Destroy(meshFilter.mesh);
        meshFilter.mesh = mesh;
        return meshFilter.mesh.GetInstanceID();
    }

    private async Task BakeMeshAsync(int meshId)
    {
        await Task.Run(() => Physics.BakeMesh(meshId, false));
    }

    private void AssignMeshToCollider()
    {
        var meshFilter = GetComponent<MeshFilter>();
        var meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
