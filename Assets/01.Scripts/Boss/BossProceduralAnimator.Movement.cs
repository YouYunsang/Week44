using UnityEngine;

public partial class BossProceduralAnimator
{
    // ── 걷기 (FullBody) ────────────────────────────────────────────────────
    void ApplyWalk()
    {
        float sin = Mathf.Sin(_cycle * Mathf.PI * 2f);

        Rot(_leftLeg,  _lLegRest, Quaternion.Euler(0f, 0f,  sin * _legSwingAngle));
        Rot(_rightLeg, _rLegRest, Quaternion.Euler(0f, 0f, -sin * _legSwingAngle));
        Rot(_leftArm,  _lArmRest, Quaternion.Euler(0f, 0f, -sin * _armSwingAngle));
        Rot(_rightArm, _rArmRest, Quaternion.Euler(0f, 0f,  sin * _armSwingAngle));

        if (_body) _body.localPosition = Vector3.Lerp(
            _body.localPosition,
            _bodyRestPos + Vector3.up * (Mathf.Abs(sin) * _walkBodyBob),
            _returnSpeed * Time.deltaTime);

        PushTowardTarget(Mathf.Abs(sin) * _walkPushPerStep * _walkCycleSpeed * Time.deltaTime);
    }

    // ── 팔 보행 (ArmsOnly) ─────────────────────────────────────────────────
    void ApplyArmWalk()
    {
        float sin = Mathf.Sin(_cycle * Mathf.PI * 2f);

        // 양팔 동일 방향으로 스윙 (교차 보행 아님)
        Rot(_leftArm,  _lArmRest, Quaternion.Euler(0f, 0f, sin * _armWalkSwingAngle));
        Rot(_rightArm, _rArmRest, Quaternion.Euler(0f, 0f, sin * _armWalkSwingAngle));

        if (_body) _body.localPosition = Vector3.Lerp(
            _body.localPosition,
            _bodyRestPos + Vector3.up * (Mathf.Abs(sin) * _armWalkBob),
            _returnSpeed * Time.deltaTime);

        PushTowardTarget(Mathf.Abs(sin) * _armWalkPushPerStep * _armWalkCycleSpeed * Time.deltaTime);
    }

    // ── 부유 이동 (CoreOnly) — 머리가 공중에 떠서 날아다니는 느낌 ─────────
    void ApplyCrawl()
    {
        float t   = Time.time;
        float sway = Mathf.Sin(_cycle * Mathf.PI * 2f);

        // 1. 호버 Y: 지면 위 일정 높이 + 상하 둥실
        float targetY = _groundY + _hoverHeight + Mathf.Sin(t * _hoverBobSpeed) * _hoverBobAmount;
        transform.position = new Vector3(
            transform.position.x,
            Mathf.Lerp(transform.position.y, targetY, _returnSpeed * Time.deltaTime),
            transform.position.z);

        // 2. 바디: 전진 방향으로 앞으로 기울고 좌우로 흔들림
        if (_body) _body.localRotation = Quaternion.Slerp(
            _body.localRotation,
            _bodyRest * Quaternion.Euler(-_floatTiltAngle, 0f, sway * _floatRollAngle),
            _returnSpeed * Time.deltaTime);

        // 3. 머리: 바디 기울기를 살짝 상쇄해 항상 앞을 향함
        if (_head) _head.localRotation = Quaternion.Slerp(
            _head.localRotation,
            _headRest * Quaternion.Euler(_floatTiltAngle * 0.35f, 0f, -sway * 6f),
            _returnSpeed * Time.deltaTime);

        // 4. 수평 이동: 사이클 없이 일정 속도로 접근
        PushTowardTarget(_floatSpeed * Time.deltaTime);
    }

    // ── 대기 루틴 ──────────────────────────────────────────────────────────

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

    void ApplyIdleArms()
    {
        float t = Mathf.Sin(Time.time * _armMenaceSpeed);
        Rot(_leftArm,  _lArmRest, Quaternion.Euler( t * _armMenaceAngle, 0f, 0f));
        Rot(_rightArm, _rArmRest, Quaternion.Euler(-t * _armMenaceAngle, 0f, 0f));

        if (_body) _body.localPosition = Vector3.Lerp(
            _body.localPosition,
            _bodyRestPos + Vector3.up * (Mathf.Sin(Time.time * _breathSpeed) * _breathAmount),
            _returnSpeed * Time.deltaTime);
    }

    void ApplyIdleCore()
    {
        float t = Time.time;

        // 1. 호버 Y: 이동과 동일한 둥실 로직
        float targetY = _groundY + _hoverHeight + Mathf.Sin(t * _hoverBobSpeed) * _hoverBobAmount;
        transform.position = new Vector3(
            transform.position.x,
            Mathf.Lerp(transform.position.y, targetY, _returnSpeed * Time.deltaTime),
            transform.position.z);

        // 2. 바디: rest로 서서히 복귀
        ReturnToRest(_body, _bodyRest);

        // 3. 머리: 위협적인 좌우·끄덕 복합 흔들림
        float sway = Mathf.Sin(t * 0.85f);
        float nod  = Mathf.Sin(t * 1.4f + 0.9f);
        if (_head) _head.localRotation = Quaternion.Slerp(
            _head.localRotation,
            _headRest * Quaternion.Euler(nod * 9f, sway * 18f, sway * 5f),
            _returnSpeed * Time.deltaTime);
    }
}
