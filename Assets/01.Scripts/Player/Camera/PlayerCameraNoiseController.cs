using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class PlayerCameraNoiseController : MonoBehaviour
{
    [Header("Noise Settings")]
    [SerializeField] private float _idleAmplitude = 0f;
    [SerializeField] private float _moveAmplitude = 1f;
    [SerializeField] private float _lerpSpeed = 8f;

    private CinemachineBasicMultiChannelPerlin _perlin;
    private float _targetAmplitude = 0f;

    private void Awake()
    {
        _perlin = GetComponent<CinemachineBasicMultiChannelPerlin>();

        if(_perlin != null)
        {
            _targetAmplitude = _idleAmplitude;
            _perlin.AmplitudeGain = _idleAmplitude;
        }
    }

    private void OnEnable()
    {
        EventBus<OnPlayerMoveStartedEvent>.Subscribe(HandleMoveStarted);
        EventBus<OnPlayerMoveStoppedEvent>.Subscribe(HandleMoveStopped);
    }

    private void OnDisable()
    {
        EventBus<OnPlayerMoveStartedEvent>.Unsubscribe(HandleMoveStarted);
        EventBus<OnPlayerMoveStoppedEvent>.Unsubscribe(HandleMoveStopped);
    }

    private void Update()
    {
        if (_perlin == null) return;

        _perlin.AmplitudeGain = Mathf.Lerp(
            _perlin.AmplitudeGain,
            _targetAmplitude,
            _lerpSpeed * Time.deltaTime);
    }

    private void HandleMoveStarted(OnPlayerMoveStartedEvent evt)
    {
        // 이동 시작 시 Noise 활성화
        _targetAmplitude = _moveAmplitude;
    }

    private void HandleMoveStopped(OnPlayerMoveStoppedEvent evt)
    {
        // 이동 종료 시 Noise 비활성화
        _targetAmplitude = _idleAmplitude;
    }
}
