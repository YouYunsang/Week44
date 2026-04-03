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

    public bool IsMoving => _isMoving;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (_input != null)
            _input.OnMove += HandleMove;
    }

    private void OnDisable()
    {
        if (_input != null)
            _input.OnMove -= HandleMove;
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
        bool shouldMove = _canMove && _moveInput.sqrMagnitude > 0.0001f;

        if(!_isMoving && shouldMove)
        {
            _isMoving = true;
            EventBus<OnPlayerMoveStartedEvent>.Publish(new OnPlayerMoveStartedEvent());
            return;
        }

        if(_isMoving && !shouldMove)
        {
            _isMoving = false;
            EventBus<OnPlayerMoveStoppedEvent>.Publish(new OnPlayerMoveStoppedEvent());
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
