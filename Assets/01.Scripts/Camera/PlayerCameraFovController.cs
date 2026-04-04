using UnityEngine;
using Unity.Cinemachine;

public class PlayerCameraFovController : MonoBehaviour
{
    [Header("FOV Settings")]
    [SerializeField] private float _defaultFov = 60f;
    [SerializeField] private float _moveFov = 70f;
    [SerializeField] private float _smoothTime = 0.15f;

    private CinemachineCamera _cinemachineCamera;
    private float _targetFov;
    private float _fovVelocity;

    private void Awake()
    {
        _cinemachineCamera = GetComponent<CinemachineCamera>();

        _targetFov = _defaultFov;

        if (_cinemachineCamera != null)
            _cinemachineCamera.Lens.FieldOfView = _defaultFov;
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
        if (_cinemachineCamera == null) return;

        float currentFov = _cinemachineCamera.Lens.FieldOfView;
        float nextFov = Mathf.SmoothDamp(currentFov, _targetFov, ref _fovVelocity, _smoothTime);

        LensSettings lens = _cinemachineCamera.Lens;
        lens.FieldOfView = nextFov;
        _cinemachineCamera.Lens = lens;
    }

    private void HandleMoveStarted(OnPlayerMoveStartedEvent evt)
    {
        // 이동 시작 시 넓은 FOV 적용
        _targetFov = _moveFov;
    }

    private void HandleMoveStopped(OnPlayerMoveStoppedEvent evt)
    {
        // 이동 종료 시 기본 FOV 복귀
        _targetFov = _defaultFov;
    }
}
