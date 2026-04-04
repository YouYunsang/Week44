using UnityEngine;
using Unity.Cinemachine;

public class LandingReactionExtension : CinemachineExtension
{
    [Header("Pitch")]
    [SerializeField] private float _pitchSmall = 1.5f;
    [SerializeField] private float _pitchMedium = 3.0f;
    [SerializeField] private float _pitchLarge = 5.0f;

    [Header("Roll")]
    [SerializeField] private float _rollSmall = 0f;
    [SerializeField] private float _rollMedium = 0.5f;
    [SerializeField] private float _rollLarge = 1.2f;

    [Header("Timing")]
    [SerializeField] private float _duration = 0.14f;
    [SerializeField] private float _attackRatio = 0.25f;

    private float _timer = 0f;
    private float _targetPitch = 0f;
    private float _targetRoll = 0f;
    private bool _isPlaying = false;

    public void PlayLanding(LandingImpactType type)
    {
        _timer = 0f;
        _isPlaying = true;

        switch (type)
        {
            case LandingImpactType.Small:
                _targetPitch = _pitchSmall;
                _targetRoll = _rollSmall;
                break;

            case LandingImpactType.Medium:
                _targetPitch = _pitchMedium;
                _targetRoll = _rollMedium;
                break;

            case LandingImpactType.Large:
                _targetPitch = _pitchLarge;
                _targetRoll = _rollLarge;
                break;
        }
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

        _timer += deltaTime;

        float normalizedTime = _timer / _duration;

        if (normalizedTime >= 1f)
        {
            _isPlaying = false;
            return;
        }

        float reactionWeight = EvaluateReactionWeight(normalizedTime);

        float pitch = _targetPitch * reactionWeight;
        float roll = _targetRoll * reactionWeight;

        Quaternion offset = Quaternion.Euler(pitch, 0f, roll);
        state.RawOrientation = state.RawOrientation * offset;
    }

    private float EvaluateReactionWeight(float normalizedTime)
    {
        float attackEnd = _attackRatio;

        // 1) 빠르게 눌리는 구간
        if (normalizedTime <= attackEnd)
        {
            float t = normalizedTime / attackEnd;
            return Mathf.SmoothStep(0f, 1f, t);
        }

        // 2) 부드럽게 복귀하는 구간
        float releaseT = (normalizedTime - attackEnd) / (1f - attackEnd);
        return Mathf.SmoothStep(1f, 0f, releaseT);
    }
}