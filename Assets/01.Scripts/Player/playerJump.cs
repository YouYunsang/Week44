using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerJump : MonoBehaviour
{
    [Header("점프 설정")]
    public float jumpForce = 5f;
    public float gravity = -9.81f;

    private CharacterController _cc;
    private float _verticalVelocity = 0f;
    private int _jumpCount = 0;
    private const int MAX_JUMP = 2; // 최대 점프 횟수

    void Start()
    {
        _cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        ApplyGravity();
        HandleJump();
    }

    void ApplyGravity()
    {
        if (_cc.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
            _jumpCount = 0; // 바닥에 닿으면 점프 횟수 초기화
        }

        _verticalVelocity += gravity * Time.deltaTime;
        _cc.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }

    void HandleJump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (_jumpCount < MAX_JUMP)
            {
                _verticalVelocity = jumpForce;
                _jumpCount++;

                Debug.Log($"점프 {_jumpCount}회");
            }
        }
    }
}