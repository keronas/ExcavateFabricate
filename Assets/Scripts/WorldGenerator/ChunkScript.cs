using System.Collections;
using System.Collections.Generic;
using LibNoise.Unity;
using LibNoise.Unity.Generator;
using UnityEngine;

public class ChunkScript : MonoBehaviour
{
    public GameObject BlockPrefab;
    public uint ChunkSize;
    public double PerlinWeight;
    public double HeightWeight;
    public double GroundLevel;
    public bool OptimizeBlocks;
    public Perlin PerlinGenerator;


    private byte[][][] data { get; set; }
    private List<GameObject> objects { get; set; } = new List<GameObject>();

    // Start is called before the first frame update
    void Start()  
    {
        InitializeData();
        InstantiateBlocks();
    }

    private void InitializeData()
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
                    var worldPosition = transform.TransformPoint(localPosition);
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
    }

    private void InstantiateBlocks()
    {
        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                for (var z = 0; z < ChunkSize; z++)
                {
                    if (data[x][y][z] != 0)
                    {
                        // If any neighbouring space is still inside chunk but doesn't contain any block, then current block may be visible
                        if (!OptimizeBlocks ||
                            (x - 1 >= 0 && data[x - 1][y][z] == 0) ||
                            (y - 1 >= 0 && data[x][y - 1][z] == 0) ||
                            (z - 1 >= 0 && data[x][y][z - 1] == 0) ||
                            (x + 1 < ChunkSize && data[x + 1][y][z] == 0) ||
                            (y + 1 < ChunkSize && data[x][y + 1][z] == 0) ||
                            (z + 1 < ChunkSize && data[x][y][z + 1] == 0))
                        {
                            CreateBlock(new Vector3(x, y, z));
                        }
                    }
                    
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void CreateBlock(Vector3 position)
    {
        var positionOffset = new Vector3(0.5f, 0.5f, 0.5f); // to fit nicely into the editor grid
        var block = GameObject.Instantiate(BlockPrefab, transform);
        block.transform.localPosition = position + positionOffset;
        block.name = $"Block {position.x};{position.y};{position.z}";
        objects.Add(block);
    }
}
