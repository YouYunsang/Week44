using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 5f;

    [Header("마우스")]
    public float mouseSensitivity = 0.15f;
    public Transform cameraTransform;

    private CharacterController cc;
    private float xRotation = 0f;
    private float gravity = -9.81f;
    private float verticalVelocity = 0f;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Look();
        Move();
    }

    void Look()
    { 
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        xRotation -= mouseDelta.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); // 카메라 상하
        transform.Rotate(0f, mouseDelta.x * mouseSensitivity, 0f);           // 플레이어 좌우
    }

    void Move()
    {
        // WASD
        float h = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
        float v = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);

        Vector3 move = transform.right * h + transform.forward * v;
        cc.Move(move * moveSpeed * Time.deltaTime);

        // 중력
        if (cc.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        cc.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}
