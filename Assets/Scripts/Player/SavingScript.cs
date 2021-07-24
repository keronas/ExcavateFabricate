using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        arrays.Add(BitConverter.GetBytes(player.transform.rotation.eulerAngles.y)); // 4 bytes Y rotation
        arrays.Add(BitConverter.GetBytes(player.CameraRotationX)); // 4 bytes X rotation

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

    private Vector3Int ByteArrayToVector3Int(byte[] array)
    {
        Vector3Int vector = new Vector3Int();
        vector.x = BitConverter.ToInt32(array, 0 * sizeof(int));
        vector.y = BitConverter.ToInt32(array, 1 * sizeof(int));
        vector.z = BitConverter.ToInt32(array, 2 * sizeof(int));
        return vector;
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
