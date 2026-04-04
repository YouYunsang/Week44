using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class PlayerCameraNoiseController : MonoBehaviour
{
    [Header("Noise Settings")]
    [SerializeField] private CameraNoiseSettingSO _defaultSetting;

    [Header("Charge Settings")]
    [SerializeField] private CameraNoiseSettingSO _chargeSetting;

    [Header("Reference")]
    [SerializeField] private PlayerDashAttack _playerDashAttack;

    private CinemachineBasicMultiChannelPerlin _perlin;
    private CameraNoiseSettingSO _currentSetting;
    private float _currentAmplitude = 0f;
    private float _currentFrequency = 1f;

    private void Awake()
    {
        _perlin = GetComponent<CinemachineBasicMultiChannelPerlin>();

        if (_playerDashAttack == null)
            _playerDashAttack = FindFirstObjectByType<PlayerDashAttack>();

        ApplyImmediateSetting(_defaultSetting);
    }

    private void Update()
    {
        if (_perlin == null || _currentSetting == null) return;

        UpdateNoiseByState();
        ApplyRuntimeBlend();
    }

    private void UpdateNoiseByState()
    {
        if(_playerDashAttack != null && _playerDashAttack.IsCharging)
        {
            SetActiveSetting(_chargeSetting);
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
        float chargeNormalized = 0f;
        bool isCharging = _playerDashAttack != null && _playerDashAttack.IsCharging;

        if (isCharging)
            chargeNormalized = _playerDashAttack.ChargeNormalized;

        float targetAmplitude = _currentSetting.AmplitudeGain;
        float targetFrequency = _currentSetting.FrequencyGain;

        if(isCharging && _currentSetting == _chargeSetting)
        {
            targetAmplitude *= Mathf.Lerp(0.35f, 1f, chargeNormalized);
            targetFrequency *= Mathf.Lerp(0.8f, 1.15f, chargeNormalized);
        }

        float blendSpeed = isCharging ? _currentSetting.BlendInSpeed : _currentSetting.BlendOutSpeed;

        _currentAmplitude = Mathf.Lerp(_currentAmplitude, targetAmplitude, blendSpeed * Time.deltaTime);
        _currentFrequency = Mathf.Lerp(_currentFrequency, targetFrequency, blendSpeed * Time.deltaTime);

        _perlin.AmplitudeGain = _currentAmplitude;
        _perlin.FrequencyGain = _currentFrequency;
    }
}
