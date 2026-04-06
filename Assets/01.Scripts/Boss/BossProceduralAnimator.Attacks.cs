using System.Collections;
using UnityEngine;

public partial class BossProceduralAnimator
{
    // ══════════════════════════════════════════════════════════════════════════
    // Phase 1 — 진입점: 다리 절단 여부에 따라 킥 or 점프 스톰프 선택
    // ══════════════════════════════════════════════════════════════════════════
    IEnumerator Phase1AttackRoutine()
    {
        if (_phaseHandler.LegsCut == 0)
            yield return KickRoutine();      // 다리 멀쩡 → 발차기
        else
            yield return JumpStompRoutine(); // 다리 하나라도 없으면 → 점프 밟기
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Phase 1 — 발차기 (FullBody)
    //   1) 예비동작: 오른 다리를 뒤로 당김
    //   2) 스윙: 앞으로 크게 휘두름 (180도 초과 → LerpLimbAngle)
    //   3) 복귀: 다리를 rest로 되돌림
    // ══════════════════════════════════════════════════════════════════════════
    IEnumerator KickRoutine()
    {
        // 판정 범위를 인디케이터로 미리 표시
        Vector3 fwdN   = FlatForward();
        Vector3 origin = GroundPos() + LocalOffset(fwdN, _kickOffset);
        _rangeIndicator?.ShowRect(SLOT_KICK, origin, fwdN, _kickSize, new Color(1f, 0.55f, 0f, 0.9f));

        // 1. 예비동작: 팔은 rest로 복귀, 오른 다리는 windup 각도로 이동 (동시 실행)
        Quaternion curL = _leftLeg  ? _leftLeg.localRotation  : _lLegRest;
        Quaternion curR = _rightLeg ? _rightLeg.localRotation : _rLegRest;
        Quaternion windupRot = _rLegRest * Quaternion.Euler(0f, 0f, _kickWindupAngle);
        StartCoroutine(LerpBothLimbs(
            _leftArm,  _leftArm  ? _leftArm.localRotation  : _lArmRest, _lArmRest,
            _rightArm, _rightArm ? _rightArm.localRotation : _rArmRest, _rArmRest,
            _kickWindupTime));
        yield return LerpBothLimbs(_leftLeg, curL, _lLegRest, _rightLeg, curR, windupRot, _kickWindupTime);

        // 2. 스윙: Slerp 최단경로 문제를 피하기 위해 float 각도 직접 보간
        //    onMid(t=0.5) 시점에 판정 발행
        yield return LerpLimbAngle(_rightLeg, _rLegRest,
            _kickWindupAngle, _kickSwingAngle, Vector3.forward, _kickSwingTime,
            onMid: () => {
                EventBus<OnBossAttackEvent>.Publish(new OnBossAttackEvent { attackType = BossAttackType.Kick });
                PublishHitRect(fwdN, _kickForce, GroundPos() + LocalOffset(fwdN, _kickOffset), fwdN, _kickSize);
            });

        // 3. 복귀
        _rangeIndicator?.Hide(SLOT_KICK);
        yield return LerpLimbAngle(_rightLeg, _rLegRest,
            _kickSwingAngle, 0f, Vector3.forward, _kickReturnTime);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Phase 1b — 점프 스톰프 (FullBody, 다리 하나 이상 절단)
    //   1) 공중으로 도약 + 다리 접힘
    //   2) 플레이어 위치로 낙하
    //   3) 착지 충격 판정
    // ══════════════════════════════════════════════════════════════════════════
    IEnumerator JumpStompRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 peakPos  = startPos + Vector3.up * _jumpHeight;

        // 1. 웅크리기 (도약 예비 동작)
        const float CrouchAngle = 15f;
        yield return LerpBothLimbsAngle(
            _leftLeg,  _lLegRest, 0f, CrouchAngle,
            _rightLeg, _rLegRest, 0f, CrouchAngle,
            Vector3.forward, 0.2f);

        // 2. 상승: ease-out으로 정점까지 도달, 다리 접어 올림
        float e = 0f;
        while (e < _jumpAscentTime)
        {
            e += Time.deltaTime;
            float t  = Mathf.Clamp01(e / _jumpAscentTime);
            float et = 1f - (1f - t) * (1f - t); // ease-out
            transform.position = Vector3.Lerp(startPos, peakPos, et);
            float legAngle = Mathf.Lerp(CrouchAngle, _jumpLegTuckAngle, t);
            if (_leftLeg)  _leftLeg.localRotation  = _lLegRest * Quaternion.AngleAxis(legAngle, Vector3.forward);
            if (_rightLeg) _rightLeg.localRotation = _rLegRest * Quaternion.AngleAxis(legAngle, Vector3.forward);
            if (_body)     _body.localRotation      = _bodyRest * Quaternion.AngleAxis(Mathf.Lerp(0f, -65f, t), Vector3.right);
            yield return null;
        }
        transform.position = peakPos;

        // 3. 정점 체공 — 플레이어 위치 조준 + 인디케이터 표시
        Vector3 aimPos    = _target != null ? _target.position : startPos;
        Vector3 landPos   = new Vector3(aimPos.x, startPos.y - _legHeight, aimPos.z);
        Vector3 fwdN      = FlatForward();
        float   indFloorY = FindFloorY(aimPos);
        Vector3 stompCtr  = new Vector3(aimPos.x, indFloorY, aimPos.z) + LocalOffset(fwdN, _stompOffset);
        Vector3 stompOrig = stompCtr - fwdN * (_stompSize.z * 0.5f);
        _rangeIndicator?.ShowRect(SLOT_STOMP, stompOrig, fwdN, _stompSize, new Color(1f, 0.85f, 0f, 0.9f));

        yield return new WaitForSeconds(_jumpHangTime);

        // 4. 낙하: ease-in으로 빠르게 내려옴
        e = 0f;
        while (e < _jumpDescentTime)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / _jumpDescentTime);
            transform.position = Vector3.Lerp(peakPos, landPos, t * t); // ease-in
            yield return null;
        }

        // 5. 착지 + 판정
        transform.position = landPos;
        _rangeIndicator?.Hide(SLOT_STOMP);
        EventBus<OnBossAttackEvent>.Publish(new OnBossAttackEvent { attackType = BossAttackType.JumpStomp });
        PublishHitRect(Vector3.down, _stompForce, stompOrig, fwdN, _stompSize);
        if (_body) StartCoroutine(ImpactSquash());

        // 6. 자세 복귀 (다리 펴기 + 바디 정렬)
        Quaternion bodyAtLand = _body ? _body.localRotation : _bodyRest;
        e = 0f;
        while (e < 0.35f)
        {
            e += Time.deltaTime;
            float t        = Mathf.Clamp01(e / 0.35f);
            float legAngle = Mathf.Lerp(_jumpLegTuckAngle, 0f, t);
            if (_leftLeg)  _leftLeg.localRotation  = _lLegRest * Quaternion.AngleAxis(legAngle, Vector3.forward);
            if (_rightLeg) _rightLeg.localRotation = _rLegRest * Quaternion.AngleAxis(legAngle, Vector3.forward);
            if (_body)     _body.localRotation      = Quaternion.Slerp(bodyAtLand, _bodyRest, t);
            yield return null;
        }
        if (_leftLeg)  _leftLeg.localRotation  = _lLegRest;
        if (_rightLeg) _rightLeg.localRotation = _rLegRest;
        if (_body)     _body.localRotation      = _bodyRest;

        // 7. 원래 높이 복귀 — landPos.y = startPos.y - _legHeight이므로
        //    복귀하지 않으면 스톰프할 때마다 보스가 영구적으로 낮아짐
        Vector3 riseStart = transform.position;
        Vector3 riseEnd   = new Vector3(riseStart.x, startPos.y, riseStart.z);
        e = 0f;
        while (e < 0.5f)
        {
            e += Time.deltaTime;
            float t  = Mathf.Clamp01(e / 0.5f);
            float et = 1f - (1f - t) * (1f - t); // ease-out
            transform.position = Vector3.Lerp(riseStart, riseEnd, et);
            yield return null;
        }
        transform.position = riseEnd;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Phase 2a — 팔 스핀 (ArmsOnly, 팔 하나 남았을 때)
    //   1) 남은 팔을 대각으로 들어 올림 (windup)
    //   2) 몸 전체를 Y축 기준 360도 회전 — 팔이 쓸고 지나가는 느낌
    //   3) 팔 복귀
    //
    //   인디케이터: SLOT_SPIN 전용 — SLOT_SLAM(rect)과 positionCount가 달라 충돌하므로 분리
    // ══════════════════════════════════════════════════════════════════════════
    IEnumerator ArmSwingRoutine()
    {
        // ── 남은 팔 결정 ──────────────────────────────────────────────────────
        // 왼팔 절단 여부로 판단: 왼팔이 잘렸으면 오른팔(남은 팔)로 스핀
        bool leftCut  = _phaseHandler.LeftArmCut || IsArmCut(_leftArm);
        bool useRight = leftCut;
        Transform  arm     = useRight ? _rightArm : _leftArm;
        Quaternion armRest = useRight ? _rArmRest : _lArmRest;
        // 레퍼런스가 null이면 반대 팔로 fallback
        if (arm == null) { arm = useRight ? _leftArm : _rightArm; armRest = useRight ? _lArmRest : _rArmRest; }

        // ── 원형 인디케이터 ───────────────────────────────────────────────────
        // _groundY = FallDownRoutine 이후 보스 루트 Y (시각적 바닥보다 _legHeight만큼 아래)
        // → _groundY + _legHeight = 낙하 전 원래 Y = 실제 바닥 높이
        // FindFloorY는 _groundLayer=~0일 때 보스 자신의 콜라이더에 막히므로 사용하지 않음
        float   floorY = _groundY + _legHeight;
        Vector3 indCtr = new Vector3(transform.position.x, floorY, transform.position.z);
        _rangeIndicator?.ShowCircle(SLOT_SPIN, indCtr, _spinRadius, new Color(1f, 0.5f, 0f, 0.9f));

        // ── 1. 팔 들기 (windup) — X축 회전으로 뻗기
        //    오른팔은 왼팔과 반대 방향(각도 반전)으로 들어올림
        float raiseAngle = useRight ? _spinRaiseAngle : -_spinRaiseAngle;
        Quaternion raiseRot = armRest * Quaternion.Euler(raiseAngle, 0f, 0f);
        Quaternion curRot   = arm ? arm.localRotation : armRest;
        yield return LerpLimb(arm, curRot, raiseRot, _spinWindupTime);

        // ── 2. 몸 전체 360도 스핀 ─────────────────────────────────────────
        Quaternion spinStartRot = transform.rotation;
        float e   = 0f;
        bool  hit = false;
        while (e < _spinDuration)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / _spinDuration);

            // 월드 Y축 기준 360도 회전 (float 보간으로 의도한 방향 보장)
            transform.rotation = Quaternion.AngleAxis(360f * t, Vector3.up) * spinStartRot;

            // 팔은 들어올린 포즈 고정 (로컬 기준이므로 몸 회전과 함께 쓸려감)
            if (arm) arm.localRotation = raiseRot;

            // 인디케이터 XZ 위치를 보스와 함께 갱신 (Y는 바닥 고정)
            indCtr.x = transform.position.x;
            indCtr.z = transform.position.z;
            _rangeIndicator?.ShowCircle(SLOT_SPIN, indCtr, _spinRadius, new Color(1f, 0.5f, 0f, 0.9f));

            // 반지름 내 플레이어 판정 (한 번만)
            if (!hit && _target != null)
            {
                float dx = _target.position.x - transform.position.x;
                float dz = _target.position.z - transform.position.z;
                if (dx * dx + dz * dz <= _spinRadius * _spinRadius)
                {
                    hit = true;
                    EventBus<OnBossAttackEvent>.Publish(new OnBossAttackEvent { attackType = BossAttackType.ArmSwing });
                    Vector3 hitDir = new Vector3(dx, 0f, dz).normalized;
                    if (hitDir.sqrMagnitude < 0.01f) hitDir = FlatForward();
                    EventBus<OnBossAttackHitEvent>.Publish(new OnBossAttackHitEvent
                    {
                        direction = hitDir,
                        force     = _spinForce
                    });
                }
            }
            yield return null;
        }

        // 스핀 완료 후 방향 정렬 (Quaternion.AngleAxis(360) ≈ identity 이므로 원 방향)
        transform.rotation = spinStartRot;
        _rangeIndicator?.Hide(SLOT_SPIN);

        // ── 3. 팔 복귀 ────────────────────────────────────────────────────
        yield return LerpLimb(arm, raiseRot, armRest, _spinReturnTime);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Phase 2b — 팔 슬램 (ArmsOnly, 양팔 모두 있을 때)
    //   1) 인디케이터 표시 후 양팔을 들고 점프
    //   2) 플레이어 위치로 하강하며 팔 내리찍기
    //   3) 착지 충격 후 팔 복귀
    // ══════════════════════════════════════════════════════════════════════════
    IEnumerator ArmSlamRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 peakPos  = startPos + Vector3.up * _slamJumpHeight;
        Vector3 fwdN     = FlatForward();

        // 착지 목표: 플레이어 XZ + _groundY(현재 루트 높이) 유지
        // FindFloorY는 인디케이터 시각 위치에만 사용 (보스 루트 높이와 분리)
        Vector3 aimPos    = _target != null ? _target.position : startPos;
        Vector3 landPos   = new Vector3(aimPos.x, _groundY, aimPos.z);
        float   indFloorY = FindFloorY(aimPos);

        // 인디케이터: 실제 바닥 Y 기준
        Vector3 indCenter = new Vector3(aimPos.x, indFloorY, aimPos.z) + LocalOffset(fwdN, _slamOffset);
        Vector3 slamOrig  = indCenter - fwdN * (_slamSize.z * 0.5f);
        _rangeIndicator?.ShowRect(SLOT_SLAM, slamOrig, fwdN, _slamSize, new Color(1f, 0.1f, 0.1f, 0.9f));

        // 1. 상승 + 팔 들기 (ease-out)
        float e = 0f;
        while (e < _slamRaiseTime)
        {
            e += Time.deltaTime;
            float t        = Mathf.Clamp01(e / _slamRaiseTime);
            float et       = 1f - (1f - t) * (1f - t); // ease-out
            float armAngle = Mathf.Lerp(0f, _slamRaiseAngle, t);
            transform.position = Vector3.Lerp(startPos, peakPos, et);
            if (_leftArm)  _leftArm.localRotation  = _lArmRest * Quaternion.AngleAxis(armAngle, Vector3.forward);
            if (_rightArm) _rightArm.localRotation = _rArmRest * Quaternion.AngleAxis(armAngle, Vector3.forward);
            yield return null;
        }
        transform.position = peakPos;

        yield return new WaitForSeconds(0.1f); // 정점 체공 — 플레이어가 위치를 인식할 시간

        // 2. 하강 + 팔 내리찍기 (ease-in)
        e = 0f;
        bool hit = false;
        while (e < _slamDownTime)
        {
            e += Time.deltaTime;
            float t        = Mathf.Clamp01(e / _slamDownTime);
            float armAngle = Mathf.Lerp(_slamRaiseAngle, _slamDownAngle, t);
            transform.position = Vector3.Lerp(peakPos, landPos, t * t); // ease-in
            if (_leftArm)  _leftArm.localRotation  = _lArmRest * Quaternion.AngleAxis(armAngle, Vector3.forward);
            if (_rightArm) _rightArm.localRotation = _rArmRest * Quaternion.AngleAxis(armAngle, Vector3.forward);

            // 하강 70% 지점에서 판정 발행
            if (!hit && t >= 0.7f)
            {
                hit = true;
                EventBus<OnBossAttackEvent>.Publish(new OnBossAttackEvent { attackType = BossAttackType.ArmSlam });
                PublishHitRect(fwdN, _slamForce, slamOrig, fwdN, _slamSize);
            }
            yield return null;
        }

        // 3. 착지 고정 후 충격 연출
        transform.position = landPos;
        _rangeIndicator?.Hide(SLOT_SLAM);
        if (_body) StartCoroutine(ImpactSquash());

        // 4. 팔 복귀 (LerpBothLimbsAngle: 180도 초과 회전 보장)
        yield return LerpBothLimbsAngle(
            _leftArm,  _lArmRest, _slamDownAngle, 0f,
            _rightArm, _rArmRest, _slamDownAngle, 0f,
            Vector3.forward, _slamReturnTime);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Phase 3 — 구르기 돌진 (CoreOnly)
    //   방향 결정 → 인디케이터 → 즉시 고속 돌진 + 회전
    // ══════════════════════════════════════════════════════════════════════════
    IEnumerator RollRoutine()
    {
        if (_head == null) yield break;

        // 1. 방향 결정
        Vector3 headPos = _head.position;
        Vector3 toFlat  = _target != null
            ? new Vector3(_target.position.x - headPos.x, 0f,
                          _target.position.z - headPos.z)
            : Vector3.zero;
        Vector3 rollDir = toFlat.sqrMagnitude > 0.01f ? toFlat.normalized : FlatForward();

        // 2. 인디케이터 표시 후 대기
        float   rollDist = _rollSpeed * _rollDuration;
        Vector3 rollOrig = new Vector3(headPos.x, 0f, headPos.z);
        _rangeIndicator?.ShowRect(SLOT_HEAD, rollOrig, rollDir,
            new Vector3(_rollHitRadius * 2f, 1f, rollDist), new Color(0.7f, 0.2f, 1f, 0.9f));
        yield return new WaitForSeconds(_rollPauseBetween);

        // 3. 돌진
        Vector3    rollAxis     = Vector3.Cross(rollDir, Vector3.up);
        Quaternion headStartRot = _head.rotation;
        float rollAngle = 0f;
        bool  hit       = false;
        float e         = 0f;
        while (e < _rollDuration)
        {
            e += Time.deltaTime;

            _head.position = new Vector3(
                _head.position.x + rollDir.x * _rollSpeed * Time.deltaTime,
                _rollHeadY,
                _head.position.z + rollDir.z * _rollSpeed * Time.deltaTime);

            rollAngle += _rollRotSpeed * Time.deltaTime;
            _head.rotation = Quaternion.AngleAxis(rollAngle, rollAxis) * headStartRot;

            if (!hit && _target != null)
            {
                float dx = _target.position.x - _head.position.x;
                float dz = _target.position.z - _head.position.z;
                if (dx * dx + dz * dz <= _rollHitRadius * _rollHitRadius)
                {
                    hit = true;
                    EventBus<OnBossAttackEvent>.Publish(new OnBossAttackEvent { attackType = BossAttackType.Roll });
                    Vector3 hitDir = new Vector3(dx, 0f, dz).normalized;
                    if (hitDir.sqrMagnitude < 0.01f) hitDir = rollDir;
                    EventBus<OnBossAttackHitEvent>.Publish(new OnBossAttackHitEvent
                    {
                        direction = hitDir,
                        force     = _rollForce
                    });
                }
            }
            yield return null;
        }

        _rangeIndicator?.Hide(SLOT_HEAD);
        _head.localRotation = _headRest;
    }
}
