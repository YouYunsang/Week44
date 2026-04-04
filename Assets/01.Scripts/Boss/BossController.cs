using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 AI — 이동/애니메이션은 BossProceduralAnimator에 위임
/// 이 컴포넌트는 상태 결정만 담당
/// </summary>
[RequireComponent(typeof(BossPhaseHandler))]
[RequireComponent(typeof(BossProceduralAnimator))]
public class BossController : MonoBehaviour
{
    enum BossState { Idle, Chase, Attack, PhaseTransition }

    [Header("References")]
    [SerializeField] Transform _player;

    [Header("Range")]
    [SerializeField] float _detectionRange = 14f;
    [SerializeField] float _attackRange    = 2.4f;

    [Header("Attack Cooldown")]
    [SerializeField] float _phase1Cooldown = 5f;  // FullBody (발차기/점프밟기)
    [SerializeField] float _phase2Cooldown = 6f;  // ArmsOnly (팔 슬램)
    [SerializeField] float _phase3Cooldown = 4f;  // CoreOnly (박치기)

    BossPhaseHandler      _phaseHandler;
    BossProceduralAnimator _animator;

    BossState _state = BossState.Idle;
    float     _lastAttackTime = -999f;

    // ── 초기화 ────────────────────────────────────────────────────────────
    void Awake()
    {
        _phaseHandler = GetComponent<BossPhaseHandler>();
        _animator     = GetComponent<BossProceduralAnimator>();

        if (_player == null)
        {
            var obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) _player = obj.transform;
        }

        if (_player != null) _animator.SetTarget(_player);
    }

    void OnEnable()
    {
        EventBus<OnBossPhaseChangedEvent>.Subscribe(OnPhaseChanged);
        EventBus<OnBossDiedEvent>.Subscribe(OnBossDied);
    }
    void OnDisable()
    {
        EventBus<OnBossPhaseChangedEvent>.Unsubscribe(OnPhaseChanged);
        EventBus<OnBossDiedEvent>.Unsubscribe(OnBossDied);
    }

    void OnBossDied(OnBossDiedEvent e)
    {
        StopAllCoroutines();
        _state = BossState.PhaseTransition; // 더 이상 공격/추적 안 함
        _animator.SetMoving(false);
    }

    // ── 메인 루프 ──────────────────────────────────────────────────────────
    void Update()
    {
        if (_player == null) return;
        if (_state == BossState.Attack || _state == BossState.PhaseTransition) return;

        switch (_state)
        {
            case BossState.Idle:  UpdateIdle();  break;
            case BossState.Chase: UpdateChase(); break;
        }
    }

    void UpdateIdle()
    {
        if (DistToPlayer() <= _detectionRange)
            EnterChase();
    }

    void UpdateChase()
    {
        _animator.SetMoving(true);

        if (DistToPlayer() <= _attackRange && CanAttack())
            StartCoroutine(AttackRoutine());
    }

    // ── 상태 전환 ─────────────────────────────────────────────────────────
    void EnterChase()
    {
        _state = BossState.Chase;
        _animator.SetMoving(true);
    }

    IEnumerator AttackRoutine()
    {
        _state            = BossState.Attack;
        _lastAttackTime   = Time.time;
        _animator.SetMoving(false);

        yield return _animator.PlayAttack(_phaseHandler.CurrentPhase);

        _state = BossState.Chase;
        _animator.SetMoving(true);
    }

    void OnPhaseChanged(OnBossPhaseChangedEvent e)
    {
        StartCoroutine(PhaseTransitionRoutine(e.phase));
    }

    IEnumerator PhaseTransitionRoutine(BossPhase newPhase)
    {
        _state = BossState.PhaseTransition;
        _animator.SetMoving(false);

        yield return _animator.PlayPhaseTransition(newPhase);

        _state = BossState.Chase;
        _animator.SetMoving(true);
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────
    bool CanAttack()
    {
        float cooldown = _phaseHandler.CurrentPhase switch
        {
            BossPhase.FullBody => _phase1Cooldown,
            BossPhase.ArmsOnly => _phase2Cooldown,
            BossPhase.CoreOnly => _phase3Cooldown,
            _                  => _phase1Cooldown
        };
        return Time.time - _lastAttackTime >= cooldown;
    }
    float DistToPlayer() => Vector3.Distance(transform.position, _player.position);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
