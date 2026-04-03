using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 5f;

    [Header("마우스")]
    public float mouseSensitivity = 0.15f;

    [Header("카메라")]
    public Camera cam;

    public InputSO input;

    private CharacterController _cc;
    private Vector2 _moveInput = Vector2.zero;
    private Vector2 _lookInput = Vector2.zero;

    void Start()
    {
        _cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void OnEnable()
    {
        input.OnMove += HandleMove;
        input.OnLook += HandleLook;
    }

    void OnDisable()
    {
        input.OnMove -= HandleMove;
        input.OnLook -= HandleLook;
    }

    void HandleMove(Vector2 value) => _moveInput = value;
    void HandleLook(Vector2 value) => _lookInput = value;

    void Update()
    {
        RotatePlayer();
        Move();
    }

    // 플레이어 몸통 좌우 회전만 담당
    // 상하 회전은 시네머신에 위임
    void RotatePlayer()
    {
        transform.Rotate(0f, _lookInput.x * mouseSensitivity, 0f);
    }

    void Move()
    {
        // 인스펙터에서 받아온 cam 기준으로 이동 방향 계산
        Vector3 forward = cam.transform.forward;
        Vector3 right   = cam.transform.right;
        forward.y = 0f;
        right.y   = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = right * _moveInput.x + forward * _moveInput.y;
        _cc.Move(move * moveSpeed * Time.deltaTime);
    }
}