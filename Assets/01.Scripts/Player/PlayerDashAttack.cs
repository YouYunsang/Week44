using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerDashAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputSO _input;
    [SerializeField] private DashAttackDataSO _data;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private PlayerLook _playerLook;
    [SerializeField] private PlayerJump _playerJump;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private PlayerSliceExecutor _sliceExecutor;

    [Header("State")]
    [SerializeField] private int _currentStack = 0;
    [SerializeField] private float _currentRange = 0f;

    private float _rechargeTimer = 0f;
    private bool _isCharging = false;
    private bool _isDashing = false;
    private bool _isTargetInRange = false;

    public int CurrentStack => _currentStack;
    public float CurrentRange => _currentRange;
    public bool IsCharging => _isCharging;
    public bool IsDashing => _isDashing;
    public bool IsTargetInRange => _isTargetInRange;
    public bool IsTargetBullet {get; private set;}

    #region GUI용(나중에 지워도 됨)
    public float RechargeNormalized =>
        _data == null || _data.StackRechargeTime <= 0f
            ? 0f
            : Mathf.Clamp01(_rechargeTimer / _data.StackRechargeTime);

    public float ChargeNormalized =>
        _data == null || Mathf.Approximately(_data.MaxAttackRange, _data.MinAttackRange)
            ? 0f
            : Mathf.Clamp01(
                (_currentRange - _data.MinAttackRange) /
                (_data.MaxAttackRange - _data.MinAttackRange));
    #endregion

    private void Awake()
    {
        // 같은 오브젝트 내부 참조 캐싱
        if (_playerMovement == null)
            _playerMovement = GetComponent<PlayerMovement>();

        if (_playerLook == null)
            _playerLook = GetComponent<PlayerLook>();

        if (_playerJump == null)
            _playerJump = GetComponent<PlayerJump>();

        if (_characterController == null)
            _characterController = GetComponent<CharacterController>();

        if (_sliceExecutor == null)
            _sliceExecutor = GetComponent<PlayerSliceExecutor>();
    }

    private void Start()
    {
        // 시작 시 스택과 최소 사거리 초기화
        _currentStack = _data.MaxStack;
        _currentRange = _data.MinAttackRange;
    }

    private void OnEnable()
    {
        // 우클릭 대시 공격 입력 구독
        if (_input != null)
        {
            _input.OnDashAttackPressed += HandleDashAttackPressed;
            _input.OnDashAttackReleased += HandleDashAttackReleased;
        }
    }

    private void OnDisable()
    {
        // 우클릭 대시 공격 입력 구독 해제
        if (_input != null)
        {
            _input.OnDashAttackPressed -= HandleDashAttackPressed;
            _input.OnDashAttackReleased -= HandleDashAttackReleased;
        }
    }

    private void Update()
    {
        // 대시 중엔 차징 갱신 금지
        if (_isDashing)
            return;

        RechargeStack();
        UpdateCharge();
    }

    private void HandleDashAttackPressed()
    {
        // 대시 중이거나 스택이 없으면 시작 불가
        if (_isDashing || _currentStack <= 0)
            return;

        _isCharging = true;
        _currentRange = _data.MinAttackRange;
        _isTargetInRange = false;

        // 차징 시작 이벤트 발행
        EventBus<OnDashChargeStartedEvent>.Publish(new OnDashChargeStartedEvent());

        EventBus<OnCameraNoiseSignalEvent>.Publish(new OnCameraNoiseSignalEvent
        {
            channel = CameraNoiseChannel.DashCharge,
            isActive = true,
            normalized = 0f
        });

        EventBus<OnCameraFovSignalEvent>.Publish(new OnCameraFovSignalEvent
        {
            channel = CameraFovChannel.DashCharge,
            isActive = true,
            normalized = 0f
        });

        EventBus<OnCameraDutchSignalEvent>.Publish(new OnCameraDutchSignalEvent
        {
            channel = CameraDutchChannel.DashCharge,
            isActive = true,
            normalized = 0f
        });
    }

    private void HandleDashAttackReleased()
    {
        // 차징 상태일 때만 release 처리
        if (!_isCharging)
            return;

        _isCharging = false;

        bool success = TryDashAndSlice();
        if (!success)
        {
            // 실패 시 차징 취소 이벤트 발행
            EventBus<OnDashChargeCanceledEvent>.Publish(new OnDashChargeCanceledEvent());
        }

        EventBus<OnCameraNoiseSignalEvent>.Publish(new OnCameraNoiseSignalEvent
        {
            channel = CameraNoiseChannel.DashCharge,
            isActive = false,
            normalized = 0f
        });

        EventBus<OnCameraFovSignalEvent>.Publish(new OnCameraFovSignalEvent
        {
            channel = CameraFovChannel.DashCharge,
            isActive = false,
            normalized = 0f
        });

        EventBus<OnCameraDutchSignalEvent>.Publish(new OnCameraDutchSignalEvent
        {
            channel = CameraDutchChannel.DashCharge,
            isActive = false,
            normalized = 0f
        });

        _isTargetInRange = false;
    }

    private void RechargeStack()
    {
        if (_currentStack >= _data.MaxStack)
            return;

        // 스택 충전 타이머 진행
        _rechargeTimer += Time.deltaTime;

        if (_rechargeTimer >= _data.StackRechargeTime)
        {
            _currentStack = Mathf.Min(_currentStack + 1, _data.MaxStack);
            _rechargeTimer = 0f;
        }
    }

    private void UpdateCharge()
    {
        if (!_isCharging || _sliceExecutor == null)
            return;

        // 홀드 중 사거리 충전
        _currentRange = Mathf.Min(
            _currentRange + _data.ChargeSpeed * Time.deltaTime,
            _data.MaxAttackRange);

        // 현재 충전 사거리 내 타겟 존재 여부 확인
       if(_sliceExecutor.TryGetSliceHit(_currentRange, out RaycastHit hit))
        {
            _isTargetInRange = true;
            IsTargetBullet = hit.collider.CompareTag("Bullet");
        }
        else
        {
            _isTargetInRange = false;
            IsTargetBullet = false;
        }

        EventBus<OnCameraNoiseSignalEvent>.Publish(new OnCameraNoiseSignalEvent
        {
            channel = CameraNoiseChannel.DashCharge,
            isActive = true,
            normalized = ChargeNormalized
        });

        EventBus<OnCameraFovSignalEvent>.Publish(new OnCameraFovSignalEvent
        {
            channel = CameraFovChannel.DashCharge,
            isActive = true,
            normalized = ChargeNormalized
        });

        EventBus<OnCameraDutchSignalEvent>.Publish(new OnCameraDutchSignalEvent
        {
            channel = CameraDutchChannel.DashCharge,
            isActive = true,
            normalized = ChargeNormalized
        });
    }

    private bool TryDashAndSlice()
    {
        if (_sliceExecutor == null)
            return false;

        // release 시점의 유효 타겟 확인
        if (!_sliceExecutor.TryGetSliceHit(_currentRange, out RaycastHit hit))
        {
            _currentRange = _data.MinAttackRange;
            return false;
        }

        if(hit.collider.CompareTag("Bullet"))
        {
            _currentRange = _data.MinAttackRange;
            return false;
        }

        ConsumeStack();
        StartCoroutine(DashAndSlice(hit));
        return true;
    }

    private void ConsumeStack()
    {
        // 스택 소비 후 충전 타이머 초기화
        _currentStack = Mathf.Max(_currentStack - 1, 0);
        _rechargeTimer = 0f;
    }

    private IEnumerator DashAndSlice(RaycastHit hit)
    {
        _isDashing = true;

        // 대시 시작 이벤트 발행
        EventBus<OnDashStartedEvent>.Publish(new OnDashStartedEvent());

        EventBus<OnCameraFovSignalEvent>.Publish(new OnCameraFovSignalEvent
        {
            channel = CameraFovChannel.Dash,
            isActive = true,
            normalized = 1f
        });

        EventBus<OnCameraMotionBlurSignalEvent>.Publish(new OnCameraMotionBlurSignalEvent
        {
            channel = CameraMotionBlurChannel.Dash,
            isActive = true,
            normalized = 1f
        });

        // 대시 시작 이벤트 발행
        EventBus<OnDashStartedEvent>.Publish(new OnDashStartedEvent());

        // 조작 잠금
        if (_playerMovement != null)
            _playerMovement.SetMoveEnabled(false);

        if (_playerLook != null)
            _playerLook.SetLookEnabled(false);

        if (_playerJump != null)
            _playerJump.enabled = false;

        // 카메라 전방 기준 대시 도착 지점 계산
        Vector3 targetPos = hit.point - _sliceExecutor.GetCameraForward() * _data.StopDistance;

        float timer = 0f;

        while (timer < _data.DashTimeout)
        {
            Vector3 currentPos = _characterController.transform.position;
            float distance = Vector3.Distance(currentPos, targetPos);

            // 목표 지점 근접 시 종료
            if (distance <= 0.2f)
                break;

            Vector3 moveDirection = (targetPos - currentPos).normalized;

            // CharacterController 기반 대시 이동
            _characterController.Move(moveDirection * _data.DashSpeed * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }

        // 대시 종료 이벤트 발행
        // 대시 종료 이벤트 발행
        EventBus<OnDashEndedEvent>.Publish(new OnDashEndedEvent());

        EventBus<OnCameraFovSignalEvent>.Publish(new OnCameraFovSignalEvent
        {
            channel = CameraFovChannel.Dash,
            isActive = false,
            normalized = 0f
        });

        EventBus<OnCameraMotionBlurSignalEvent>.Publish(new OnCameraMotionBlurSignalEvent
        {
            channel = CameraMotionBlurChannel.Dash,
            isActive = false,
            normalized = 0f
        });

        // 공격 실행 이벤트 발행
        EventBus<OnDashStrikeEvent>.Publish(new OnDashStrikeEvent());

        // 실제 슬라이스 실행
        _sliceExecutor.ExecuteSlice(hit, WeaponSwingType.DashAttack);

        // 조작 복구
        if (_playerMovement != null)
            _playerMovement.SetMoveEnabled(true);

        if (_playerLook != null)
            _playerLook.SetLookEnabled(true);

        if (_playerJump != null)
            _playerJump.enabled = true;

        _currentRange = _data.MinAttackRange;
        _isDashing = false;
    }
}