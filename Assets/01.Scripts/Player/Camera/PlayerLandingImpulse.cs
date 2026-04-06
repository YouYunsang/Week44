using UnityEngine;
using Unity.Cinemachine;

public class PlayerLandingImpulse : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerJump _playerJump;
    [SerializeField] private CinemachineImpulseSource _impulseSource;
    [SerializeField] private LandingReactionExtension _landingExtension;

    [Header("Landing Detection")]
    [SerializeField] private float _minAirTime = 0.1f;
    [SerializeField] private float _minLandingDownwardSpeed = 2f;

    [Header("Landing Speed Threshold")]
    [SerializeField] private float _mediumLandingSpeed = 6f;
    [SerializeField] private float _largeLandingSpeed = 10f;

    [Header("Impulse Strength")]
    [SerializeField] private float _smallImpulseStrength = 0.35f;
    [SerializeField] private float _mediumImpulseStrength = 0.7f;
    [SerializeField] private float _largeImpulseStrength = 1.1f;

    [Header("Impulse Duration")]
    [SerializeField] private float _smallImpulseDuration = 0.10f;
    [SerializeField] private float _mediumImpulseDuration = 0.14f;
    [SerializeField] private float _largeImpulseDuration = 0.18f;

    private bool _wasGrounded = true;
    private float _airTime = 0f;
    private float _lastDownwardSpeed = 0f;

    private float _defaultImpulseDuration;

    private void Awake()
    {
        if (_playerJump == null)
            _playerJump = GetComponent<PlayerJump>();

        if (_impulseSource == null)
            _impulseSource = GetComponent<CinemachineImpulseSource>();

        CacheDefaultImpulseSettings();
    }

    private void Start()
    {
        if (_playerJump == null) return;

        _wasGrounded = _playerJump.IsGrounded;
    }

    private void Update()
    {
        if (_playerJump == null) return;

        UpdateAirState();
        DetectLanding();

        _wasGrounded = _playerJump.IsGrounded;
    }

    private void UpdateAirState()
    {
        if (_playerJump.IsGrounded) return;

        _airTime += Time.deltaTime;

        if(_playerJump.VerticalVelocity < 0f)
            _lastDownwardSpeed = _playerJump.VerticalVelocity;
    }

    private void DetectLanding()
    {
        bool isGrounded = _playerJump.IsGrounded;

        if (_wasGrounded || !isGrounded) return;

        if(_airTime < _minAirTime)
        {
            ResetAirState();
            return;
        }

        float downwardSpeed = Mathf.Abs(_lastDownwardSpeed);

        if(downwardSpeed < _minLandingDownwardSpeed)
        {
            ResetAirState();
            return;
        }

        LandingImpactType impactType = EvaluateImpactType(downwardSpeed);
        float impulseStrength = GetImpulseStrength(impactType);
        float impulseDuration = GetImpulseDuration(impactType);

        GenerateLandingImpulse(impulseStrength, impulseDuration);

        if(_landingExtension != null)
            _landingExtension.PlayLanding(impactType);

        PublishLandingEvent(impactType, downwardSpeed, impulseStrength);

        ResetAirState();
    }

    private void CacheDefaultImpulseSettings()
    {
        if (_impulseSource == null || _impulseSource.ImpulseDefinition == null) return;

        _defaultImpulseDuration = _impulseSource.ImpulseDefinition.ImpulseDuration;
    }

    private LandingImpactType EvaluateImpactType(float downwardSpeed)
    {
        if (downwardSpeed >= _largeLandingSpeed) return LandingImpactType.Large;

        if(downwardSpeed >= _mediumLandingSpeed) return LandingImpactType.Medium;

        return LandingImpactType.Small;
    }

    private float GetImpulseStrength(LandingImpactType impactType)
    {
        switch (impactType)
        {
            case LandingImpactType.Large:
                return _largeImpulseStrength;
            
            case LandingImpactType.Medium:
                return _mediumImpulseStrength;

            default:
                return _smallImpulseStrength;
        }
    }

    private float GetImpulseDuration(LandingImpactType impactType)
    {
        switch (impactType)
        {
            case LandingImpactType.Large:
                return _largeImpulseDuration;

            case LandingImpactType.Medium:
                return _mediumImpulseDuration;

            default:
                return _smallImpulseDuration;
        }
    }

    private void GenerateLandingImpulse(float impulseStrength, float impulseDuration)
    {
        if (_impulseSource == null || _impulseSource.ImpulseDefinition == null) return;

        ApplyImpulseDuration(impulseDuration);

        _impulseSource.GenerateImpulseWithForce(impulseStrength);

        RestoreDefaultImpulseSettings();
    }

    private void ApplyImpulseDuration(float impulseDuration)
    {
        CinemachineImpulseDefinition definition = _impulseSource.ImpulseDefinition;

        definition.ImpulseDuration = impulseDuration;
    }

    private void RestoreDefaultImpulseSettings()
    {
        CinemachineImpulseDefinition definition = _impulseSource.ImpulseDefinition;
        definition.ImpulseDuration = _defaultImpulseDuration;
    }

    private void PublishLandingEvent(LandingImpactType impactType, float downwardSpeed, float impulseStrength)
    {
        EventBus<OnPlayerLandedEvent>.Publish(new OnPlayerLandedEvent
        {
            impactType = impactType,
            downwardSpeed = downwardSpeed,
            impulseStrength = impulseStrength,
        });
    }

    private void ResetAirState()
    {
        _airTime = 0f;
        _lastDownwardSpeed = 0f;
    }
}
