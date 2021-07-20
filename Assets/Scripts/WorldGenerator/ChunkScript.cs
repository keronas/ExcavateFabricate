using System.Collections;
using System.Collections.Generic;
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
    public uint ChunkSize;
    public double PerlinWeight;
    public double HeightWeight;
    public double GroundLevel;
    public bool OptimizeBlocks;
    public Perlin PerlinGenerator;


    private byte[][][] data { get; set; }
    private List<GameObject> objects { get; set; } = new List<GameObject>();

    public void SetBlock(Vector3Int worldPosition, byte value)
    {
        var localPosition = worldPosition - transform.position;
        data[(int)localPosition.x][(int)localPosition.y][(int)localPosition.z] = value;
        RecreateMesh();
        AssignMeshToCollider();
    }

    // Start is called before the first frame update
    async void Start()  
    {
        var currentPosition = transform.position;
        await InitializeData(currentPosition);
        var meshId = RecreateMesh();
        await BakeMesh(meshId);
        AssignMeshToCollider();
    }

    private async Task InitializeData(Vector3 currentPosition)
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
                            data[x][y][z] = 1;
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


    /// <returns>Mesh ID</returns>
    private int RecreateMesh()
    {
        var combineInstances = new List<CombineInstance>();

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                for (var z = 0; z < ChunkSize; z++)
                {
                    if (data[x][y][z] != 0)
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
                            var combineInstance = new CombineInstance();
                            combineInstance.mesh = BlockMesh;
                            combineInstance.transform = Matrix4x4.Translate(new Vector3(x, y, z));
                            combineInstances.Add(combineInstance);
                        }
                    }
                }
            }
        }

        var meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh.CombineMeshes(combineInstances.ToArray());
        return meshFilter.mesh.GetInstanceID();
    }

    private async Task BakeMesh(int meshId)
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
