using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class PlayerCameraNoiseController : MonoBehaviour
{
    [Header("Noise Settings")]
    [SerializeField] private CameraNoiseSettingSO _defaultSetting;

    [Header("Move Settings")]
    [SerializeField] private CameraNoiseSettingSO _moveSetting;

    [Header("Charge Settings")]
    [SerializeField] private CameraNoiseSettingSO _chargeSetting;

    private CinemachineBasicMultiChannelPerlin _perlin;
    private CameraNoiseSettingSO _currentSetting;

    private float _currentAmplitude = 0f;
    private float _currentFrequency = 1f;

    private bool _isMoveActive = false;
    private float _moveNormalized = 0f;

    private bool _isChargeActive = false;
    private float _chargeNormalized = 0f;

    private void Awake()
    {
        _perlin = GetComponent<CinemachineBasicMultiChannelPerlin>();

        ApplyImmediateSetting(_defaultSetting);
    }

    private void OnEnable()
    {
        EventBus<OnCameraNoiseSignalEvent>.Subscribe(HandleNoiseSignal);
    }

    private void OnDisable()
    {
        EventBus<OnCameraNoiseSignalEvent>.Unsubscribe(HandleNoiseSignal);
    }

    private void Update()
    {
        if (_perlin == null || _currentSetting == null) return;

        UpdateNoiseByPriority();
        ApplyRuntimeBlend();
    }

    private void HandleNoiseSignal(OnCameraNoiseSignalEvent evt)
    {
        switch (evt.channel)
        {
            case CameraNoiseChannel.Move:
                _isMoveActive = evt.isActive;
                _moveNormalized = evt.normalized;
                break;

            case CameraNoiseChannel.DashCharge:
                _isChargeActive = evt.isActive;
                _chargeNormalized = evt.normalized;
                break;
        }
    }

    private void UpdateNoiseByPriority()
    {
        // 우선순위: Charge > Move > Default
        if (_isChargeActive)
        {
            SetActiveSetting(_chargeSetting);
            return;
        }

        if (_isMoveActive)
        {
            SetActiveSetting(_moveSetting);
            return;
        }

        SetActiveSetting(_defaultSetting);
    }

    private void SetActiveSetting(CameraNoiseSettingSO setting)
    {
        if(setting == null || _currentSetting == setting) return;

        _currentSetting = setting;
        
        _perlin.NoiseProfile = _currentSetting.NoiseProfile;
        _perlin.PivotOffset = _currentSetting.PivotOffset;
    }

    private void ApplyImmediateSetting(CameraNoiseSettingSO setting)
    {
        if (_perlin == null || setting == null) return;

        _currentSetting = setting;

        _perlin.NoiseProfile = setting.NoiseProfile;
        _perlin.PivotOffset = setting.PivotOffset;
        _perlin.AmplitudeGain = setting.AmplitudeGain;
        _perlin.FrequencyGain = setting.FrequencyGain;

        _currentAmplitude = setting.AmplitudeGain;
        _currentFrequency = setting.FrequencyGain;
    }

    private void ApplyRuntimeBlend()
    {
        float normalized = 1f;
        bool isBlendingIn = false;

        if (_isChargeActive && _currentSetting == _chargeSetting)
        {
            normalized = _chargeNormalized;
            isBlendingIn = true;
        }
        else if (_isMoveActive && _currentSetting == _moveSetting)
        {
            normalized = _moveNormalized;
            isBlendingIn = true;
        }

        float targetAmplitude = _currentSetting.AmplitudeGain;
        float targetFrequency = _currentSetting.FrequencyGain;

        // 활성 상태에서는 normalized 기반 스케일링
        if (isBlendingIn)
        {
            targetAmplitude *= Mathf.Lerp(0.35f, 1f, normalized);
            targetFrequency *= Mathf.Lerp(0.85f, 1.15f, normalized);
        }

        float blendSpeed = isBlendingIn ? _currentSetting.BlendInSpeed : _currentSetting.BlendOutSpeed;

        _currentAmplitude = Mathf.Lerp(_currentAmplitude, targetAmplitude, blendSpeed * Time.deltaTime);
        _currentFrequency = Mathf.Lerp(_currentFrequency, targetFrequency, blendSpeed * Time.deltaTime);

        _perlin.AmplitudeGain = _currentAmplitude;
        _perlin.FrequencyGain = _currentFrequency;
    }
}
