using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibNoise.Unity.Generator;
using UnityEngine;

public class WorldGeneratorScript : MonoBehaviour
{
    public ChunkSettingsScript ChunkSettings;
    public Transform ChunkViewCenter;
    public uint ChunkViewDistance;
    public bool IsDoneCreatingChunks { get; private set; } = false;
    public ChunkScript[] AllChunkScripts => allChunks.Values.Select(ob => ob.GetComponent<ChunkScript>()).ToArray();

    private Dictionary<Vector3Int, GameObject> allChunks = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> activeChunks = new Dictionary<Vector3Int, GameObject>();
    private Queue<Vector3Int> chunksToCreate = new Queue<Vector3Int>();

    private Perlin perlinGenerator = new Perlin();

    public void CreateBlock(Vector3Int worldPosition, byte blockType)
    {
        var chunkPosition = Vector3Int.FloorToInt((Vector3)worldPosition / ChunkSettings.ChunkSize); // explicit floor needed for negative numbers
        var chunk = activeChunks[chunkPosition].GetComponent<ChunkScript>();
        chunk.SetBlock(worldPosition, blockType);
    }

    // Start is called before the first frame update
    void Start()
    {
        perlinGenerator.Seed = new System.Random().Next();
    }

    // Update is called once per frame
    void Update()
    {
        RefreshChunks();
        if (chunksToCreate.Any())
        {
            CreateChunk(chunksToCreate.Dequeue());
        }
        else
        {
            IsDoneCreatingChunks = true;
        }
    }

    private void RefreshChunks()
    {
        var centerPosition = Vector3Int.FloorToInt(ChunkViewCenter.position / ChunkSettings.ChunkSize);
        var newActiveChunks = new Dictionary<Vector3Int, GameObject>();

        for (var z = -(int)ChunkViewDistance; z <= ChunkViewDistance; z++)
        {
            var circleWidthAtZ = (int)Mathf.Sqrt(ChunkViewDistance * ChunkViewDistance - z * z); // pythagorean theorem to get width at given Z
            for (var x = -circleWidthAtZ; x <= circleWidthAtZ; x++)
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
        chunk.transform.position = (Vector3)position * ChunkSettings.ChunkSize;
        chunk.layer = 6;
        var meshRenderer = chunk.AddComponent<MeshRenderer>();
        meshRenderer.material = ChunkSettings.BlockMaterial;
        var chunkScript = chunk.AddComponent<ChunkScript>();
        chunkScript.ChunkSettings = ChunkSettings;
        chunkScript.PerlinGenerator = perlinGenerator;
        allChunks.Add(position, chunk);
        activeChunks[position] = chunk;
        return chunk;
    }
}
