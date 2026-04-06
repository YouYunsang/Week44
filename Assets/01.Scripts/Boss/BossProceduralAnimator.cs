using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 보스 절차적 애니메이션 — 메인 진입점
///
/// 페이즈별 공격 패턴 (각 페이즈당 하나의 패턴):
///   Phase 1 (FullBody)  : 발차기
///   Phase 2 (ArmsOnly)  : 팔 슬램 (점프 후 내리찍기)
///   Phase 3 (CoreOnly)  : 구르기 돌진
///
/// 파일 구조 (partial class):
///   BossProceduralAnimator.cs          — 필드 / 상태 / API / 라이프사이클
///   BossProceduralAnimator.Movement.cs — 이동·대기 루틴
///   BossProceduralAnimator.Attacks.cs  — 공격 루틴
///   BossProceduralAnimator.Phases.cs   — 페이즈 전환 루틴
///   BossProceduralAnimator.Helpers.cs  — 유틸 헬퍼 / Gizmo
/// </summary>
public partial class BossProceduralAnimator : MonoBehaviour
{
    // ── 신체 파츠 ──────────────────────────────────────────────────────────
    [Header("Body Parts")]
    [SerializeField] Transform _body;
    [SerializeField] Transform _head;
    [SerializeField] Transform _leftArm;
    [SerializeField] Transform _rightArm;
    [SerializeField] Transform _leftLeg;
    [SerializeField] Transform _rightLeg;

    // ── 이동 파라미터 ──────────────────────────────────────────────────────
    [Header("Walk")]
    [SerializeField] float _walkCycleSpeed  = 2f;
    [SerializeField] float _walkPushPerStep = 2f;
    [SerializeField] float _legSwingAngle   = 45f;
    [SerializeField] float _armSwingAngle   = 45f;
    [SerializeField] float _walkBodyBob     = 0.07f;

    [Header("Arm Walk (ArmsOnly)")]
    [SerializeField] float _armWalkCycleSpeed  = 0.7f;
    [SerializeField] float _armWalkPushPerStep = 1.4f;
    [SerializeField] float _armWalkSwingAngle  = 55f;
    [SerializeField] float _armWalkBob         = 0.05f;

    [Header("Float (CoreOnly)")]
    [SerializeField] float _floatSpeed      = 2.5f;   // 이동 속도 (units/sec)
    [SerializeField] float _floatSwaySpeed  = 1.4f;   // 좌우 흔들림 사이클 속도
    [SerializeField] float _floatRollAngle  = 20f;    // 좌우 기울기 각도
    [SerializeField] float _floatTiltAngle  = 22f;    // 전진 시 앞으로 기울기
    [SerializeField] float _hoverHeight     = 0.4f;   // 지면 위 호버 높이
    [SerializeField] float _hoverBobSpeed   = 2.8f;   // 상하 둥실 속도
    [SerializeField] float _hoverBobAmount  = 0.09f;  // 상하 둥실 진폭

    [Header("Idle")]
    [SerializeField] float _breathSpeed    = 1.0f;
    [SerializeField] float _breathAmount   = 0.03f;
    [SerializeField] float _armMenaceSpeed = 0.8f;
    [SerializeField] float _armMenaceAngle = 22f;

    // ── 공격 파라미터 ──────────────────────────────────────────────────────
    [Header("Attack — Kick")]
    [SerializeField] float   _kickAttackRange = 12f;
    [SerializeField] float   _kickWindupAngle = -90f;
    [SerializeField] float   _kickSwingAngle  =  130f;
    [SerializeField] float   _kickWindupTime  =  0.7f;
    [SerializeField] float   _kickSwingTime   =  0.1f;
    [SerializeField] float   _kickReturnTime  =  0.4f;
    [SerializeField] float   _kickForce       =  11f;
    [SerializeField] Vector3 _kickSize        = new Vector3(2f, 6.8f, 17f);
    [SerializeField] Vector3 _kickOffset      = new Vector3(0, 0, -6.5f);

    [Header("Attack — Jump Stomp (FullBody, leg cut)")]
    [SerializeField] float   _stompAttackRange = 24f;
    [SerializeField] float   _jumpHeight       =  3f;
    [SerializeField] float   _jumpAscentTime   =  0.2f;
    [SerializeField] float   _jumpHangTime     =  1f;
    [SerializeField] float   _jumpDescentTime  =  0.3f;
    [SerializeField] float   _jumpLegTuckAngle = 90f;
    [SerializeField] float   _stompForce       =  18f;
    [SerializeField] Vector3 _stompSize        = new Vector3(2f, 6.8f, 12f);
    [SerializeField] Vector3 _stompOffset      = new Vector3(0, 0f, -6.5f);

    [Header("Attack — Arm Spin (ArmsOnly, one arm cut)")]
    [SerializeField] float _swingAttackRange = 12f;
    [SerializeField] float _spinRaiseAngle   = -65f;  // 팔 들기 각도 (Z축)
    [SerializeField] float _spinWindupTime   =  0.55f;
    [SerializeField] float _spinDuration     =  0.65f;
    [SerializeField] float _spinReturnTime   =  0.45f;
    [SerializeField] float _spinRadius       =  5.0f;
    [SerializeField] float _spinForce        =  15f;

    [Header("Attack — Arm Slam (ArmsOnly, both arms)")]
    [SerializeField] float   _slamAttackRange = 40f;
    [SerializeField] float   _slamRaiseAngle  = 190f;
    [SerializeField] float   _slamDownAngle   =  -5f;
    [SerializeField] float   _slamJumpHeight  =  5f;
    [SerializeField] float   _slamRaiseTime   =  1.3f;
    [SerializeField] float   _slamDownTime    =  0.4f;
    [SerializeField] float   _slamReturnTime  =  0.3f;
    [SerializeField] float   _slamForce       =  14f;
    [SerializeField] Vector3 _slamSize        = new Vector3(11f, 6f, 15f);
    [SerializeField] Vector3 _slamOffset      = new Vector3(1, 0, 0f);

    [Header("Attack — Roll (CoreOnly)")]
    [SerializeField] float _rollAttackRange = 42f;
    [SerializeField] float _rollSpeed     = 20f;   // 돌진 속도 (units/sec)
    [SerializeField] float _rollRotSpeed = 720f;   // 구르기 회전 속도 (degrees/sec)
    [SerializeField] float _rollDuration =  1.2f;  // 돌진 지속 시간
    [SerializeField] float _rollHitRadius =  3f;   // 판정 반지름
    [SerializeField] float _rollForce    = 18f;

    // ── 페이즈 전환 ────────────────────────────────────────────────────────
    [Header("Phase Transition")]
    [SerializeField] float _legHeight     = 1.0f;
    [SerializeField] float _fallBodyPitch = 28f;
    [SerializeField] float _fallTime      = 0.55f;
    [SerializeField] float _fallShakeMag  = 0.05f;
    [SerializeField] float _fallShakeDur  = 0.4f;

    // ── 공통 ───────────────────────────────────────────────────────────────
    [Header("Common")]
    [SerializeField] float     _rotateSpeed = 5f;
    [SerializeField] float     _returnSpeed = 9f;
    /// <summary>바닥 레이캐스트 레이어 — 보스 레이어 제외 권장</summary>
    [SerializeField] LayerMask _groundLayer = ~0;

    [Header("Range Indicator")]
    [SerializeField] BossRangeIndicator _rangeIndicator;

    // 인디케이터 슬롯 상수 (풀 크기 4 — BossRangeIndicator._pool)
    // 킥과 스톰프는 FullBody에서 동시에 쓰이지 않으므로 슬롯 0 공유
    const int SLOT_KICK  = 0;
    const int SLOT_STOMP = 0; // 킥과 같은 슬롯, 둘 다 ShowRect(16pts)라 충돌 없음
    const int SLOT_SLAM  = 1;
    const int SLOT_SPIN  = 2; // circle(33pts) — rect와 positionCount가 달라 전용 슬롯 사용
    const int SLOT_HEAD  = 3;

    [Header("Attack Cooldowns")]
    [SerializeField] float _kickCooldown     = 3f;
    [SerializeField] float _stompCooldown    = 5f;
    [SerializeField] float _spinCooldown     = 3f;
    [SerializeField] float _slamCooldown     = 6f;
    [SerializeField] float _rollCooldown     = 2.5f;
    [SerializeField] int   _rollCount        = 3;
    [SerializeField] float _rollPauseBetween = 0.4f;

    // ── 내부 상태 ──────────────────────────────────────────────────────────
    BossPhase _phase = BossPhase.FullBody;

    // PhaseHandler에서 직접 읽으므로 별도 추적 불필요
    BossPhaseHandler _phaseHandler;

    Transform _target;
    bool      _moving;
    bool      _attacking;
    float     _cycle;

    float _lastAttackTime = -999f;
    float _currentCooldown;

    // 현재 페이즈에서 보스 루트의 정지 Y 위치
    // FallDownRoutine / ArmSlamRoutine 착지 시 갱신됨
    float _groundY;

    Quaternion _bodyRest, _headRest;
    Quaternion _lArmRest, _rArmRest;
    Quaternion _lLegRest, _rLegRest;
    Vector3    _bodyRestPos;

    // ── 공개 API ──────────────────────────────────────────────────────────
    public void SetTarget(Transform t) => _target = t;
    public void SetMoving(bool m)      => _moving = m;

    /// <summary>현재 페이즈·상황에서 공격을 시작할 최대 거리</summary>
    public float CurrentAttackRange => _phase switch
    {
        // FullBody: 다리 멀쩡하면 킥, 하나라도 잘리면 점프 스톰프 (사거리 더 넓음)
        BossPhase.FullBody => _phaseHandler.LegsCut == 0 ? _kickAttackRange : _stompAttackRange,
        BossPhase.ArmsOnly => AnyArmCut() ? _swingAttackRange : _slamAttackRange,
        BossPhase.CoreOnly => _rollAttackRange,
        _                  => _kickAttackRange
    };

    /// <summary>쿨다운이 끝났으면 true — BossController가 공격 조건 판단에 사용</summary>
    public bool CanAttack() => Time.time - _lastAttackTime >= _currentCooldown;

    /// <summary>
    /// 팔 하나 이상 절단 여부 확인.
    /// PhaseHandler 이벤트가 오지 않은 경우에도 BossLimb.IsSliced로 보완.
    /// </summary>
    bool AnyArmCut()
    {
        if (_phaseHandler.ArmsCut >= 1) return true;
        return IsArmCut(_leftArm) || IsArmCut(_rightArm);
    }

    static bool IsArmCut(Transform arm)
    {
        if (arm == null) return false;
        var limb = arm.GetComponent<BossLimb>()
                ?? arm.GetComponentInParent<BossLimb>()
                ?? arm.GetComponentInChildren<BossLimb>();
        return limb?.IsSliced ?? false;
    }

    [Header("Attack — Roll Stop")]
    [SerializeField] float _rollStopDuration = 0.6f;
    [SerializeField] float _rollHeadY        = 2.5f;

    static readonly WaitForSeconds WaitOneSecond = new(1.0f);

    /// <summary>페이즈 전환 연출 (ArmsOnly: 쓰러지기, CoreOnly: 잠깐 대기)</summary>
    public IEnumerator PlayPhaseTransition(BossPhase newPhase)
    {
        if (newPhase == BossPhase.ArmsOnly)
            yield return FallDownRoutine();
        else
        {
            // CoreOnly 진입: 팔·몸통 게임오브젝트 제거
            if (_leftArm)  Destroy(_leftArm.gameObject);
            if (_rightArm) Destroy(_rightArm.gameObject);
            if (_body)     Destroy(_body.gameObject);
            yield return WaitOneSecond;
        }
    }

    /// <summary>
    /// 페이즈에 맞는 공격 루틴 실행.
    /// FullBody→발차기 / ArmsOnly→슬램(양팔) or 스핀(한팔) / CoreOnly→박치기
    /// </summary>
    public IEnumerator PlayAttack(BossPhase phase)
    {
        _attacking = true;
        _moving    = false;

        // 공격 시작 시점 기록 — 쿨다운 계산 기준점
        _lastAttackTime = Time.time;
        bool oneArmCut = AnyArmCut();
        _currentCooldown = phase switch
        {
            BossPhase.FullBody => _phaseHandler.LegsCut == 0 ? _kickCooldown : _stompCooldown,
            BossPhase.ArmsOnly => oneArmCut ? _spinCooldown : _slamCooldown,
            BossPhase.CoreOnly => _rollCooldown,
            _                  => _kickCooldown
        };

        if (phase == BossPhase.CoreOnly)
        {
            for (int i = 0; i < _rollCount; i++)
                yield return RollRoutine();
        }
        else
            yield return phase switch
            {
                BossPhase.FullBody => Phase1AttackRoutine(),
                BossPhase.ArmsOnly => oneArmCut ? ArmSwingRoutine() : ArmSlamRoutine(),
                _                  => null
            };

        _attacking = false;
    }

    // ── 라이프사이클 ──────────────────────────────────────────────────────

    void Awake()
    {
        _groundY      = transform.position.y;
        _phaseHandler = GetComponent<BossPhaseHandler>();

        _rangeIndicator = _rangeIndicator != null
            ? _rangeIndicator
            : GetComponent<BossRangeIndicator>() ?? gameObject.AddComponent<BossRangeIndicator>();

        if (_body)     { _bodyRest = _body.localRotation; _bodyRestPos = _body.localPosition; }
        if (_head)     _headRest  = _head.localRotation;
        if (_leftArm)  _lArmRest  = _leftArm.localRotation;
        if (_rightArm) _rArmRest  = _rightArm.localRotation;
        if (_leftLeg)  _lLegRest  = _leftLeg.localRotation;
        if (_rightLeg) _rLegRest  = _rightLeg.localRotation;
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

    // ── 이벤트 핸들러 ─────────────────────────────────────────────────────

    void OnBossDied(OnBossDiedEvent e)
    {
        // 팔이 아직 붙어있으면 직접 숨김 (BossLimb 계층구조와 무관하게 확실히 처리)
        if (_leftArm  && !_phaseHandler.LeftArmCut)  HideRenderers(_leftArm);
        if (_rightArm && !_phaseHandler.RightArmCut) HideRenderers(_rightArm);
        StartCoroutine(DeathFallRoutine());
    }

    static void HideRenderers(Transform root)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>())
            r.enabled = false;
    }

    void OnPhaseChanged(OnBossPhaseChangedEvent e)
    {
        _phase = e.phase;
        // FallDownRoutine은 BossController → PlayPhaseTransition을 통해 실행됨
    }

    // ── 메인 루프 ──────────────────────────────────────────────────────────

    void Update()
    {
        if (_attacking) return;

        if (_target != null)
        {
            if (_phase == BossPhase.CoreOnly)
                FaceHead();
            else
                FaceTarget();
        }

        if (_moving && _target != null)
        {
            float cycleSpeed = _phase switch
            {
                BossPhase.ArmsOnly => _armWalkCycleSpeed,
                BossPhase.CoreOnly => _floatSwaySpeed,
                _                  => _walkCycleSpeed
            };
            _cycle += cycleSpeed * Time.unscaledDeltaTime;
        }

        switch (_phase)
        {
            case BossPhase.FullBody: if (_moving) ApplyWalk();    else ApplyIdleBreath(); break;
            case BossPhase.ArmsOnly: if (_moving) ApplyArmWalk(); else ApplyIdleArms();   break;
            case BossPhase.CoreOnly: if (_moving) ApplyCrawl();   else ApplyIdleCore();   break;
        }
    }
}
