using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private InputSO _input;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;

    private CharacterController _characterController;
    private Vector2 _moveInput = Vector2.zero;
    private bool _canMove = true;
    private bool _isMoving = false;
    private bool _isGrounded = true;

    public bool IsMoving => _isMoving;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (_input != null) _input.OnMove += HandleMove;
        EventBus<OnMenuOpenEvent>.Subscribe(OnMenuOpen);
        EventBus<OnMenuCloseEvent>.Subscribe(OnMenuClose);
        EventBus<OnPlayerGroundedChangedEvent>.Subscribe(OnPlayerGroundedChanged);
    }

    private void OnDisable()
    {
        if (_input != null) _input.OnMove -= HandleMove;
        EventBus<OnMenuOpenEvent>.Unsubscribe(OnMenuOpen);
        EventBus<OnMenuCloseEvent>.Unsubscribe(OnMenuClose);
        EventBus<OnPlayerGroundedChangedEvent>.Unsubscribe(OnPlayerGroundedChanged);
    }

    private void OnMenuOpen(OnMenuOpenEvent e)   => SetMoveEnabled(false);
    private void OnMenuClose(OnMenuCloseEvent e) => SetMoveEnabled(true);

    private void OnPlayerGroundedChanged(OnPlayerGroundedChangedEvent e)
    {
        _isGrounded = e.isGrounded;

        // 공중으로 뜨는 순간 Move 상태 강제 종료
        if (!_isGrounded && _isMoving)
        {
            StopMoveState();
        }
    }

    private void Update()
    {
        UpdateMoveState();

        Move();
    }

    private void HandleMove(Vector2 value)
    {
        // 입력값 저장
        _moveInput = value;
    }

    #region Move
    private void UpdateMoveState()
    {
        bool shouldMove = _canMove && _isGrounded && _moveInput.sqrMagnitude > 0.0001f;

        if(!_isMoving && shouldMove)
        {
            _isMoving = true;
            EventBus<OnPlayerMoveStartedEvent>.Publish(new OnPlayerMoveStartedEvent());

            EventBus<OnCameraNoiseSignalEvent>.Publish(new OnCameraNoiseSignalEvent
            {
                channel = CameraNoiseChannel.Move,
                isActive = true,
                normalized = 1f
            });

            EventBus<OnCameraFovSignalEvent>.Publish(new OnCameraFovSignalEvent
            {
                channel = CameraFovChannel.Move,
                isActive = true,
                normalized = 1f
            });

            return;
        }

        if(_isMoving && !shouldMove)
        {
            _isMoving = false;
            EventBus<OnPlayerMoveStoppedEvent>.Publish(new OnPlayerMoveStoppedEvent());

            EventBus<OnCameraNoiseSignalEvent>.Publish(new OnCameraNoiseSignalEvent
            {
                channel = CameraNoiseChannel.Move,
                isActive = false,
                normalized = 0f
            });

            EventBus<OnCameraFovSignalEvent>.Publish(new OnCameraFovSignalEvent
            {
                channel = CameraFovChannel.Move,
                isActive = false,
                normalized = 0f
            });
        }

        if(_isMoving && !shouldMove)
        {
            StopMoveState();
        }
    }

    private void Move()
    {
        if (!_canMove) return;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = right * _moveInput.x + forward * _moveInput.y;

        _characterController.Move(moveDirection * _moveSpeed * Time.deltaTime);
    }

    private void StopMoveState()
    {
        _isMoving = false;
        EventBus<OnPlayerMoveStoppedEvent>.Publish(new OnPlayerMoveStoppedEvent());

        EventBus<OnCameraNoiseSignalEvent>.Publish(new OnCameraNoiseSignalEvent
        {
            channel = CameraNoiseChannel.Move,
            isActive = false,
            normalized = 0f
        });
    }

    public void SetMoveEnabled(bool canMove)
    {
        _canMove = canMove;

        if (!canMove)
        {
            _moveInput = Vector2.zero;

            if (_isMoving)
            {
                _isMoving = false;
                EventBus<OnPlayerMoveStoppedEvent>.Publish(new OnPlayerMoveStoppedEvent());
            }
        }
    }
    #endregion
}
