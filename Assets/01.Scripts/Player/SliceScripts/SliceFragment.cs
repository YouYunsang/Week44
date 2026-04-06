using UnityEngine;

/// <summary>
/// 슬라이스된 조각에 붙는 컴포넌트
/// 일정 시간 후 자동으로 제거됨
/// </summary>
public class SliceFragment : MonoBehaviour
{
    private const float DESTROY_DELAY = 6f;
    [SerializeField] float _extraGravityMultiplier = 3f;

    Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        Destroy(gameObject, DESTROY_DELAY);
    }

    void FixedUpdate()
    {
        if (_rb != null)
            _rb.AddForce(Physics.gravity * (_extraGravityMultiplier - 1f), ForceMode.Acceleration);
    }
}
