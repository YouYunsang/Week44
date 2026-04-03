using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private InputSO _input;
    [SerializeField] private Transform _eyePivot;

    [Header("Look Settings")]
    [SerializeField] private float _mouseSensitivity = 0.15f;
    [SerializeField] private float _minPitch = -90f;
    [SerializeField] private float _maxPitch = 90f;

    private Vector2 _lookInput = Vector2.zero;
    private float _currentPitch = 0f;
    private bool _canLook = true;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        if (_input != null) _input.OnLook += HandleLook;
    }

    private void OnDisable()
    {
        if (_input != null) _input.OnLook -= HandleLook;
    }

    private void Update()
    {
        RotateLook();
    }

    private void HandleLook(Vector2 value)
    {
        _lookInput = value;
    }

    private void RotateLook()
    {
        if (!_canLook || _eyePivot == null) return;

        float yaw = _lookInput.x * _mouseSensitivity;
        float pitchDelta = _lookInput.y * _mouseSensitivity;

        transform.Rotate(0f, yaw, 0f);

        _currentPitch -= pitchDelta;
        _currentPitch = Mathf.Clamp(_currentPitch, _minPitch, _maxPitch);

        _eyePivot.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
    }

    public void SetLookEnabled(bool canLook)
    {
        _canLook = canLook;

        if (!canLook)
            _lookInput = Vector2.zero;
    }
}
