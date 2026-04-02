using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJump : MonoBehaviour
{
    [Header("점프 설정")]
    public float jumpForce = 5f;
    public float gravity   = -9.81f;

    [Header("그라운드 체크")]
    public Transform groundCheck;
    public float     groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private CharacterController _cc;
    private float _verticalVelocity = 0f;
    private bool  _isGrounded       = false;

    void Start()
    {
        _cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        CheckGround();
        HandleJump();
        ApplyGravity();
    }

    void CheckGround()
    {
        _isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayer);
    }
//
    void HandleJump()
    {
        // 바닥에 있을 때만 점프 가능
        if (Keyboard.current.spaceKey.wasPressedThisFrame && _isGrounded)
        {
            _verticalVelocity = jumpForce;
        }
    }

    void ApplyGravity()
    {
        if (_isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        _verticalVelocity += gravity * Time.deltaTime;
        _cc.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        if (groundCheck == null) return;

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}