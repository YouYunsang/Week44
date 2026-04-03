using System.IO;
using UnityEngine;

[System.Serializable]
public class SettingsData
{
    public float bgmVolume       = 1f;
    public float sfxVolume       = 1f;
    public float mouseSensitivity = 0.15f;
    public bool  fullscreen      = true;
}

public class SettingsManager : MonoSingleton<SettingsManager>
{
    public SettingsData Data { get; private set; }

    static string SavePath => Path.Combine(Application.persistentDataPath, "settings.json");

    protected override void Awake()
    {
        base.Awake();
        Load();
        Apply();
    }

    public void SetBGMVolume(float value)
    {
        Data.bgmVolume = Mathf.Clamp01(value);
        Publish();
    }

    public void SetSFXVolume(float value)
    {
        Data.sfxVolume = Mathf.Clamp01(value);
        Publish();
    }

    public void SetMouseSensitivity(float value)
    {
        Data.mouseSensitivity = Mathf.Max(0f, value);
        Publish();
    }

    public void SetFullscreen(bool value)
    {
        Data.fullscreen = value;
        Screen.fullScreen = value;
        Publish();
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(Data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
    }

    void Load()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            Data = JsonUtility.FromJson<SettingsData>(json);
        }
        else
        {
            Data = new SettingsData();
        }
    }

    void Apply()
    {
        Screen.fullScreen = Data.fullscreen;
        Publish();
    }

    void Publish()
    {
        EventBus<OnSettingsChangedEvent>.Publish(new OnSettingsChangedEvent { data = Data });
    }
}
