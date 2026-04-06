using System.Collections;
using UnityEngine;

/// <summary>
/// 페이즈별 공격 패턴 실행.
/// BossController에서 StartCoroutine(Execute(...))로 호출.
///
/// Phase 1 (FullBody)  : 양팔 교대 스윙
/// Phase 2 (ArmsOnly)  : 양팔 동시 광역 슬램
/// Phase 3 (CoreOnly)  : 박치기 돌진
/// </summary>
public class BossAttackExecutor : MonoBehaviour
{
    [Header("Attack Points")]
    [SerializeField] Transform _leftArmPoint;
    [SerializeField] Transform _rightArmPoint;
    [SerializeField] Transform _headPoint;

    [Header("Hit Settings")]
    [SerializeField] float _meleeHitRadius = 1.2f;
    [SerializeField] float _meleeForce     = 8f;
    [SerializeField] float _slamHitRadius  = 2.5f;
    [SerializeField] float _slamForce      = 12f;
    [SerializeField] float _headForce      = 15f;
    [SerializeField] LayerMask _playerLayer;

    // BossController가 yield return으로 기다릴 수 있도록 IEnumerator 반환
    public IEnumerator Execute(BossPhase phase)
    {
        switch (phase)
        {
            case BossPhase.FullBody: yield return Phase1Attack(); break;
            case BossPhase.ArmsOnly: yield return Phase2Attack(); break;
            case BossPhase.CoreOnly: yield return Phase3Attack(); break;
        }
    }

    // ── Phase 1: 양팔 교대 스윙 ──────────────────────────────────────────
    IEnumerator Phase1Attack()
    {
        // 왼팔 선타
        yield return new WaitForSeconds(0.3f);
        HitAtPoint(_leftArmPoint, _meleeHitRadius, _meleeForce);

        yield return new WaitForSeconds(0.25f);

        // 오른팔 후타
        HitAtPoint(_rightArmPoint, _meleeHitRadius, _meleeForce);

        yield return new WaitForSeconds(0.4f);
    }

    // ── Phase 2: 양팔 동시 슬램 ──────────────────────────────────────────
    IEnumerator Phase2Attack()
    {
        yield return new WaitForSeconds(0.5f);  // 예비 동작

        HitAtPoint(_leftArmPoint,  _slamHitRadius, _slamForce);
        HitAtPoint(_rightArmPoint, _slamHitRadius, _slamForce);

        yield return new WaitForSeconds(0.6f);
    }

    // ── Phase 3: 박치기 돌진 ─────────────────────────────────────────────
    IEnumerator Phase3Attack()
    {
        yield return new WaitForSeconds(0.6f);  // 예비 동작

        HitAtPoint(_headPoint, _meleeHitRadius, _headForce);

        yield return new WaitForSeconds(0.5f);
    }

    // ── 히트박스 ─────────────────────────────────────────────────────────
    void HitAtPoint(Transform point, float radius, float force)
    {
        if (point == null) return;

        Collider[] hits = Physics.OverlapSphere(point.position, radius, _playerLayer);
        foreach (var col in hits)
        {
            if (!col.CompareTag("Player")) continue;

            Vector3 dir = (col.transform.position - point.position).normalized;
            EventBus<OnBossAttackHitEvent>.Publish(new OnBossAttackHitEvent
            {
                direction = dir,
                force     = force
            });
            break;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
        if (_leftArmPoint)  Gizmos.DrawWireSphere(_leftArmPoint.position,  _meleeHitRadius);
        if (_rightArmPoint) Gizmos.DrawWireSphere(_rightArmPoint.position, _meleeHitRadius);
        if (_headPoint)     Gizmos.DrawWireSphere(_headPoint.position,      _meleeHitRadius);
    }
}
