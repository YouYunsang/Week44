using UnityEngine;
using Unity.Cinemachine;

public class PlayerCameraFovController : MonoBehaviour
{
    [Header("Default Settings")]
    [SerializeField] private CameraFovSettingSO _defaultSetting;

    [Header("Move Setting")]
    [SerializeField] private CameraFovSettingSO _moveSetting;

    [Header("Charge Setting")]
    [SerializeField] private CameraFovSettingSO _chargeSetting;

    [Header("Dash Setting")]
    [SerializeField] private CameraFovSettingSO _dashSetting;

    private CinemachineCamera _cinemachineCamera;
    private CameraFovSettingSO _currentSetting;

    private bool _isMoveActive = false;
    private float _moveNormalized = 0f;

    private bool _isChargeActive = false;
    private float _chargeNormalized = 0f;

    private float _targetFov;
    private float _currentBlendTime = 0.15f;
    private float _fovVelocity;

    private bool _isDashActive = false;
    private float _dashNormalized = 0f;

    private void Awake()
    {
        _cinemachineCamera = GetComponent<CinemachineCamera>();

        ApplyImmediateSetting(_defaultSetting);
    }

    private void OnEnable()
    {
        EventBus<OnCameraFovSignalEvent>.Subscribe(HandleFovSignal);
    }

    private void OnDisable()
    {
        EventBus<OnCameraFovSignalEvent>.Unsubscribe(HandleFovSignal);
    }

    private void Update()
    {
        if (_cinemachineCamera == null || _currentSetting == null) return;

        UpdateFovByPriority();
        ApplyRuntimeBlend();
    }

    private void HandleFovSignal(OnCameraFovSignalEvent evt)
    {
        switch (evt.channel)
        {
            case CameraFovChannel.Move:
                _isMoveActive = evt.isActive;
                _moveNormalized = evt.normalized;
                break;

            case CameraFovChannel.DashCharge:
                _isChargeActive = evt.isActive;
                _chargeNormalized = evt.normalized;
                break;

            case CameraFovChannel.Dash:
                _isDashActive = evt.isActive;
                _dashNormalized = evt.normalized;
                break;
        }
    }

    private void UpdateFovByPriority()
    {
        // 우선순위: Dash > Charge > Move > Default
        if (_isDashActive)
        {
            SetActiveSetting(_dashSetting, true);
            return;
        }

        if (_isChargeActive)
        {
            SetActiveSetting(_chargeSetting, true);
            return;
        }

        if (_isMoveActive)
        {
            SetActiveSetting(_moveSetting, true);
            return;
        }

        SetActiveSetting(_defaultSetting, false);
    }

    private void SetActiveSetting(CameraFovSettingSO setting, bool isBlendingIn)
    {
        if(setting == null) return;

        if(_currentSetting != setting)
            _currentSetting = setting;

        _currentBlendTime = isBlendingIn ? _currentSetting.BlendInTime : _currentSetting.BlendOutTime;

        _targetFov = EvaluateTargetFov(setting);
    }

    private float EvaluateTargetFov(CameraFovSettingSO setting)
    {
        float normalized = 1f;

        if (_isDashActive && setting == _dashSetting)
            normalized = _dashNormalized;
        else if (_isChargeActive && setting == _chargeSetting)
            normalized = _chargeNormalized;
        else if (_isMoveActive && setting == _moveSetting)
            normalized = _moveNormalized;

        return Mathf.Lerp(_defaultSetting.TargetFov, setting.TargetFov, normalized);
    }

    private void ApplyImmediateSetting(CameraFovSettingSO setting)
    {
        if (_cinemachineCamera == null || setting == null)
            return;

        _currentSetting = setting;
        _targetFov = setting.TargetFov;
        _currentBlendTime = setting.BlendOutTime;

        LensSettings lens = _cinemachineCamera.Lens;
        lens.FieldOfView = setting.TargetFov;
        _cinemachineCamera.Lens = lens;
    }

    private void ApplyRuntimeBlend()
    {
        float currentFov = _cinemachineCamera.Lens.FieldOfView;
        float nextFov = Mathf.SmoothDamp(
            currentFov,
            _targetFov,
            ref _fovVelocity,
            _currentBlendTime);

        LensSettings lens = _cinemachineCamera.Lens;
        lens.FieldOfView = nextFov;
        _cinemachineCamera.Lens = lens;
    }
}
