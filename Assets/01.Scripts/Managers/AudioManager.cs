using UnityEngine;

public class AudioManager : MonoSingleton<AudioManager>
{
    [Header("Sources")]
    [SerializeField] AudioSource bgmSource;
    [SerializeField] AudioSource sfxSource;

    [Header("BGM")]
    [SerializeField] AudioClip bgmGame;
    [SerializeField] AudioClip bgmGameOver;

    [Header("SFX")]
    [SerializeField] AudioClip sfxSlice;
    [SerializeField] AudioClip sfxGameOver;
    [SerializeField] bool sfxAffectedByTimeScale = true;

    void OnEnable()
    {
        EventBus<OnGameStartEvent>.Subscribe(OnGameStart);
        EventBus<OnGameOverEvent>.Subscribe(OnGameOver);
        EventBus<OnSliceEvent>.Subscribe(OnSlice);
        EventBus<OnTimeScaleChangedEvent>.Subscribe(OnTimeScaleChanged);
        EventBus<OnSettingsChangedEvent>.Subscribe(OnSettingsChanged);
    }

    void OnDisable()
    {
        EventBus<OnGameStartEvent>.Unsubscribe(OnGameStart);
        EventBus<OnGameOverEvent>.Unsubscribe(OnGameOver);
        EventBus<OnSliceEvent>.Unsubscribe(OnSlice);
        EventBus<OnTimeScaleChangedEvent>.Unsubscribe(OnTimeScaleChanged);
        EventBus<OnSettingsChangedEvent>.Unsubscribe(OnSettingsChanged);
    }

    void OnGameStart(OnGameStartEvent e)
    {
        PlayBGM(bgmGame);
    }

    void OnGameOver(OnGameOverEvent e)
    {
        PlayBGM(bgmGameOver);
        PlaySFX(sfxGameOver);
    }

    void OnSlice(OnSliceEvent e)
    {
        PlaySFX(sfxSlice);
    }

    void OnTimeScaleChanged(OnTimeScaleChangedEvent e)
    {
        float pitch = e.timeScale > 0f ? e.timeScale : 0f;
        bgmSource.pitch = pitch;
        if (sfxAffectedByTimeScale)
            sfxSource.pitch = pitch;
    }

    void OnSettingsChanged(OnSettingsChangedEvent e)
    {
        SetBGMVolume(e.data.bgmVolume);
        SetSFXVolume(e.data.sfxVolume);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }
}
