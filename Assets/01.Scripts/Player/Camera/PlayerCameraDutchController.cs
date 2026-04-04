using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class PlayerCameraDutchController : MonoBehaviour
{
    [Header("Default Settings")]
    [SerializeField] private CameraDutchSettingSO _defaultSetting;

    [Header("Charge Settings")]
    [SerializeField] private CameraDutchSettingSO _chargeSetting;

    [Header("Charge Oscillation")]
    [SerializeField] private float _chargeOscillationSpeed = 10f;
    [SerializeField] private float _chargeEndAmplitudeMultiplier = 0.15f;

    private CinemachineCamera _cinemachineCamera;
    private CameraDutchSettingSO _currentSetting;

    private bool _isChargeActive = false;
    private float _chargeNormalized = 0f;

    private float _currentDutch = 0f;

    private void Awake()
    {
        _cinemachineCamera = GetComponent<CinemachineCamera>();
        ApplyImmediateSetting(_defaultSetting);
    }

    private void OnEnable()
    {
        EventBus<OnCameraDutchSignalEvent>.Subscribe(HandleDutchSignal);
    }

    private void OnDisable()
    {
        EventBus<OnCameraDutchSignalEvent>.Unsubscribe(HandleDutchSignal);
    }
    
    private void Update()
    {
        if (_cinemachineCamera == null || _currentSetting == null) return;

        UpdateDutchByPriority();
        ApplyRuntimeBlend();
    }

    private void HandleDutchSignal(OnCameraDutchSignalEvent evt)
    {
        switch (evt.channel)
        {
            case CameraDutchChannel.DashCharge:
                _isChargeActive = evt.isActive;
                _chargeNormalized = evt.normalized;
                break;
        }
    }

    private void UpdateDutchByPriority()
    {
        if(_isChargeActive)
        {
            SetActiveSetting(_chargeSetting);
            return;
        }

        SetActiveSetting(_defaultSetting);
    }

    private void SetActiveSetting(CameraDutchSettingSO setting)
    {
        if (setting == null || _currentSetting == setting)
            return;

        _currentSetting = setting;

        Debug.LogFormat("_currentSetting = {0}", _currentSetting);
    }

    private void ApplyImmediateSetting(CameraDutchSettingSO setting)
    {
        if (_cinemachineCamera == null || setting == null)
            return;

        _currentSetting = setting;
        _currentDutch = setting.TargetDutch;
        _cinemachineCamera.Lens.Dutch = setting.TargetDutch;
    }

    private void ApplyRuntimeBlend()
    {
        float targetDutch = 0f;
        float blendSpeed = _currentSetting.BlendOutSpeed;


        if (_isChargeActive && _currentSetting == _chargeSetting)
        {
            // 시작할 때 가장 강하고, 차지될수록 점점 약해짐
            float amplitude = Mathf.Lerp(
                _currentSetting.TargetDutch,
                _currentSetting.TargetDutch * _chargeEndAmplitudeMultiplier,
                _chargeNormalized);

            // 좌우로 흔들리도록 sin 사용
            targetDutch = Mathf.Sin(Time.time * _chargeOscillationSpeed) * amplitude;

            blendSpeed = _currentSetting.BlendInSpeed;
        }
        else
        {
            targetDutch = _defaultSetting != null ? _defaultSetting.TargetDutch : 0f;
            Debug.LogFormat("{0}", _defaultSetting == null);
            blendSpeed = _currentSetting.BlendInSpeed;
        }

        _currentDutch = Mathf.Lerp(_currentDutch, targetDutch, blendSpeed * Time.deltaTime);
        _cinemachineCamera.Lens.Dutch = _currentDutch;
    }
}
