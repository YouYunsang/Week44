using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 절차적 애니메이션 — 애니메이션 사이클이 이동을 주도
///
/// Phase 1 공격 (거리 기반 선택):
///   근거리  → 발차기  (뒤로 당기기 → 뻥 → 복귀)
///   중거리  → 점프 밟기 (도약 → 다리 접기 → 내리찍기)
///
/// Phase 2 공격: 양팔 슬램
/// Phase 3 공격: 박치기 돌진
/// </summary>
public class BossProceduralAnimator : MonoBehaviour
{
    // ── 신체 파츠 ──────────────────────────────────────────────────────────
    [Header("Body Parts")]
    [SerializeField] Transform _body;
    [SerializeField] Transform _head;
    [SerializeField] Transform _leftArm;
    [SerializeField] Transform _rightArm;
    [SerializeField] Transform _leftLeg;
    [SerializeField] Transform _rightLeg;

    // ── 걷기 ───────────────────────────────────────────────────────────────
    [Header("Walk")]
    [SerializeField] float _walkCycleSpeed  = 0.45f;
    [SerializeField] float _walkPushPerStep = 0.9f;
    [SerializeField] float _legSwingAngle   = 45f;
    [SerializeField] float _armSwingAngle   = 22f;
    [SerializeField] float _walkBodyBob     = 0.07f;

    // ── 기어가기 ───────────────────────────────────────────────────────────
    [Header("Crawl")]
    [SerializeField] float _crawlCycleSpeed  = 0.28f;
    [SerializeField] float _crawlPushPerStep = 0.5f;
    [SerializeField] float _crawlRollAngle   = 28f;
    [SerializeField] float _crawlPitchAngle  = 32f;

    // ── 대기 ───────────────────────────────────────────────────────────────
    [Header("Idle")]
    [SerializeField] float _breathSpeed    = 1.0f;
    [SerializeField] float _breathAmount   = 0.03f;
    [SerializeField] float _armMenaceSpeed = 0.8f;
    [SerializeField] float _armMenaceAngle = 22f;

    // ── Phase1 공격: 발차기 ────────────────────────────────────────────────
    [Header("Attack — Kick")]
    [SerializeField] float _kickWindupAngle  = -55f;
    [SerializeField] float _kickSwingAngle   =  80f;
    [SerializeField] float _kickWindupTime   =  0.9f;  // 느린 예비동작
    [SerializeField] float _kickSwingTime    =  0.13f; // 킥 자체는 빠르게
    [SerializeField] float _kickReturnTime   =  0.7f;
    [SerializeField] float _kickForce        =  11f;
    [SerializeField] float _kickHitRadius    =  1.2f;
    [SerializeField] float _kickMaxRange     =  3.2f;  // 이 거리 이하면 발차기

    // ── Phase1 공격: 점프 밟기 ─────────────────────────────────────────────
    [Header("Attack — Jump Stomp")]
    [SerializeField] float _jumpHeight        =  2.8f;
    [SerializeField] float _jumpAscentTime    =  1.1f;  // 천천히 올라가기
    [SerializeField] float _jumpHangTime      =  0.5f;  // 정점 멈춤 길게
    [SerializeField] float _jumpDescentTime   =  0.22f; // 내려찍기는 빠르게
    [SerializeField] float _jumpLegTuckAngle  = -70f;
    [SerializeField] float _jumpStompAngle    =  60f;
    [SerializeField] float _stompForce        =  18f;
    [SerializeField] float _stompRadius       =  2.8f;  // 착지 충격 반경
    [SerializeField] float _stompMinRange     =  2.5f;  // 이 거리 이상이면 점프

    // ── Phase2 공격: 팔 슬램 ──────────────────────────────────────────────
    [Header("Attack — Arm Slam")]
    [SerializeField] float _slamRaiseAngle   = -70f;
    [SerializeField] float _slamDownAngle    =  85f;
    [SerializeField] float _slamRaiseTime    =  1.1f;   // 천천히 들기
    [SerializeField] float _slamDownTime     =  0.18f;  // 내려치기는 빠르게
    [SerializeField] float _slamReturnTime   =  0.7f;
    [SerializeField] float _slamForce        =  14f;
    [SerializeField] float _slamHitRadius    =  2.2f;

    // ── Phase3 공격: 박치기 ────────────────────────────────────────────────
    [Header("Attack — Headbutt")]
    [SerializeField] float _headWindupAngle   = -38f;
    [SerializeField] float _headLungeAngle    =  58f;
    [SerializeField] float _headWindupTime    =  1.2f;  // 천천히 뒤로
    [SerializeField] float _headLungeTime     =  0.2f;  // 박치기는 빠르게
    [SerializeField] float _headReturnTime    =  0.6f;
    [SerializeField] float _headbuttLungeDist =  1.6f;
    [SerializeField] float _headForce         =  16f;
    [SerializeField] float _headbuttHitRadius =  1.0f;

    // ── 팔 보행 (ArmsOnly) ─────────────────────────────────────────────────
    [Header("Arm Walk")]
    [SerializeField] float _armWalkCycleSpeed  = 0.35f;
    [SerializeField] float _armWalkPushPerStep = 0.7f;
    [SerializeField] float _armWalkSwingAngle  = 55f;
    [SerializeField] float _armWalkBob         = 0.05f;

    // ── 페이즈 전환 ────────────────────────────────────────────────────────
    [Header("Phase Transition — Fall")]
    [SerializeField] float _legHeight    = 1.0f;   // 다리 높이 = 낙하 거리
    [SerializeField] float _fallBodyPitch  = 28f;
    [SerializeField] float _fallTime       = 0.55f;
    [SerializeField] float _fallShakeMag   = 0.05f;
    [SerializeField] float _fallShakeDur   = 0.4f;

    // ── 범위 표시 ──────────────────────────────────────────────────────────
    [Header("Range Indicator")]
    [SerializeField] BossRangeIndicator _rangeIndicator;

    // 슬롯 상수
    const int SLOT_KICK  = 0;
    const int SLOT_STOMP = 1;
    const int SLOT_SLAM  = 2;
    const int SLOT_HEAD  = 3;

    // ── 공통 ───────────────────────────────────────────────────────────────
    [Header("Common")]
    [SerializeField] float _rotateSpeed = 5f;
    [SerializeField] float _returnSpeed = 9f;

    // ── 내부 상태 ──────────────────────────────────────────────────────────
    BossPhase  _phase = BossPhase.FullBody;
    int        _legsCut = 0;   // 잘린 다리 수 (0=두 개, 1=하나, 2=없음)
    Transform  _target;
    bool       _moving;
    bool       _attacking;
    float      _cycle;
    float      _groundY;   // 착지 기준 Y 위치

    Quaternion _bodyRest, _headRest;
    Quaternion _lArmRest, _rArmRest;
    Quaternion _lLegRest, _rLegRest;
    Vector3    _bodyRestPos;

    // ── 공개 API ──────────────────────────────────────────────────────────
    public void SetTarget(Transform t) => _target = t;
    public void SetMoving(bool m)      => _moving  = m;

    public IEnumerator PlayPhaseTransition(BossPhase newPhase)
    {
        // 낙하는 OnPhaseChanged에서 직접 StartCoroutine — 여기선 그 시간만 기다림
        float wait = newPhase == BossPhase.ArmsOnly
            ? _fallTime + _fallShakeDur + 0.2f
            : 1.0f;
        yield return new WaitForSeconds(wait);
    }

    public IEnumerator PlayAttack(BossPhase phase)
    {
        _attacking = true;
        _moving    = false;

        yield return phase switch
        {
            BossPhase.FullBody => Phase1AttackRoutine(),
            BossPhase.ArmsOnly => ArmSlamRoutine(),
            BossPhase.CoreOnly => HeadbuttRoutine(),
            _                  => null
        };

        _attacking = false;
    }

    // ── 초기화 ────────────────────────────────────────────────────────────
    void Awake()
    {
        _groundY = transform.position.y;

        if (_rangeIndicator == null)
            _rangeIndicator = GetComponent<BossRangeIndicator>();
        if (_rangeIndicator == null)
            _rangeIndicator = gameObject.AddComponent<BossRangeIndicator>();

        if (_body)     { _bodyRest = _body.localRotation; _bodyRestPos = _body.localPosition; }
        if (_head)     _headRest = _head.localRotation;
        if (_leftArm)  _lArmRest = _leftArm.localRotation;
        if (_rightArm) _rArmRest = _rightArm.localRotation;
        if (_leftLeg)  _lLegRest = _leftLeg.localRotation;
        if (_rightLeg) _rLegRest = _rightLeg.localRotation;
    }

    void OnEnable()
    {
        EventBus<OnBossPhaseChangedEvent>.Subscribe(OnPhaseChanged);
        EventBus<OnBossLimbSlicedEvent>.Subscribe(OnLimbSliced);
        EventBus<OnBossDiedEvent>.Subscribe(OnBossDied);
    }
    void OnDisable()
    {
        EventBus<OnBossPhaseChangedEvent>.Unsubscribe(OnPhaseChanged);
        EventBus<OnBossLimbSlicedEvent>.Unsubscribe(OnLimbSliced);
        EventBus<OnBossDiedEvent>.Unsubscribe(OnBossDied);
    }

    void OnBossDied(OnBossDiedEvent e) => StartCoroutine(DeathFallRoutine());

    void OnLimbSliced(OnBossLimbSlicedEvent e)
    {
        if (e.limb == LimbType.LeftLeg || e.limb == LimbType.RightLeg)
            _legsCut++;
    }

    void OnPhaseChanged(OnBossPhaseChangedEvent e)
    {
        _phase = e.phase;
        if (e.phase == BossPhase.ArmsOnly)
            StartCoroutine(FallDownRoutine());
    }

    // ── 메인 루프 ──────────────────────────────────────────────────────────
    void Update()
    {
        if (_attacking) return;

        if (_target != null) FaceTarget();

        if (_moving && _target != null)
        {
            float speed = _phase == BossPhase.CoreOnly ? _crawlCycleSpeed : _walkCycleSpeed;
            _cycle += speed * Time.deltaTime;
        }

        switch (_phase)
        {
            case BossPhase.FullBody:
                if (_moving) ApplyWalk();
                else         ApplyIdleBreath();
                break;
            case BossPhase.ArmsOnly:
                if (_moving) ApplyArmWalk();
                else         ApplyIdleArms();
                break;
            case BossPhase.CoreOnly:
                if (_moving) ApplyCrawl();
                else         ApplyIdleCore();
                break;
        }
    }

    // ── 걷기 ───────────────────────────────────────────────────────────────
    void ApplyWalk()
    {
        float sin = Mathf.Sin(_cycle * Mathf.PI * 2f);

        Rot(_leftLeg,  _lLegRest, Quaternion.Euler(0f, 0f,  sin * _legSwingAngle));
        Rot(_rightLeg, _rLegRest, Quaternion.Euler(0f, 0f, -sin * _legSwingAngle));
        Rot(_leftArm,  _lArmRest, Quaternion.Euler(0f, 0f, -sin * _armSwingAngle));
        Rot(_rightArm, _rArmRest, Quaternion.Euler(0f, 0f,  sin * _armSwingAngle));

        float bob = Mathf.Abs(sin) * _walkBodyBob;
        if (_body) _body.localPosition = Vector3.Lerp(
            _body.localPosition, _bodyRestPos + Vector3.up * bob,
            _returnSpeed * Time.deltaTime);

        float push = Mathf.Abs(sin) * _walkPushPerStep * _walkCycleSpeed * Time.deltaTime;
        PushTowardTarget(push);
    }

    // ── 기어가기 ───────────────────────────────────────────────────────────
    void ApplyCrawl()
    {
        float sin   = Mathf.Sin(_cycle * Mathf.PI * 2f);
        float pitch = Mathf.Sin(_cycle * Mathf.PI);

        if (_body) _body.localRotation = Quaternion.Slerp(
            _body.localRotation,
            _bodyRest * Quaternion.Euler(pitch * _crawlPitchAngle, 0f, sin * _crawlRollAngle),
            _returnSpeed * Time.deltaTime);

        if (_head) _head.localRotation = Quaternion.Slerp(
            _head.localRotation,
            _headRest * Quaternion.Euler(-pitch * _crawlPitchAngle * 0.4f, 0f, 0f),
            _returnSpeed * Time.deltaTime);

        float push = pitch * _crawlPushPerStep * _crawlCycleSpeed * Time.deltaTime;
        PushTowardTarget(push);
    }

    // ── 대기 ───────────────────────────────────────────────────────────────
    void ApplyIdleBreath()
    {
        float breath = Mathf.Sin(Time.time * _breathSpeed) * _breathAmount;
        if (_body) _body.localPosition = Vector3.Lerp(
            _body.localPosition, _bodyRestPos + Vector3.up * breath,
            _returnSpeed * Time.deltaTime);

        ReturnToRest(_leftLeg,  _lLegRest);
        ReturnToRest(_rightLeg, _rLegRest);
        ReturnToRest(_leftArm,  _lArmRest);
        ReturnToRest(_rightArm, _rArmRest);
    }

    // ── 팔 보행 ────────────────────────────────────────────────────────────
    void ApplyArmWalk()
    {
        float sin = Mathf.Sin(_cycle * Mathf.PI * 2f);

        // 팔을 다리처럼 교대 스윙
        Rot(_leftArm,  _lArmRest, Quaternion.Euler(0f, 0f,  sin * _armWalkSwingAngle));
        Rot(_rightArm, _rArmRest, Quaternion.Euler(0f, 0f, -sin * _armWalkSwingAngle));

        // 몸통 좌우 흔들림 (땅을 짚으며 이동하는 느낌)
        float bob = Mathf.Abs(sin) * _armWalkBob;
        if (_body) _body.localPosition = Vector3.Lerp(
            _body.localPosition, _bodyRestPos + Vector3.up * bob,
            _returnSpeed * Time.deltaTime);

        float push = Mathf.Abs(sin) * _armWalkPushPerStep * _armWalkCycleSpeed * Time.deltaTime;
        PushTowardTarget(push);
    }

    void ApplyIdleArms()
    {
        float t = Mathf.Sin(Time.time * _armMenaceSpeed);
        Rot(_leftArm,  _lArmRest, Quaternion.Euler( t * _armMenaceAngle, 0f, 0f));
        Rot(_rightArm, _rArmRest, Quaternion.Euler(-t * _armMenaceAngle, 0f, 0f));

        float breath = Mathf.Sin(Time.time * _breathSpeed) * _breathAmount;
        if (_body) _body.localPosition = Vector3.Lerp(
            _body.localPosition, _bodyRestPos + Vector3.up * breath,
            _returnSpeed * Time.deltaTime);
    }

    void ApplyIdleCore()
    {
        float sway = Mathf.Sin(Time.time * 0.8f);
        if (_head) _head.localRotation = Quaternion.Slerp(
            _head.localRotation,
            _headRest * Quaternion.Euler(0f, sway * 12f, sway * 4f),
            _returnSpeed * Time.deltaTime);
        ReturnToRest(_body, _bodyRest);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 공격 루틴
    // ══════════════════════════════════════════════════════════════════════

    // ── Phase1: 거리에 따라 발차기 / 점프 밟기 선택 ──────────────────────
    IEnumerator Phase1AttackRoutine()
    {
        // 다리 두 개 → 발차기 / 한 개 잘림 → 점프밟기
        if (_legsCut == 0)
            yield return KickRoutine();
        else
            yield return JumpStompRoutine();
    }

    // ── 발차기 ─────────────────────────────────────────────────────────────
    IEnumerator KickRoutine()
    {
        // 1. 예비동작
        Quaternion windupRot = _rLegRest * Quaternion.Euler(0f, 0f, _kickWindupAngle);
        yield return LerpLimb(_rightLeg, _rLegRest, windupRot, _kickWindupTime);

        // 2. 킥
        Quaternion kickRot = _rLegRest * Quaternion.Euler(0f, 0f, _kickSwingAngle);
        yield return LerpLimb(_rightLeg, windupRot, kickRot, _kickSwingTime,
            onMid: () => PublishHit(transform.forward, _kickForce,
                transform.position + transform.forward * 0.8f, _kickHitRadius));

        // 3. 복귀
        yield return LerpLimb(_rightLeg, kickRot, _rLegRest, _kickReturnTime);
    }

    // ── 점프 엉찍 ─────────────────────────────────────────────────────────
    IEnumerator JumpStompRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 peakPos  = startPos + Vector3.up * _jumpHeight;

        // 엉찍 자세 정의
        // 다리: Z로 위를 향해 접기 (양쪽 같은 방향 — 무릎이 앞/위로)
        Quaternion tuckL    = _lLegRest * Quaternion.Euler(0f, 0f,  _jumpLegTuckAngle);
        Quaternion tuckR    = _rLegRest * Quaternion.Euler(0f, 0f,  _jumpLegTuckAngle);
        // 몸통: 뒤로 크게 눕혀서 엉덩이가 아래 향하게
        Quaternion bodyBack = _bodyRest * Quaternion.Euler(-65f, 0f, 0f);

        // 1. 준비 — 살짝 웅크리기
        Quaternion crouchL = _lLegRest * Quaternion.Euler(0f, 0f, 15f);
        Quaternion crouchR = _rLegRest * Quaternion.Euler(0f, 0f, 15f);
        yield return LerpBothLegs(_lLegRest, crouchL, _rLegRest, crouchR, 0.2f);

        // 2. 상승 — 다리 접고 몸 뒤로 눕히기
        float e = 0f;
        while (e < _jumpAscentTime)
        {
            e += Time.deltaTime;
            float t  = Mathf.Clamp01(e / _jumpAscentTime);
            float et = 1f - (1f - t) * (1f - t);
            transform.position = Vector3.Lerp(startPos, peakPos, et);

            if (_leftLeg)  _leftLeg.localRotation  = Quaternion.Slerp(crouchL, tuckL, t);
            if (_rightLeg) _rightLeg.localRotation = Quaternion.Slerp(crouchR, tuckR, t);
            if (_body)     _body.localRotation      = Quaternion.Slerp(_bodyRest, bodyBack, t);
            yield return null;
        }
        transform.position = peakPos;

        // 3. 정점 대기 — 몸 뒤로 누운 채 조준
        Vector3 aimPos = _target != null ? _target.position : startPos;
        float   landY  = startPos.y - _legHeight;
        Vector3 targetLandPos = new Vector3(aimPos.x, landY, aimPos.z);

        yield return new WaitForSeconds(_jumpHangTime);

        // 4. 엉찍 하강 — 자세 유지하며 빠르게 낙하
        e = 0f;
        while (e < _jumpDescentTime)
        {
            e += Time.deltaTime;
            float t  = Mathf.Clamp01(e / _jumpDescentTime);
            float et = t * t;
            transform.position = Vector3.Lerp(peakPos, targetLandPos, et);
            yield return null;
        }

        // 5. 착지 충격
        transform.position = targetLandPos;
        PublishHit(Vector3.down, _stompForce, targetLandPos, _stompRadius);

        if (_body) StartCoroutine(ImpactSquash());

        // 6. 다리·몸통 자세 복귀
        Quaternion bodyAtLand = _body ? _body.localRotation : _bodyRest;
        e = 0f;
        while (e < 0.35f)
        {
            e += Time.deltaTime;
            float t = e / 0.35f;
            if (_leftLeg)  _leftLeg.localRotation  = Quaternion.Slerp(tuckL,      _lLegRest, t);
            if (_rightLeg) _rightLeg.localRotation = Quaternion.Slerp(tuckR,      _rLegRest, t);
            if (_body)     _body.localRotation      = Quaternion.Slerp(bodyAtLand, _bodyRest, t);
            yield return null;
        }
        if (_leftLeg)  _leftLeg.localRotation  = _lLegRest;
        if (_rightLeg) _rightLeg.localRotation = _rLegRest;
        if (_body)     _body.localRotation      = _bodyRest;

        // 7. 원래 높이로 복귀
        Vector3 riseStart = transform.position;
        Vector3 riseEnd   = new Vector3(riseStart.x, startPos.y, riseStart.z);
        e = 0f;
        while (e < 0.5f)
        {
            e += Time.deltaTime;
            float t  = e / 0.5f;
            float et = 1f - (1f - t) * (1f - t); // ease-out
            transform.position = Vector3.Lerp(riseStart, riseEnd, et);
            yield return null;
        }
        transform.position = riseEnd;
    }

    // 착지 충격 — 몸통이 아래로 찌그러졌다 복귀
    IEnumerator ImpactSquash()
    {
        Vector3 squashedPos = _bodyRestPos + Vector3.down * 0.15f;
        float e = 0f;
        while (e < 0.1f)
        {
            e += Time.deltaTime;
            _body.localPosition = Vector3.Lerp(_bodyRestPos, squashedPos, e / 0.1f);
            yield return null;
        }
        e = 0f;
        while (e < 0.3f)
        {
            e += Time.deltaTime;
            _body.localPosition = Vector3.Lerp(squashedPos, _bodyRestPos, e / 0.3f);
            yield return null;
        }
        _body.localPosition = _bodyRestPos;
    }

    // ── 페이즈 전환: 다리 잘림 → 쓰러짐 ─────────────────────────────────────
    IEnumerator FallDownRoutine()
    {
        Vector3    startPos  = transform.position;
        Vector3    landPos   = new Vector3(startPos.x, startPos.y - _legHeight, startPos.z);
        Quaternion bodyTilt  = _bodyRest * Quaternion.Euler(_fallBodyPitch, 0f, 0f);
        Quaternion buckleL   = _lLegRest * Quaternion.Euler(0f, 0f, 60f);
        Quaternion buckleR   = _rLegRest * Quaternion.Euler(0f, 0f, 60f);

        float e = 0f;
        while (e < _fallTime)
        {
            e += Time.deltaTime;
            float t  = Mathf.Clamp01(e / _fallTime);
            float et = t * t; // ease-in — 중력처럼 가속

            if (_leftLeg)  _leftLeg.localRotation  = Quaternion.Slerp(_lLegRest, buckleL, t);
            if (_rightLeg) _rightLeg.localRotation = Quaternion.Slerp(_rLegRest, buckleR, t);
            if (_body)     _body.localRotation     = Quaternion.Slerp(_bodyRest, bodyTilt, et);

            transform.position = Vector3.Lerp(startPos, landPos, et);
            yield return null;
        }
        transform.position = landPos;
        _groundY = landPos.y; // 이후 점프 착지 기준도 갱신

        // 착지 충격 진동
        yield return LandShake();
    }

    IEnumerator LandShake()
    {
        float e = 0f;
        while (e < _fallShakeDur)
        {
            e += Time.deltaTime;
            float fade = 1f - e / _fallShakeDur;
            if (_body) _body.localPosition = _bodyRestPos + UnityEngine.Random.insideUnitSphere * _fallShakeMag * fade;
            yield return null;
        }
        if (_body) _body.localPosition = _bodyRestPos;
    }

    IEnumerator DeathFallRoutine()
    {
        _attacking = true;
        _moving    = false;

        Vector3    startPos = transform.position;
        Vector3    landPos  = new Vector3(startPos.x, startPos.y - _legHeight * 0.5f, startPos.z);
        Quaternion bodyFlop = _bodyRest * Quaternion.Euler(_fallBodyPitch * 1.5f, 0f, 0f);

        float e = 0f;
        while (e < _fallTime * 1.2f)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / (_fallTime * 1.2f));
            if (_body) _body.localRotation = Quaternion.Slerp(_bodyRest, bodyFlop, t * t);
            if (_head) _head.localRotation = Quaternion.Slerp(_headRest,
                _headRest * Quaternion.Euler(30f, 0f, 0f), t);
            transform.position = Vector3.Lerp(startPos, landPos, t * t);
            yield return null;
        }

        yield return LandShake();
    }

    // ── Phase2: 팔 슬램 ────────────────────────────────────────────────────
    IEnumerator ArmSlamRoutine()
    {
        Quaternion raiseL = _lArmRest * Quaternion.Euler(_slamRaiseAngle, 0f, 0f);
        Quaternion raiseR = _rArmRest * Quaternion.Euler(_slamRaiseAngle, 0f, 0f);
        Quaternion downL  = _lArmRest * Quaternion.Euler(_slamDownAngle,  0f, 0f);
        Quaternion downR  = _rArmRest * Quaternion.Euler(_slamDownAngle,  0f, 0f);

        _rangeIndicator?.Show(SLOT_SLAM,
            transform.position + transform.forward * 1.0f, _slamHitRadius,
            new Color(1f, 0.1f, 0.1f, 0.9f));

        yield return LerpBothArms(_lArmRest, raiseL, _rArmRest, raiseR, _slamRaiseTime);
        yield return LerpBothArms(raiseL, downL, raiseR, downR, _slamDownTime,
            onMid: () => PublishHit(transform.forward, _slamForce,
                transform.position + transform.forward * 1.0f, _slamHitRadius));

        _rangeIndicator?.Hide(SLOT_SLAM);
        yield return LerpBothArms(downL, _lArmRest, downR, _rArmRest, _slamReturnTime);
    }

    // ── Phase3: 박치기 돌진 ────────────────────────────────────────────────
    IEnumerator HeadbuttRoutine()
    {
        Quaternion windupRot = _headRest * Quaternion.Euler(_headWindupAngle, 0f, 0f);
        Quaternion lungeRot  = _headRest * Quaternion.Euler(_headLungeAngle,  0f, 0f);

        _rangeIndicator?.Show(SLOT_HEAD,
            transform.position + transform.forward * _headbuttLungeDist, _headbuttHitRadius,
            new Color(0.7f, 0.2f, 1f, 0.9f));

        yield return LerpLimb(_head, _headRest, windupRot, _headWindupTime);

        Vector3 lungeDir = _target != null
            ? (_target.position - transform.position).normalized
            : transform.forward;
        lungeDir.y = 0f;

        float e = 0f;
        bool hit = false;
        while (e < _headLungeTime)
        {
            e += Time.deltaTime;
            float t = e / _headLungeTime;
            if (_head) _head.localRotation = Quaternion.Slerp(windupRot, lungeRot, t);
            transform.position += lungeDir * (_headbuttLungeDist / _headLungeTime) * Time.deltaTime;

            if (!hit && t >= 0.5f)
            {
                hit = true;
                PublishHit(lungeDir, _headForce, transform.position, _headbuttHitRadius);
            }
            yield return null;
        }

        _rangeIndicator?.Hide(SLOT_HEAD);
        yield return LerpLimb(_head, lungeRot, _headRest, _headReturnTime);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 헬퍼
    // ══════════════════════════════════════════════════════════════════════

    float FindFloorY(Vector3 origin)
    {
        // 위에서 아래로 레이를 쏴 실제 바닥 Y를 구함
        Ray ray = new Ray(origin + Vector3.up * 5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            return hit.point.y;
        return _groundY; // 바닥 못 찾으면 초기값 사용
    }

    void FaceTarget()
    {
        Vector3 dir = (_target.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation, Quaternion.LookRotation(dir),
            _rotateSpeed * Time.deltaTime);
    }

    void PushTowardTarget(float amount)
    {
        if (_target == null) return;
        Vector3 dir = (_target.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.position += dir.normalized * amount;
    }

    void PublishHit(Vector3 dir, float force, Vector3 hitCenter, float radius)
    {
        EventBus<OnBossAttackHitEvent>.Publish(new OnBossAttackHitEvent { direction = dir, force = force });
    }

    void Rot(Transform t, Quaternion rest, Quaternion delta)
    {
        if (t == null) return;
        t.localRotation = Quaternion.Slerp(t.localRotation, rest * delta, _returnSpeed * Time.deltaTime);
    }

    void ReturnToRest(Transform t, Quaternion rest)
    {
        if (t == null) return;
        t.localRotation = Quaternion.Slerp(t.localRotation, rest, _returnSpeed * Time.deltaTime);
    }

    IEnumerator LerpLimb(Transform limb, Quaternion from, Quaternion to,
        float duration, Action onMid = null)
    {
        float e = 0f;
        bool midFired = false;
        while (e < duration)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / duration);
            if (limb) limb.localRotation = Quaternion.Slerp(from, to, t);
            if (!midFired && t >= 0.5f) { midFired = true; onMid?.Invoke(); }
            yield return null;
        }
        if (limb) limb.localRotation = to;
    }

    IEnumerator LerpBothLegs(Quaternion fromL, Quaternion toL,
        Quaternion fromR, Quaternion toR, float duration, Action onMid = null)
    {
        float e = 0f;
        bool midFired = false;
        while (e < duration)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / duration);
            if (_leftLeg)  _leftLeg.localRotation  = Quaternion.Slerp(fromL, toL, t);
            if (_rightLeg) _rightLeg.localRotation = Quaternion.Slerp(fromR, toR, t);
            if (!midFired && t >= 0.5f) { midFired = true; onMid?.Invoke(); }
            yield return null;
        }
        if (_leftLeg)  _leftLeg.localRotation  = toL;
        if (_rightLeg) _rightLeg.localRotation = toR;
    }

    IEnumerator LerpBothArms(Quaternion fromL, Quaternion toL,
        Quaternion fromR, Quaternion toR, float duration, Action onMid = null)
    {
        float e = 0f;
        bool midFired = false;
        while (e < duration)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / duration);
            if (_leftArm)  _leftArm.localRotation  = Quaternion.Slerp(fromL, toL, t);
            if (_rightArm) _rightArm.localRotation = Quaternion.Slerp(fromR, toR, t);
            if (!midFired && t >= 0.5f) { midFired = true; onMid?.Invoke(); }
            yield return null;
        }
        if (_leftArm)  _leftArm.localRotation  = toL;
        if (_rightArm) _rightArm.localRotation = toR;
    }

    // ── 공격 범위 Gizmo ────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Vector3 origin  = transform.position;
        Vector3 forward = transform.forward;

        // Phase1 — 발차기 범위 (주황)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(origin + forward * _kickMaxRange, _kickHitRadius);
        Gizmos.DrawLine(origin, origin + forward * _kickMaxRange);

        // Phase1 — 점프 밟기 착지 범위 (노랑)
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(origin + forward * _stompMinRange, _stompRadius);
        Gizmos.DrawLine(origin, origin + forward * _stompMinRange);

        // Phase2 — 팔 슬램 범위 (빨강)
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.4f);
        if (_leftArm)  Gizmos.DrawWireSphere(_leftArm.position,  _slamHitRadius);
        if (_rightArm) Gizmos.DrawWireSphere(_rightArm.position, _slamHitRadius);

        // Phase3 — 박치기 돌진 범위 (보라)
        Gizmos.color = new Color(0.7f, 0.2f, 1f, 0.4f);
        Gizmos.DrawWireSphere(origin + forward * _headbuttLungeDist, _headbuttHitRadius);
        Gizmos.DrawLine(origin, origin + forward * _headbuttLungeDist);
    }
}
