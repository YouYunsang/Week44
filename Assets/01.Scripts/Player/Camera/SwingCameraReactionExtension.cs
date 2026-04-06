using UnityEngine;
using Unity.Cinemachine;

public class SwingCameraReactionExtension : CinemachineExtension
{
    [Header("Normal Attack")]
    [SerializeField] private float _normalPitchAmount = 1.2f;
    [SerializeField] private float _normalYawAmount = 1.0f;
    [SerializeField] private float _normalRollAmount = 0.4f;
    [SerializeField] private float _normalDuration = 0.10f;

    [Header("Dash Attack")]
    [SerializeField] private float _dashPitchAmount = 2.5f;
    [SerializeField] private float _dashYawAmount = 2.0f;
    [SerializeField] private float _dashRollAmount = 0.9f;
    [SerializeField] private float _dashDuration = 0.13f;

    [Header("Timing")]
    [SerializeField, Range(0.1f, 0.5f)] private float _attackRatio = 0.28f;

    private bool _isPlaying = false;
    private float _timer = 0f;
    private float _duration = 0.1f;

    private float _targetPitch = 0f;
    private float _targetYaw = 0f;
    private float _targetRoll = 0f;

    private void OnEnable()
    {
        EventBus<OnWeaponSwingEvent>.Subscribe(HandleWeaponSwing);
    }

    private void OnDisable()
    {
        EventBus<OnWeaponSwingEvent>.Unsubscribe(HandleWeaponSwing);
    }

    private void HandleWeaponSwing(OnWeaponSwingEvent evt)
    {
        Vector2 direction = evt.direction.normalized;

        float pitchAmount;
        float yawAmount;
        float rollAmount;

        switch (evt.swingType)
        {
            case WeaponSwingType.DashAttack:
                pitchAmount = _dashPitchAmount;
                yawAmount = _dashYawAmount;
                rollAmount = _dashRollAmount;
                _duration = _dashDuration;
                break;

            default:
                pitchAmount = _normalPitchAmount;
                yawAmount = _normalYawAmount;
                rollAmount = _normalRollAmount;
                _duration = _normalDuration;
                break;
        }

        // 방향 기반 목표 회전 계산
        _targetPitch = -direction.y * pitchAmount;
        _targetYaw = direction.x * yawAmount;
        _targetRoll = -direction.x * rollAmount;

        // 새 스윙이 들어오면 즉시 새 반응으로 갱신
        _timer = 0f;
        _isPlaying = true;
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (!_isPlaying)
            return;

        if (stage != CinemachineCore.Stage.Aim)
            return;

        if (deltaTime < 0f)
            return;

        _timer += deltaTime;

        float normalizedTime = _timer / _duration;
        if (normalizedTime >= 1f)
        {
            _isPlaying = false;
            return;
        }

        float weight = EvaluateReactionWeight(normalizedTime);

        float pitch = _targetPitch * weight;
        float yaw = _targetYaw * weight;
        float roll = _targetRoll * weight;

        Quaternion offset = Quaternion.Euler(pitch, yaw, roll);
        state.RawOrientation = state.RawOrientation * offset;
    }

    private float EvaluateReactionWeight(float normalizedTime)
    {
        float attackEnd = _attackRatio;

        // 1) 빠르게 들어가는 구간
        if (normalizedTime <= attackEnd)
        {
            float t = normalizedTime / attackEnd;
            return Mathf.SmoothStep(0f, 1f, t);
        }

        // 2) 빠르게 복귀하는 구간
        float releaseT = (normalizedTime - attackEnd) / (1f - attackEnd);
        return Mathf.SmoothStep(1f, 0f, releaseT);
    }
}
