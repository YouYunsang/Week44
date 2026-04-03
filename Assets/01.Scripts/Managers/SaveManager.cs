using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public string sceneName;
    public float  posX;
    public float  posY;
    public float  posZ;
}

public class SaveManager : MonoSingleton<SaveManager>
{
    public SaveData Data { get; private set; }

    static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    protected override void Awake()
    {
        base.Awake();
        Load();
    }

    public void SaveCheckpoint(Vector3 position, string sceneName)
    {
        Data.sceneName = sceneName;
        Data.posX = position.x;
        Data.posY = position.y;
        Data.posZ = position.z;

        string json = JsonUtility.ToJson(Data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
    }

    public Vector3 GetRespawnPosition()
    {
        return new Vector3(Data.posX, Data.posY, Data.posZ);
    }

    public bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public void Load()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            Data = JsonUtility.FromJson<SaveData>(json);
        }
        else
        {
            Data = new SaveData();
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);

        Data = new SaveData();
    }
}
