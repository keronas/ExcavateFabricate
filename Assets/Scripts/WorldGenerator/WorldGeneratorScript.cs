using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LibNoise.Unity.Generator;
using UnityEngine;

public class WorldGeneratorScript : MonoBehaviour
{
    public Mesh BlockMesh;
    public Material BlockMaterial;
    public Transform ChunkViewCenter;
    public uint ChunkSize;
    public uint ChunkViewDistance;
    public double PerlinWeight;
    public double HeightWeight;
    public double GroundLevel;
    public bool OptimizeBlocks;

    private Dictionary<Vector3Int, GameObject> allChunks = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> activeChunks = new Dictionary<Vector3Int, GameObject>();
    private Queue<Vector3Int> chunksToCreate = new Queue<Vector3Int>();

    private Perlin perlinGenerator = new Perlin();

    public void CreateBlock(Vector3Int worldPosition)
    {
        var chunkPosition = Vector3Int.FloorToInt((Vector3)worldPosition / ChunkSize); // explicit floor needed for negative numbers
        var chunk = activeChunks[chunkPosition].GetComponent<ChunkScript>();
        chunk.SetBlock(worldPosition, 1);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (chunksToCreate.Any())
        {
            CreateChunk(chunksToCreate.Dequeue());
        }
        RefreshChunks();
    }

    private void RefreshChunks()
    {
        var centerPosition = Vector3Int.RoundToInt(ChunkViewCenter.position) / (int)ChunkSize;
        var newActiveChunks = new Dictionary<Vector3Int, GameObject>();

        for (var z = -(int)ChunkViewDistance; z < ChunkViewDistance; z++)
        {
            var circleWidthAtZ = (int)Mathf.Sqrt(ChunkViewDistance * ChunkViewDistance - z * z); // pythagorean theorem to get width at given Z
            for (var x = -circleWidthAtZ; x < circleWidthAtZ; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    var position = new Vector3Int(centerPosition.x + x, y, centerPosition.z + z);
                    if (allChunks.ContainsKey(position))
                    {
                        var chunk = allChunks[position];
                        newActiveChunks.Add(position, chunk);
                        chunk.SetActive(true);
                    }
                    else if (!chunksToCreate.Contains(position))
                    {
                        chunksToCreate.Enqueue(position);
                        newActiveChunks.Add(position, null);
                    }
                }
            }
        }

        activeChunks.Except(newActiveChunks).ToList().ForEach(pair => pair.Value?.SetActive(false)); // deactivate chunks outside new viewrange
        activeChunks = newActiveChunks;
    }

    private GameObject CreateChunk(Vector3Int position)
    {
        var chunk = new GameObject($"Chunk {position.x};{position.y};{position.z}");
        chunk.transform.position = (Vector3)position * ChunkSize;
        chunk.layer = 6;
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
        allChunks.Add(position, chunk);
        activeChunks[position] = chunk;
        return chunk;
    }
}
