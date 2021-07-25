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
    public ChunkSettingsScript ChunkSettings;
    public Perlin PerlinGenerator;

    private MeshData blockMeshData;
    public byte[][][] Data { get; set; }
    public Vector3Int Position { get; private set; }
    public bool DoneInitializing { get; private set; } = false;

    public void SetBlock(Vector3Int worldPosition, byte value)
    {
        var localPosition = worldPosition - transform.position;
        Data[(int)localPosition.x][(int)localPosition.y][(int)localPosition.z] = value;
        AssignMeshDataToFilter(CreateMeshData(blockMeshData));
        AssignMeshToCollider();
    }

    public TimeSpan GetBlockDestroyDuration(Vector3Int worldPosition)
    {
        var localPosition = worldPosition - transform.position;
        var blockValue = Data[(int)localPosition.x][(int)localPosition.y][(int)localPosition.z];
        return TimeSpan.FromMilliseconds(ChunkSettings.BlockDestroyDurationsMillis[blockValue - 1]);
    }

    // Start is called before the first frame update
    async void Start()  
    {
        try
        {
            Position = Vector3Int.RoundToInt(transform.position) / (int)ChunkSettings.ChunkSize;
            blockMeshData = new MeshData(ChunkSettings.BlockMesh);
            if (Data == null) // data not been set manually
            {
                await InitializeDataAsync(transform.position);
            }
            var meshData = await Task.Run(() => CreateMeshData(blockMeshData));
            var meshId = AssignMeshDataToFilter(meshData);
            await BakeMeshAsync(meshId);
            AssignMeshToCollider();
            DoneInitializing = true;
        }
        catch (MissingReferenceException e) when (e.Message.Contains($"The object of type '{nameof(ChunkScript)}"))
        {
            // this object has already been destroyed, ignore exception
        }
    }

    private async Task InitializeDataAsync(Vector3 currentPosition)
    {
        await Task.Run(() =>
        {
            var chunkSize = ChunkSettings.ChunkSize;
            Data = new byte[chunkSize][][];
            for (var x = 0; x < chunkSize; x++)
            {
                Data[x] = new byte[chunkSize][];
                for (var y = 0; y < chunkSize; y++)
                {
                    Data[x][y] = new byte[chunkSize];
                    for (var z = 0; z < chunkSize; z++)
                    {
                        var localPosition = new Vector3(x, y, z);
                        var worldPosition = currentPosition + localPosition;
                        var perlinPositionMultiplier = 0.04f;
                        var perlinValue = PerlinGenerator.GetValue(worldPosition * perlinPositionMultiplier);
                        var heightValue = ChunkSettings.GroundLevel - worldPosition.y;

                        if (perlinValue * ChunkSettings.PerlinWeight + heightValue * ChunkSettings.HeightWeight > 1)
                        {
                            Data[x][y][z] = (byte)(Mathf.Clamp(worldPosition.y / ChunkSettings.LayerHeight, 0, ChunkSettings.BlockColors.Length - 1) + 1);
                        }
                        else
                        {
                            Data[x][y][z] = 0;
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
        var chunkSize = ChunkSettings.ChunkSize;

        for (var x = 0; x < chunkSize; x++)
        {
            for (var y = 0; y < chunkSize; y++)
            {
                for (var z = 0; z < chunkSize; z++)
                {
                    var blockValue = Data[x][y][z];
                    if (blockValue != 0)
                    {
                        // If any neighbouring space outside chunk or doesn't contain any block, then current block may be visible
                        if (!ChunkSettings.OptimizeBlocks ||
                            (x - 1 < 0 || Data[x - 1][y][z] == 0) ||
                            (y - 1 < 0 || Data[x][y - 1][z] == 0) ||
                            (z - 1 < 0 || Data[x][y][z - 1] == 0) ||
                            (x + 1 >= chunkSize || Data[x + 1][y][z] == 0) ||
                            (y + 1 >= chunkSize || Data[x][y + 1][z] == 0) ||
                            (z + 1 >= chunkSize || Data[x][y][z + 1] == 0))
                        {
                            var existingVerticesCount = vertices.Count;
                            vertices.AddRange(blockMeshData.Vertices.Select(vertex => vertex + new Vector3(x, y, z)));
                            normals.AddRange(blockMeshData.Normals);
                            triangles.AddRange(blockMeshData.Triangles.Select(vertexId => vertexId + existingVerticesCount));
                            colors.AddRange(Enumerable.Repeat(ChunkSettings.BlockColors[blockValue - 1], blockMeshData.Vertices.Length));
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
        Destroy(meshFilter.sharedMesh);
        meshFilter.sharedMesh = mesh;
        return meshFilter.sharedMesh.GetInstanceID();
    }

    private async Task BakeMeshAsync(int meshId)
    {
        await Task.Run(() => Physics.BakeMesh(meshId, false));
    }

    private void AssignMeshToCollider()
    {
        var meshFilter = GetComponent<MeshFilter>();
        var meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.sharedMesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
