using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkScript : MonoBehaviour
{
    public GameObject BlockPrefab;
    public uint ChunkSize;
    public Vector2 Position;


    private byte[][][] data { get; set; }
    private List<GameObject> objects { get; set; } = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                for (var z = 0; z < ChunkSize; z++)
                {
                    if (Random.value > 0.5)
                        CreateBlock(new Vector3(x, y, z));
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void CreateBlock(Vector3 position)
    {
        var positionOffset = new Vector3(0.5f, 0.5f, 0.5f); // to fit nicely into the editor grid
        var block = GameObject.Instantiate(BlockPrefab, position + positionOffset, new Quaternion(), transform);
        block.name = $"Block {position.x};{position.y};{position.z}";
        objects.Add(block);
    }
}
