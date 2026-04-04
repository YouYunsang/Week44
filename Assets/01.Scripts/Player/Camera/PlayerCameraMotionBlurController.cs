using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerCameraMotionBlurController : MonoBehaviour
{
    [SerializeField] private Volume _volume;

    [SerializeField] private CameraMotionBlurSettingSO _defaultSetting;
    [SerializeField] private CameraMotionBlurSettingSO _dashSetting;

    private MotionBlur _motionBlur;

    private bool _isDash;
    private float _normalized;

    private float _currentIntensity;

    private void Awake()
    {
        _volume.profile.TryGet(out _motionBlur);
    }

    private void OnEnable()
    {
        EventBus<OnCameraMotionBlurSignalEvent>.Subscribe(Handle);
    }

    private void OnDisable()
    {
        EventBus<OnCameraMotionBlurSignalEvent>.Unsubscribe(Handle);
    }

    private void Handle(OnCameraMotionBlurSignalEvent e)
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

        _motionBlur.intensity.value = _currentIntensity;
    }
}