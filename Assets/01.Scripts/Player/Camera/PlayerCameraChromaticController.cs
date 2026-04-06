using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerCameraChromaticController : MonoBehaviour
{
    [SerializeField] private Volume _volume;

    [SerializeField] private CameraChromaticSettingSO _defaultSetting;
    [SerializeField] private CameraChromaticSettingSO _dashSetting;

    private ChromaticAberration _chromatic;

    private bool _isDash;
    private float _normalized;

    private float _currentIntensity;

    private void Awake()
    {
        _volume.profile.TryGet(out _chromatic);
    }

    private void OnEnable()
    {
        EventBus<OnCameraChromaticSignalEvent>.Subscribe(Handle);
    }

    private void OnDisable()
    {
        EventBus<OnCameraChromaticSignalEvent>.Unsubscribe(Handle);
    }

    private void Handle(OnCameraChromaticSignalEvent e)
    {
        _isDash = e.isActive;
        _normalized = e.normalized;
    }

    private void Update()
    {
        float target = _isDash
            ? _dashSetting.Intensity * _normalized
            : _defaultSetting.Intensity;

        float speed = _isDash
            ? _dashSetting.BlendInSpeed
            : _dashSetting.BlendOutSpeed;

        _currentIntensity = Mathf.Lerp(_currentIntensity, target, speed * Time.deltaTime);

        _chromatic.intensity.value = _currentIntensity;
    }
}