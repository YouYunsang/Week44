using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerJump : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _gravity = -9.81f;

    [Header("Ground Check")]
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private InputSO _input;

    private CharacterController _characterController;
    private float _verticalVelocity = 0f;
    private bool _isGrounded = false;

    private void Awake()
    {
        // 자기 컴포넌트 캐싱
        _characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        // 점프 입력 이벤트 구독
        if (_input != null)
            _input.OnJump += HandleJump;
    }

    private void OnDisable()
    {
        // 점프 입력 이벤트 구독 해제
        if (_input != null)
            _input.OnJump -= HandleJump;
    }

    private void Update()
    {
        // 지면 체크 및 중력 처리
        CheckGround();
        ApplyGravity();
    }

    private void HandleJump()
    {
        // 착지 상태일 때만 점프 속도 부여
        if (_isGrounded)
            _verticalVelocity = _jumpForce;
    }

    private void CheckGround()
    {
        if (_groundCheck == null)
            return;

        // Overlap 체크로 지면 판별
        _isGrounded = Physics.CheckSphere(
            _groundCheck.position,
            _groundCheckRadius,
            _groundLayer
        );
    }

    private void ApplyGravity()
    {
        // 바닥에 붙어 있을 때 작은 음수 유지
        if (_isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        // 중력 누적 적용
        _verticalVelocity += _gravity * Time.deltaTime;

        // 수직 이동 적용
        _characterController.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        if (_groundCheck == null)
            return;

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
    }
}