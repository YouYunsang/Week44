using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class PlayerCameraNoiseController : MonoBehaviour
{
    [Header("Noise Settings")]
    [SerializeField] private CameraNoiseSettingSO _defaultSetting;

    [Header("Move Settings")]
    [SerializeField] private CameraNoiseSettingSO _moveSetting;

    [Header("Boss Stomp Settings")]
    [SerializeField] private CameraNoiseSettingSO _bossStompSetting;

    private CinemachineBasicMultiChannelPerlin _perlin;
    private CameraNoiseSettingSO _currentSetting;

    private float _currentAmplitude = 0f;
    private float _currentFrequency = 1f;

    private bool _isMoveActive = false;
    private float _moveNormalized = 0f;

    private bool _isBossStompActive = false;
    private float _bossStompNormalized = 0f;

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

            case CameraNoiseChannel.BossStomp:
                _isBossStompActive = evt.isActive;
                _bossStompNormalized = evt.normalized;
                break;
        }
    }

    private void UpdateNoiseByPriority()
    {
        if (_isBossStompActive)
        {
            SetActiveSetting(_bossStompSetting);
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

        if (_isBossStompActive && _currentSetting == _bossStompSetting)
        {
            normalized = _bossStompNormalized;
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
            if (_currentSetting == _bossStompSetting)
            {
                targetAmplitude *= normalized;
                targetFrequency *= Mathf.Lerp(0.9f, 1f, normalized);
            }
            else if (_currentSetting == _moveSetting)
            {
                targetAmplitude *= Mathf.Lerp(0.35f, 1f, normalized);
                targetFrequency *= Mathf.Lerp(0.85f, 1.15f, normalized);
            }
        }

        float blendSpeed = isBlendingIn ? _currentSetting.BlendInSpeed : _currentSetting.BlendOutSpeed;

        _currentAmplitude = Mathf.Lerp(_currentAmplitude, targetAmplitude, blendSpeed * Time.deltaTime);
        _currentFrequency = Mathf.Lerp(_currentFrequency, targetFrequency, blendSpeed * Time.deltaTime);

        _perlin.AmplitudeGain = _currentAmplitude;
        _perlin.FrequencyGain = _currentFrequency;
    }
}
