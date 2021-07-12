using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibNoise.Unity.Generator;
using UnityEngine;

public class WorldGeneratorScript : MonoBehaviour
{
    public Mesh BlockMesh;
    public Material BlockMaterial;
    public uint ChunkSize;
    public double PerlinWeight;
    public double HeightWeight;
    public double GroundLevel;
    public bool OptimizeBlocks;

    private Dictionary<Vector3Int, GameObject> chunks = new Dictionary<Vector3Int, GameObject>();

    private Perlin perlinGenerator = new Perlin();

    // Start is called before the first frame update
    void Start()
    {
        for (var x = 0; x < 10; x++)
        {
            for (var y = 0; y < 2; y++)
            {
                for (var z = 0; z < 10; z++)
                {
                    CreateChunk(new Vector3Int(x, y, z));
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void CreateChunk(Vector3Int position)
    {
        var chunk = new GameObject($"Chunk {position.x};{position.y};{position.z}");
        chunk.transform.position = (Vector3)position * ChunkSize;
        var meshRenderer = chunk.AddComponent<MeshRenderer>();
        meshRenderer.material = BlockMaterial;
        var chunkScript = chunk.AddComponent<ChunkScript>();
        chunkScript.BlockMesh = BlockMesh;
        chunkScript.ChunkSize = ChunkSize;
        chunkScript.PerlinWeight = PerlinWeight;
        chunkScript.HeightWeight = HeightWeight;
        chunkScript.GroundLevel = GroundLevel;
        chunkScript.OptimizeBlocks = OptimizeBlocks;
        chunkScript.PerlinGenerator = perlinGenerator;
        chunks.Add(position, chunk);
    }
}
