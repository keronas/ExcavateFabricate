using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class WorldGeneratorScript : MonoBehaviour
{
    public GameObject BlockPrefab;
    public uint ChunkSize;


    // Start is called before the first frame update
    void Start()
    {
        var chunk = new GameObject("TestChunk");
        var chunkScript = chunk.AddComponent<ChunkScript>();
        chunkScript.BlockPrefab = BlockPrefab;
        chunkScript.ChunkSize = ChunkSize;
        chunkScript.Position = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
