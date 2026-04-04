using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class BgmClipEntry
{
    public AudioBgmKey key;
    public AudioClip clip;
}

[System.Serializable]
public class SfxClipEntry
{
    public AudioSfxKey key;
    public AudioClip clip;
}

public class AudioAssetManager : MonoSingleton<AudioAssetManager>
{
    [Header("BGM Library")]
    [SerializeField] List<BgmClipEntry> bgmLibrary = new List<BgmClipEntry>();

    [Header("SFX Library")]
    [SerializeField] List<SfxClipEntry> sfxLibrary = new List<SfxClipEntry>();

    readonly Dictionary<AudioBgmKey, AudioClip> _bgmMap = new Dictionary<AudioBgmKey, AudioClip>();
    readonly Dictionary<AudioSfxKey, AudioClip> _sfxMap = new Dictionary<AudioSfxKey, AudioClip>();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        BuildBGMMap();
        BuildSFXMap();
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        AutoSyncLibrariesFromAssets();
#endif
        BuildBGMMap();
        BuildSFXMap();
    }

    public bool TryGetBgmClip(AudioBgmKey key, out AudioClip clip)
    {
        clip = null;
        if (key == AudioBgmKey.None) return false;
        return _bgmMap.TryGetValue(key, out clip);
    }

    public bool TryGetSfxClip(AudioSfxKey key, out AudioClip clip)
    {
        clip = null;
        if (key == AudioSfxKey.None) return false;
        return _sfxMap.TryGetValue(key, out clip);
    }

    public void PreloadAudioData()
    {
        foreach (var pair in _bgmMap)
            TryPreload(pair.Value);

        foreach (var pair in _sfxMap)
            TryPreload(pair.Value);
    }

    void TryPreload(AudioClip clip)
    {
        if (clip == null) return;
        if (clip.preloadAudioData) return;

        clip.LoadAudioData();
    }

    void BuildBGMMap()
    {
        _bgmMap.Clear();

        for (int i = 0; i < bgmLibrary.Count; i++)
        {
            BgmClipEntry entry = bgmLibrary[i];
            if (entry == null) continue;
            if (entry.key == AudioBgmKey.None) continue;
            if (entry.clip == null) continue;

            _bgmMap[entry.key] = entry.clip;
        }
    }

    void BuildSFXMap()
    {
        _sfxMap.Clear();

        for (int i = 0; i < sfxLibrary.Count; i++)
        {
            SfxClipEntry entry = sfxLibrary[i];
            if (entry == null) continue;
            if (entry.key == AudioSfxKey.None) continue;
            if (entry.clip == null) continue;

            _sfxMap[entry.key] = entry.clip;
        }
    }

#if UNITY_EDITOR
    void AutoSyncLibrariesFromAssets()
    {
        SyncBgmLibrary();
        SyncSfxLibrary();
    }

    void SyncBgmLibrary()
    {
        var list = new List<BgmClipEntry>();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/05.Audio/BGM" });

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (!AudioKeyLookup.TryGetBgmKey(fileName, out AudioBgmKey key)) continue;

            AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;

            list.Add(new BgmClipEntry { key = key, clip = clip });
        }

        bgmLibrary = list.OrderBy(e => e.key).ToList();
    }

    void SyncSfxLibrary()
    {
        var list = new List<SfxClipEntry>();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/05.Audio/SFX" });

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (!AudioKeyLookup.TryGetSfxKey(fileName, out AudioSfxKey key)) continue;

            AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;

            list.Add(new SfxClipEntry { key = key, clip = clip });
        }

        sfxLibrary = list.OrderBy(e => e.key).ToList();
    }
#endif
}
