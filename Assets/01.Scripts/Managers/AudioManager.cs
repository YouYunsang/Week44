using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoSingleton<AudioManager>
{
    [Header("Sources")]
    [SerializeField] AudioAssetManager audioAssetManager;
    [SerializeField] Transform bgmRoot;
    [SerializeField] Transform sfxRoot;
    [SerializeField] List<AudioSource> bgmSources = new List<AudioSource>();
    [SerializeField] List<AudioSource> sfxSources = new List<AudioSource>();

    [Header("BGM")]
    [SerializeField] AudioBgmKey gameStartBgmKey = AudioBgmKey.None;
    [SerializeField] AudioBgmKey gameOverBgmKey = AudioBgmKey.None;

    [Header("SFX")]
    [SerializeField] AudioSfxKey sliceSfxKey = AudioSfxKey.None;
    [SerializeField] AudioSfxKey gameOverSfxKey = AudioSfxKey.None;
    [SerializeField] bool sfxAffectedByTimeScale = true;

    [Header("Playback Tuning")]
    [SerializeField] bool preloadAudioDataOnAwake = true;
    [SerializeField] bool autoCreateBgmSources = true;
    [Min(0f)]
    [SerializeField] float bgmLayerStartDelay = 0f;
    [Min(0f)]
    [SerializeField] float bgmFadeDuration = 0.25f;
    [Range(0f, 1f)]
    [SerializeField] float pauseBgmVolumeMult  = 0.3f;
    [Range(0f, 1f)]
    [SerializeField] float slowBgmVolumeMult   = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] float slowBgmPitch        = 0.7f;
    [Range(10f, 22000f)]
    [SerializeField] float slowLowPassCutoff    = 800f;
    [Range(1f, 10f)]
    [SerializeField] float slowLowPassResonance = 1f;
    [Min(0f)]
    [SerializeField] float slowLowPassLerpDuration = 0.3f;

    float _bgmTargetVolume = 1f;
    float _pauseVolumeMult = 1f;
    float _slowVolumeMult  = 1f;

    float ActualBgmVolume => _bgmTargetVolume * _pauseVolumeMult * _slowVolumeMult;
    Coroutine _bgmTransitionRoutine;
    readonly List<AudioSource> _activeBgmSources = new List<AudioSource>();
    readonly List<AudioSource> _incomingBgmSourcesBuffer = new List<AudioSource>();

    protected override void Awake()
    {
        base.Awake();

        if (audioAssetManager == null)
            audioAssetManager = AudioAssetManager.Instance;

        if (preloadAudioDataOnAwake && audioAssetManager != null)
            audioAssetManager.PreloadAudioData();

        RefreshSourceLists();
    }

    void OnValidate()
    {
        RefreshSourceLists();
    }

    void OnEnable()
    {
        EventBus<OnGameStartEvent>.Subscribe(OnGameStart);
        EventBus<OnGameOverEvent>.Subscribe(OnGameOver);
        EventBus<OnSliceEvent>.Subscribe(OnSlice);
        EventBus<OnTimeScaleChangedEvent>.Subscribe(OnTimeScaleChanged);
        EventBus<OnSettingsChangedEvent>.Subscribe(OnSettingsChanged);
        EventBus<OnMenuOpenEvent>.Subscribe(OnMenuOpen);
        EventBus<OnMenuCloseEvent>.Subscribe(OnMenuClose);
        EventBus<OnSlowGaugeChangedEvent>.Subscribe(OnSlowGaugeChanged);
    }

    void OnDisable()
    {
        EventBus<OnGameStartEvent>.Unsubscribe(OnGameStart);
        EventBus<OnGameOverEvent>.Unsubscribe(OnGameOver);
        EventBus<OnSliceEvent>.Unsubscribe(OnSlice);
        EventBus<OnTimeScaleChangedEvent>.Unsubscribe(OnTimeScaleChanged);
        EventBus<OnSettingsChangedEvent>.Unsubscribe(OnSettingsChanged);
        EventBus<OnMenuOpenEvent>.Unsubscribe(OnMenuOpen);
        EventBus<OnMenuCloseEvent>.Unsubscribe(OnMenuClose);
        EventBus<OnSlowGaugeChangedEvent>.Unsubscribe(OnSlowGaugeChanged);
    }

    void OnGameStart(OnGameStartEvent e)
    {
        PlayBGM(gameStartBgmKey);
    }

    void OnGameOver(OnGameOverEvent e)
    {
        PlayBGM(gameOverBgmKey);
        PlaySFX(gameOverSfxKey);
    }

    void OnSlice(OnSliceEvent e)
    {
        PlaySFX(sliceSfxKey);
    }

    void OnTimeScaleChanged(OnTimeScaleChangedEvent e)
    {
        // 완전 정지(pause)일 때는 pitch를 건드리지 않음 — 볼륨 덕으로만 처리
        if (e.timeScale <= 0f) return;

        // 슬로우(< 1) 구간: timeScale 0→1에 맞춰 slowBgmPitch→1 사이를 매핑
        // 일반/패스트(>= 1) 구간: timeScale 그대로
        float pitch = e.timeScale < 1f
            ? Mathf.Lerp(slowBgmPitch, 1f, e.timeScale)
            : e.timeScale;

        for (int i = 0; i < bgmSources.Count; i++)
        {
            AudioSource source = bgmSources[i];
            if (source == null) continue;
            source.pitch = pitch;
        }

        if (sfxAffectedByTimeScale)
        {
            for (int i = 0; i < sfxSources.Count; i++)
            {
                AudioSource source = sfxSources[i];
                if (source == null) continue;
                source.pitch = pitch;
            }
        }
    }

    void OnMenuOpen(OnMenuOpenEvent e)
    {
        _pauseVolumeMult = pauseBgmVolumeMult;
        ApplyBgmVolume();
    }

    void OnMenuClose(OnMenuCloseEvent e)
    {
        _pauseVolumeMult = 1f;
        ApplyBgmVolume();
    }

    Coroutine _lowPassRoutine;

    void OnSlowGaugeChanged(OnSlowGaugeChangedEvent e)
    {
        _slowVolumeMult = e.isSlowing ? slowBgmVolumeMult : 1f;
        ApplyBgmVolume();

        float targetCutoff = e.isSlowing ? slowLowPassCutoff : 22000f;
        if (_lowPassRoutine != null) StopCoroutine(_lowPassRoutine);
        _lowPassRoutine = StartCoroutine(LerpLowPassFilter(targetCutoff));
    }

    System.Collections.IEnumerator LerpLowPassFilter(float targetCutoff)
    {
        var filters = GetOrAddLowPassFilters();

        // 시작 컷오프: 첫 번째 필터 기준
        float startCutoff = filters.Count > 0 ? filters[0].cutoffFrequency : 22000f;

        foreach (var f in filters)
        {
            f.lowpassResonanceQ = slowLowPassResonance;
            f.enabled = true;
        }

        float elapsed = 0f;
        while (elapsed < slowLowPassLerpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float cutoff = Mathf.Lerp(startCutoff, targetCutoff, elapsed / slowLowPassLerpDuration);
            foreach (var f in filters)
                f.cutoffFrequency = cutoff;
            yield return null;
        }

        foreach (var f in filters)
        {
            f.cutoffFrequency = targetCutoff;
            f.enabled = targetCutoff < 22000f;
        }

        _lowPassRoutine = null;
    }

    System.Collections.Generic.List<AudioLowPassFilter> GetOrAddLowPassFilters()
    {
        var result = new System.Collections.Generic.List<AudioLowPassFilter>();
        for (int i = 0; i < bgmSources.Count; i++)
        {
            AudioSource source = bgmSources[i];
            if (source == null) continue;

            AudioLowPassFilter filter = source.GetComponent<AudioLowPassFilter>();
            if (filter == null)
                filter = source.gameObject.AddComponent<AudioLowPassFilter>();

            result.Add(filter);
        }
        return result;
    }

    void OnSettingsChanged(OnSettingsChangedEvent e)
    {
        SetBGMVolume(e.data.bgmVolume);
        SetSFXVolume(e.data.sfxVolume);
    }

    public bool PlayBGM(AudioBgmKey key)
    {
        return PlayBGMLayers(key);
    }

    public bool PlayBGM(string nameKey)
    {
        if (!AudioKeyLookup.TryGetBgmKey(nameKey, out AudioBgmKey key)) return false;
        return PlayBGM(key);
    }

    public bool PlayBGMLayers(params AudioBgmKey[] keys)
    {
        if (keys == null || keys.Length == 0) return false;
        if (!TryGetAssetManager(out var assets)) return false;

        var targetClips = new List<AudioClip>();
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] == AudioBgmKey.None) continue;
            if (!assets.TryGetBgmClip(keys[i], out AudioClip clip)) continue;
            targetClips.Add(clip);
        }

        if (targetClips.Count == 0) return false;

        if (_bgmTransitionRoutine != null)
            StopCoroutine(_bgmTransitionRoutine);

        _bgmTransitionRoutine = StartCoroutine(TransitionToBgmClips(targetClips));
        return true;
    }

    public void StopBGMLayers()
    {
        if (_bgmTransitionRoutine != null)
            StopCoroutine(_bgmTransitionRoutine);

        for (int i = 0; i < bgmSources.Count; i++)
        {
            AudioSource source = bgmSources[i];
            if (source == null) continue;
            source.Stop();
            source.clip = null;
        }

        _activeBgmSources.Clear();
    }

    public bool PlaySFX(AudioSfxKey key)
    {
        if (key == AudioSfxKey.None) return false;
        if (!TryGetAssetManager(out var assets)) return false;
        if (!assets.TryGetSfxClip(key, out AudioClip clip)) return false;
        if (!TryGetPrimarySfxSource(out AudioSource sfxSource)) return false;

        sfxSource.PlayOneShot(clip);
        return true;
    }

    public bool PlaySFX(string nameKey)
    {
        if (!AudioKeyLookup.TryGetSfxKey(nameKey, out AudioSfxKey key)) return false;
        return PlaySFX(key);
    }

    public void SetBGMVolume(float volume)
    {
        _bgmTargetVolume = volume;
        ApplyBgmVolume();
    }

    void ApplyBgmVolume()
    {
        for (int i = 0; i < bgmSources.Count; i++)
        {
            AudioSource source = bgmSources[i];
            if (source == null) continue;
            source.volume = ActualBgmVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        for (int i = 0; i < sfxSources.Count; i++)
        {
            AudioSource source = sfxSources[i];
            if (source == null) continue;
            source.volume = volume;
        }
    }

    IEnumerator TransitionToBgmClips(List<AudioClip> targetClips)
    {
        RefreshSourceLists();

        var outgoing = new List<AudioSource>();
        for (int i = 0; i < _activeBgmSources.Count; i++)
        {
            AudioSource source = _activeBgmSources[i];
            if (source == null || source.clip == null) continue;
            outgoing.Add(source);
        }

        int requiredIncoming = targetClips.Count;
        EnsureBgmSourceCount(outgoing.Count + requiredIncoming);
        RefreshSourceLists();

        if (!TryCollectDistinctIncomingSources(requiredIncoming, outgoing, _incomingBgmSourcesBuffer))
        {
            yield return StartCoroutine(SequentialFadeSwap(targetClips));
            yield break;
        }

        double startTime = AudioSettings.dspTime + bgmLayerStartDelay;
        for (int i = 0; i < _incomingBgmSourcesBuffer.Count; i++)
        {
            AudioSource source = _incomingBgmSourcesBuffer[i];
            source.Stop();
            source.clip = targetClips[i];
            source.loop = true;
            source.volume = 0f;
            source.PlayScheduled(startTime);
        }

        if (bgmFadeDuration > 0f)
        {
            float[] outgoingStart = new float[outgoing.Count];
            for (int i = 0; i < outgoing.Count; i++)
                outgoingStart[i] = outgoing[i].volume;

            float t = 0f;
            while (t < bgmFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float ratio = t / bgmFadeDuration;

                for (int i = 0; i < outgoing.Count; i++)
                    outgoing[i].volume = Mathf.Lerp(outgoingStart[i], 0f, ratio);

                for (int i = 0; i < _incomingBgmSourcesBuffer.Count; i++)
                    _incomingBgmSourcesBuffer[i].volume = Mathf.Lerp(0f, ActualBgmVolume, ratio);

                yield return null;
            }
        }

        for (int i = 0; i < outgoing.Count; i++)
        {
            AudioSource source = outgoing[i];
            source.Stop();
            source.clip = null;
            source.volume = ActualBgmVolume;
        }

        _activeBgmSources.Clear();
        for (int i = 0; i < _incomingBgmSourcesBuffer.Count; i++)
        {
            AudioSource source = _incomingBgmSourcesBuffer[i];
            source.volume = ActualBgmVolume;
            _activeBgmSources.Add(source);
        }

        _incomingBgmSourcesBuffer.Clear();
    }

    IEnumerator SequentialFadeSwap(List<AudioClip> targetClips)
    {
        if (bgmFadeDuration > 0f)
        {
            float[] startVolumes = new float[bgmSources.Count];
            for (int i = 0; i < bgmSources.Count; i++)
                startVolumes[i] = bgmSources[i] != null ? bgmSources[i].volume : 0f;

            float t = 0f;
            while (t < bgmFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float ratio = t / bgmFadeDuration;

                for (int i = 0; i < bgmSources.Count; i++)
                {
                    AudioSource source = bgmSources[i];
                    if (source == null) continue;
                    source.volume = Mathf.Lerp(startVolumes[i], 0f, ratio);
                }

                yield return null;
            }
        }

        double startTime = AudioSettings.dspTime + bgmLayerStartDelay;
        for (int i = 0; i < bgmSources.Count; i++)
        {
            AudioSource source = bgmSources[i];
            if (source == null) continue;

            if (i < targetClips.Count)
            {
                source.Stop();
                source.clip = targetClips[i];
                source.loop = true;
                source.volume = 0f;
                source.PlayScheduled(startTime);
            }
            else
            {
                source.Stop();
                source.clip = null;
                source.volume = 0f;
            }
        }

        if (bgmFadeDuration > 0f)
        {
            float t = 0f;
            while (t < bgmFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float ratio = t / bgmFadeDuration;

                for (int i = 0; i < bgmSources.Count; i++)
                {
                    AudioSource source = bgmSources[i];
                    if (source == null || source.clip == null) continue;
                    source.volume = Mathf.Lerp(0f, ActualBgmVolume, ratio);
                }

                yield return null;
            }
        }

        for (int i = 0; i < bgmSources.Count; i++)
        {
            AudioSource source = bgmSources[i];
            if (source == null || source.clip == null) continue;
            source.volume = ActualBgmVolume;
        }

        _activeBgmSources.Clear();
        for (int i = 0; i < bgmSources.Count && i < targetClips.Count; i++)
        {
            AudioSource source = bgmSources[i];
            if (source == null || source.clip == null) continue;
            _activeBgmSources.Add(source);
        }
    }

    bool TryCollectDistinctIncomingSources(int requiredCount, List<AudioSource> exclude, List<AudioSource> result)
    {
        result.Clear();

        for (int i = 0; i < bgmSources.Count; i++)
        {
            AudioSource source = bgmSources[i];
            if (source == null) continue;
            if (exclude.Contains(source)) continue;

            result.Add(source);
            if (result.Count >= requiredCount)
                return true;
        }

        result.Clear();
        return false;
    }

    void EnsureBgmSourceCount(int requiredCount)
    {
        if (!autoCreateBgmSources) return;

        int currentCount = 0;
        for (int i = 0; i < bgmSources.Count; i++)
        {
            if (bgmSources[i] != null)
                currentCount++;
        }

        int missing = requiredCount - currentCount;
        if (missing <= 0) return;

        Transform parent = bgmRoot != null ? bgmRoot : transform;
        AudioSource template = null;
        for (int i = 0; i < bgmSources.Count; i++)
        {
            if (bgmSources[i] == null) continue;
            template = bgmSources[i];
            break;
        }

        for (int i = 0; i < missing; i++)
        {
            var go = new GameObject($"BGM Auto Source {currentCount + i + 1}");
            go.transform.SetParent(parent, false);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.volume = ActualBgmVolume;

            if (template != null)
            {
                source.spatialBlend = template.spatialBlend;
                source.outputAudioMixerGroup = template.outputAudioMixerGroup;
                source.pitch = template.pitch;
            }

            bgmSources.Add(source);
        }
    }

    void RefreshSourceLists()
    {
        if (bgmRoot != null) bgmSources = CollectAudioSourcesFromRoot(bgmRoot);
        if (sfxRoot != null) sfxSources = CollectAudioSourcesFromRoot(sfxRoot);
    }

    List<AudioSource> CollectAudioSourcesFromRoot(Transform root)
    {
        var list = new List<AudioSource>();
        if (root == null) return list;

        AudioSource[] sources = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null) continue;
            if (list.Contains(source)) continue;
            list.Add(source);
        }

        return list;
    }

    bool TryGetPrimarySfxSource(out AudioSource source)
    {
        for (int i = 0; i < sfxSources.Count; i++)
        {
            if (sfxSources[i] == null) continue;
            source = sfxSources[i];
            return true;
        }

        source = null;
        return false;
    }

    bool TryGetAssetManager(out AudioAssetManager assets)
    {
        assets = audioAssetManager;
        if (assets != null) return true;

        assets = AudioAssetManager.Instance;
        audioAssetManager = assets;
        return assets != null;
    }
}
