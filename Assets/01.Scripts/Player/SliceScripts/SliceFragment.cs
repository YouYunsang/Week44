using UnityEngine;

/// <summary>
/// 슬라이스된 조각에 붙는 컴포넌트
/// 일정 시간 후 자동으로 제거됨
/// </summary>
public class SliceFragment : MonoBehaviour
{
    private float _destroyDelay = 3f;
    [SerializeField] float _extraGravityMultiplier = 3f;

    Rigidbody _rb;

    public void Init(float delay)
    {
        _rb = GetComponent<Rigidbody>();
        _destroyDelay = delay;
        Destroy(gameObject, _destroyDelay);
    }

    void FixedUpdate()
    {
        if (_rb != null)
            _rb.AddForce(Physics.gravity * (_extraGravityMultiplier - 1f), ForceMode.Acceleration);
    }
}
