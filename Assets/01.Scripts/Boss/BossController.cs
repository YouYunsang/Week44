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
    [SerializeField] float _detectionRange  = 14f;
    [SerializeField] float _attackFacingAngle = 40f; // 공격 허용 전방 각도 (±)

    [Header("Debug Start State")]
    [SerializeField] bool _debugStartNoLegs;
    [SerializeField] bool _debugStartHeadOnly;

    BossPhaseHandler       _phaseHandler;
    BossProceduralAnimator _animator;

    BossState _state = BossState.Idle;

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

        if (_player != null)
            _animator.SetTarget(_player);
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
        _state = BossState.PhaseTransition;
        _animator.SetMoving(false);
    }

    void Start()
    {
        if (_debugStartNoLegs || _debugStartHeadOnly)
            StartCoroutine(DebugInitRoutine());
    }

    IEnumerator DebugInitRoutine()
    {
        yield return null; // 한 프레임 대기 — 모든 컴포넌트 초기화 완료 후 발행

        if (_debugStartHeadOnly)
        {
            EventBus<OnBossLimbSlicedEvent>.Publish(new OnBossLimbSlicedEvent { limb = LimbType.LeftArm });
            EventBus<OnBossLimbSlicedEvent>.Publish(new OnBossLimbSlicedEvent { limb = LimbType.RightArm });
        }
        else if (_debugStartNoLegs)
        {
            EventBus<OnBossLimbSlicedEvent>.Publish(new OnBossLimbSlicedEvent { limb = LimbType.LeftLeg });
            EventBus<OnBossLimbSlicedEvent>.Publish(new OnBossLimbSlicedEvent { limb = LimbType.RightLeg });
        }
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

        // 범위 안 + 쿨다운 완료 + 전방 각도 내에 있을 때 공격 시작
        if (DistToPlayer() <= _animator.CurrentAttackRange && _animator.CanAttack() && IsFacingPlayer())
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
        _state = BossState.Attack;
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
    float DistToPlayer() => Vector3.Distance(transform.position, _player.position);

    bool IsFacingPlayer()
    {
        if (_phaseHandler.CurrentPhase == BossPhase.CoreOnly) return true;

        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return true;
        return Vector3.Angle(transform.forward, dir.normalized) <= _attackFacingAngle;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        if (_animator != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _animator.CurrentAttackRange);
        }
    }
}
