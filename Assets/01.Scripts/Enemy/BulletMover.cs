using UnityEngine;

/// <summary>
/// Transform.Translate 방식으로 총알을 직선 이동시킵니다.
/// OnTriggerEnter 작동을 위해 Kinematic Rigidbody를 자동으로 추가합니다.
/// EnemyController 에서 Initialize() 를 자동 호출합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BulletMover : MonoBehaviour
{
    [Tooltip("총알 생존 시간 (초)")]
    public float Lifetime = 5f;

    [Tooltip("적에게 입히는 데미지 (IDamageable 인터페이스 사용 시)")]
    public float Damage = 10f;

    [Tooltip("플레이어 레이어 (EnemyController의 PlayerLayer와 동일하게 설정)")]
    public LayerMask PlayerLayer;

    private Vector3 _direction;
    private float _speed;
    private bool _initialized;
    private bool _isDestroyed; // 중복 Destroy 방지

    private void Awake()
    {
        if (!TryGetComponent(out Rigidbody rb))
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Initialize(Vector3 direction, float speed)
    {
        _direction = direction.normalized;
        _speed = speed;
        _initialized = true;
        Destroy(gameObject, Lifetime);
    }

    private void Update()
    {
        if (!_initialized) return;
        transform.Translate(_direction * _speed * Time.deltaTime, Space.World);
    }

    // ── 플레이어 판별 ──────────────────────────────
    /// <summary>
    /// 태그가 "Player" 이거나 PlayerLayer에 포함된 레이어면 true
    /// (1 << obj.layer) : 해당 오브젝트의 레이어를 비트 마스크로 변환
    /// PlayerLayer.value와 & 연산 → 겹치는 레이어가 있으면 0이 아님
    /// </summary>
    private bool IsPlayer(GameObject obj)
    {
        if (obj.CompareTag("Player")) return true;
        if (PlayerLayer != 0 && (PlayerLayer.value & (1 << obj.layer)) != 0) return true;
        return false;
    }

    // ── 3D 충돌 ────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (other.CompareTag("Turret")) return;

        // 플레이어 태그 또는 플레이어 레이어가 아니면 무시
        if (!IsPlayer(other.gameObject)) return;

        if (other.TryGetComponent(out IDamageable damageable))
            damageable.TakeDamage(Damage);
        HitDestroy();
    }

    private void OnCollisionEnter(Collision other)
    {
        // null 체크: 충돌 데이터 또는 gameObject가 이미 파괴된 경우 방어
        if (other == null || other.gameObject == null) return;
        // 이미 소멸 처리된 경우 중복 실행 방지
        if (_isDestroyed) return;
        // 터렛 자신과의 충돌 무시
        if (other.gameObject.CompareTag("Enemy")) return;

        // 플레이어 태그 또는 플레이어 레이어가 아니면 무시
        if (!IsPlayer(other.gameObject)) return;

        // 데미지 처리: 컴포넌트가 없어도 충돌 자체는 유효
        if (other.gameObject.TryGetComponent(out IDamageable damageable))
            damageable.TakeDamage(Damage);
        HitDestroy();
    }

    // ── 소멸 처리 ──────────────────────────────────
    /// <summary>
    /// 중복 Destroy 호출을 막기 위해 플래그를 먼저 세운 뒤 소멸합니다.
    /// </summary>
    private void HitDestroy()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;
        Destroy(gameObject);
    }
}

/// <summary>
/// 데미지를 받을 수 있는 오브젝트가 구현하는 인터페이스
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}