using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class SavingScript : MonoBehaviour
{
    public string FilePath;
    public WorldGeneratorScript worldGenerator;
    public PlayerScript player;

    async void Update()
    {
        if (Input.GetButtonDown("QuickSave"))
        {
            await SaveGame();
        }
        else if (Input.GetButtonDown("QuickLoad"))
        {
            await LoadGame();
        }
    }

    private async Task SaveGame()
    {
        var arrays = new List<byte[]>();
        arrays.Add(Vector3ToByteArray(player.transform.position)); // 12 bytes position
        arrays.Add(BitConverter.GetBytes(player.CameraRotationX)); // 4 bytes X rotation
        arrays.Add(BitConverter.GetBytes(player.transform.rotation.eulerAngles.y)); // 4 bytes Y rotation

        var chunks = worldGenerator.AllChunkScripts;
        var data = await Task.Run(() =>
        {
            foreach (ChunkScript chunk in chunks)
            {
                arrays.Add(Vector3IntToByteArray(chunk.Position));
                arrays.Add(chunk.Data.SelectMany(array => array).SelectMany(array => array).ToArray());
            }
            return arrays.SelectMany(arr => arr).ToArray();
        });

        var file = File.Create(FilePath);
        await file.WriteAsync(data, 0, data.Length);
        await file.FlushAsync();
        file.Close();
    }

    private async Task LoadGame()
    {
        if (!File.Exists(FilePath))
            return;

        var file = File.OpenRead(FilePath);
        var data = new byte[file.Length];
        await file.ReadAsync(data, 0, (int)file.Length); // cache data
        var reader = new BinaryReader(new MemoryStream(data));

        var playerPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        var xRotation = reader.ReadSingle();
        var yRotation = reader.ReadSingle();
        player.LoadTransform(playerPosition, xRotation, yRotation);

        var chunks = new List<(Vector3Int, byte[][][])>();
        await Task.Run(() =>
        {
            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                var position = new Vector3Int(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                var chunkSize = worldGenerator.ChunkSettings.ChunkSize;

                var chunkData = new byte[chunkSize][][];
                for (var x = 0; x < chunkSize; x++)
                {
                    chunkData[x] = new byte[chunkSize][];
                    for (var y = 0; y < chunkSize; y++)
                    {
                        chunkData[x][y] = reader.ReadBytes((int)chunkSize);
                    }
                }
                chunks.Add((position, chunkData));
            }
        });
        
        worldGenerator.LoadChunks(chunks);
    }

    // modified from https://answers.unity.com/questions/683693/converting-vector3-to-byte.html for lazyness reasons
    private byte[] Vector3IntToByteArray(Vector3Int vector)
    {
        byte[] buff = new byte[sizeof(int) * 3];
        Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, buff, 0 * sizeof(int), sizeof(int));
        Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, buff, 1 * sizeof(int), sizeof(int));
        Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, buff, 2 * sizeof(int), sizeof(int));
        return buff;
    }

    private byte[] Vector3ToByteArray(Vector3 vector)
    {
        byte[] buff = new byte[sizeof(float) * 3];
        Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, buff, 0 * sizeof(float), sizeof(float));
        Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, buff, 1 * sizeof(float), sizeof(float));
        Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, buff, 2 * sizeof(float), sizeof(float));
        return buff;
    }

    private Vector3 ByteArrayToVector3(byte[] array)
    {
        Vector3 vector = new Vector3();
        vector.x = BitConverter.ToSingle(array, 0 * sizeof(float));
        vector.y = BitConverter.ToSingle(array, 1 * sizeof(float));
        vector.z = BitConverter.ToSingle(array, 2 * sizeof(float));
        return vector;
    }
}
