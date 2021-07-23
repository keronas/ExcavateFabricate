using System;
using UnityEngine;

public class ChunkSettingsScript : MonoBehaviour
{
    public Mesh BlockMesh;
    public Material BlockMaterial;
    public Color32[] BlockColors;
    public uint[] BlockDestroyDurationsMillis;
    public uint ChunkSize;
    public double PerlinWeight;
    public double HeightWeight;
    public double GroundLevel;
    public uint LayerHeight;
    public bool OptimizeBlocks;
}
