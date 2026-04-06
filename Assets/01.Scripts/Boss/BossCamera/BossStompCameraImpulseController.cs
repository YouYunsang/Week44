using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class BossStompCameraImpulseController : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private CinemachineImpulseSource _impulseSource;
    [SerializeField] private Transform _player;

    [Header("Stomp Impulse Settings")]
    [SerializeField] private float _maxStrength = 2.5f;
    [SerializeField] private float _minStrength = 0.3f;

    [SerializeField] private float _minDistance = 2f;
    [SerializeField] private float _maxDistance = 18f;

    [Header("Boss Stomp Noise Settings")]
    [SerializeField] private float _bossStompNoiseDuration = 1f;

    private Coroutine _bossStompNoiseRoutine;

    private void OnEnable()
    {
        EventBus<OnBossStompImpactEvent>.Subscribe(OnStompImpact);
    }

    private void OnDisable()
    {
        EventBus<OnBossStompImpactEvent>.Unsubscribe(OnStompImpact);

        if (_bossStompNoiseRoutine != null)
        {
            StopCoroutine(_bossStompNoiseRoutine);
            _bossStompNoiseRoutine = null;
        }

        EventBus<OnCameraNoiseSignalEvent>.Publish(new OnCameraNoiseSignalEvent
        {
            channel = CameraNoiseChannel.BossStomp,
            isActive = false,
            normalized = 0f
        });
    }

    private void OnStompImpact(OnBossStompImpactEvent evt)
    {
        if (_player == null)
            return;

        float distanceNormailized = EvaluateDistanceNormalized(evt.position);
        if (distanceNormailized <= 0) return;

        float impulseStrength = EvaluateImpulseStrength(distanceNormailized);

        TriggerImpulse(impulseStrength);
        PlayBossStompNoise(distanceNormailized);
    }

    private void TriggerImpulse(float strength)
    {
        if (_impulseSource == null)
            return;

        _impulseSource.GenerateImpulse(strength);
    }

    private float EvaluateDistanceNormalized(Vector3 stomPos)
    {
        if (_player == null) return 0f;

        float distance = Vector3.Distance(_player.position, stomPos);

        if (distance >= _maxDistance) return 0f;

        if(distance <= _minDistance) return 1f;

        return Mathf.InverseLerp(_maxDistance, _minDistance, distance);
    }

    private float EvaluateImpulseStrength(float distanceNormalized)
    {
        return Mathf.Lerp(_minStrength, _maxStrength, distanceNormalized);
    }

    private void PlayBossStompNoise(float distanceNormalized)
    {
        if (_bossStompNoiseRoutine != null)
            StopCoroutine(_bossStompNoiseRoutine);

        _bossStompNoiseRoutine = StartCoroutine(BossStompNoiseRoutine(distanceNormalized));
    }

    private IEnumerator BossStompNoiseRoutine(float startNormalized)
    {
        float elapsed = 0f;

        while (elapsed < _bossStompNoiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _bossStompNoiseDuration);
            float timeFade = 1f - t;

            float normalized = startNormalized * timeFade;

            EventBus<OnCameraNoiseSignalEvent>.Publish(new OnCameraNoiseSignalEvent
            {
                channel = CameraNoiseChannel.BossStomp,
                isActive = true,
                normalized = normalized
            });

            yield return null;
        }

        EventBus<OnCameraNoiseSignalEvent>.Publish(new OnCameraNoiseSignalEvent
        {
            channel = CameraNoiseChannel.BossStomp,
            isActive = false,
            normalized = 0f
        });

        _bossStompNoiseRoutine = null;
    }
}