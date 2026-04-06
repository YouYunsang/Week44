using System;
using System.Collections;
using UnityEngine;

public partial class BossProceduralAnimator
{
    // ══════════════════════════════════════════════════════════════════════
    // 위치 / 방향 헬퍼
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>보스 루트의 현재 XZ를 _groundY 기준 Y로 반환</summary>
    Vector3 GroundPos() =>
        new Vector3(transform.position.x, _groundY, transform.position.z);

    /// <summary>Y를 제거한 정규화 전방 벡터</summary>
    Vector3 FlatForward() =>
        new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

    /// <summary>
    /// origin 위치 위에서 아래로 레이캐스트해 실제 바닥 Y를 반환.
    /// _groundLayer를 인스펙터에서 지형 레이어만 포함하도록 설정하면
    /// 보스·플레이어 콜라이더를 무시할 수 있음.
    /// </summary>
    float FindFloorY(Vector3 origin)
    {
        var ray = new Ray(origin + Vector3.up * 5f, Vector3.down);
        return Physics.Raycast(ray, out RaycastHit hit, 20f, _groundLayer)
            ? hit.point.y
            : _groundY;
    }

    /// <summary>로컬 오프셋(x=우, y=상, z=전)을 월드 좌표 벡터로 변환</summary>
    static Vector3 LocalOffset(Vector3 fwdN, Vector3 offset)
    {
        Vector3 right = Vector3.Cross(Vector3.up, fwdN);
        return right * offset.x + Vector3.up * offset.y + fwdN * offset.z;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 이동 헬퍼
    // ══════════════════════════════════════════════════════════════════════

    void FaceHead()
    {
        if (_head == null) return;
        Vector3 dir = new Vector3(
            _target.position.x - _head.position.x, 0f,
            _target.position.z - _head.position.z);
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        _head.rotation = Quaternion.Slerp(_head.rotation, targetRot, _rotateSpeed * Time.unscaledDeltaTime);
    }

    void FaceTarget()
    {
        Vector3 dir = new Vector3(
            _target.position.x - transform.position.x, 0f,
            _target.position.z - transform.position.z);
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation, Quaternion.LookRotation(dir),
            _rotateSpeed * Time.unscaledDeltaTime);
    }

    void PushTowardTarget(float amount)
    {
        if (_target == null) return;
        Vector3 dir = new Vector3(
            _target.position.x - transform.position.x, 0f,
            _target.position.z - transform.position.z);
        if (dir.sqrMagnitude < 0.01f) return;
        transform.position += dir.normalized * amount;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 공격 판정 헬퍼
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 직육면체 범위 안에 플레이어가 있으면 OnBossAttackHitEvent를 발행.
    /// origin = 박스 뒷면 하단 중앙, size = (너비, 높이, 깊이)
    /// </summary>
    void PublishHitRect(Vector3 dir, float force, Vector3 origin, Vector3 forward, Vector3 size)
    {
        if (_target == null) return;

        Vector3 fwd = new Vector3(forward.x, 0f, forward.z).normalized;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;

        Vector3 right    = Vector3.Cross(Vector3.up, fwd);
        Vector3 toTarget = new Vector3(
            _target.position.x - origin.x, 0f,
            _target.position.z - origin.z);

        float fwdDist   = Vector3.Dot(toTarget, fwd);
        float rightDist = Vector3.Dot(toTarget, right);

        var   cc     = _target.GetComponent<CharacterController>();
        float buffer = cc != null ? cc.radius : 0f;

        bool inBox = fwdDist              >= -buffer
                  && fwdDist              <= size.z + buffer
                  && Mathf.Abs(rightDist) <= size.x * 0.5f + buffer;

        if (!inBox) return;

        Vector3 center = origin + fwd * (size.z * 0.5f) + Vector3.up * (size.y * 0.5f);
        Vector3 hitDir = (_target.position - center).normalized;
        EventBus<OnBossAttackHitEvent>.Publish(new OnBossAttackHitEvent
        {
            direction = hitDir.sqrMagnitude > 0.01f ? hitDir : dir,
            force     = force
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // 애니메이션 헬퍼
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>현재 회전에서 rest * delta로 Slerp (이동 중 자연스러운 복귀)</summary>
    void Rot(Transform t, Quaternion rest, Quaternion delta)
    {
        if (t == null) return;
        t.localRotation = Quaternion.Slerp(t.localRotation, rest * delta, _returnSpeed * Time.deltaTime);
    }

    /// <summary>현재 회전에서 rest로 Slerp (대기 시 복귀)</summary>
    void ReturnToRest(Transform t, Quaternion rest)
    {
        if (t == null) return;
        t.localRotation = Quaternion.Slerp(t.localRotation, rest, _returnSpeed * Time.deltaTime);
    }

    /// <summary>단일 리짐 회전 보간. onMid는 t=0.5 시점에 한 번 호출</summary>
    IEnumerator LerpLimb(Transform limb, Quaternion from, Quaternion to,
        float duration, Action onMid = null)
    {
        float e        = 0f;
        bool  midFired = false;
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

    /// <summary>
    /// 단일 축 각도(float)를 직접 보간 — Slerp의 최단경로 제약 없이 의도한 방향으로 회전.
    /// baseRot * AngleAxis(angle, axis) 로 최종 회전을 계산.
    /// 180도 초과 스윙에 사용.
    /// </summary>
    IEnumerator LerpLimbAngle(Transform limb, Quaternion baseRot,
        float fromAngle, float toAngle, Vector3 axis,
        float duration, Action onMid = null)
    {
        float e        = 0f;
        bool  midFired = false;
        while (e < duration)
        {
            e += Time.deltaTime;
            float t     = Mathf.Clamp01(e / duration);
            float angle = Mathf.Lerp(fromAngle, toAngle, t);
            if (limb) limb.localRotation = baseRot * Quaternion.AngleAxis(angle, axis);
            if (!midFired && t >= 0.5f) { midFired = true; onMid?.Invoke(); }
            yield return null;
        }
        if (limb) limb.localRotation = baseRot * Quaternion.AngleAxis(toAngle, axis);
    }

    /// <summary>두 리짐을 동시에 보간 (다리·팔 쌍 공용)</summary>
    IEnumerator LerpBothLimbs(
        Transform lL, Quaternion fromL, Quaternion toL,
        Transform lR, Quaternion fromR, Quaternion toR,
        float duration, Action onMid = null)
    {
        float e        = 0f;
        bool  midFired = false;
        while (e < duration)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / duration);
            if (lL) lL.localRotation = Quaternion.Slerp(fromL, toL, t);
            if (lR) lR.localRotation = Quaternion.Slerp(fromR, toR, t);
            if (!midFired && t >= 0.5f) { midFired = true; onMid?.Invoke(); }
            yield return null;
        }
        if (lL) lL.localRotation = toL;
        if (lR) lR.localRotation = toR;
    }

    /// <summary>
    /// 두 리짐을 단일 축 각도(float)로 동시에 보간 — 180도 초과 스윙용.
    /// baseL/R * AngleAxis(angle, axis) 로 계산.
    /// </summary>
    IEnumerator LerpBothLimbsAngle(
        Transform lL, Quaternion baseL, float fromAngleL, float toAngleL,
        Transform lR, Quaternion baseR, float fromAngleR, float toAngleR,
        Vector3 axis, float duration, Action onMid = null)
    {
        float e        = 0f;
        bool  midFired = false;
        while (e < duration)
        {
            e += Time.deltaTime;
            float t  = Mathf.Clamp01(e / duration);
            float aL = Mathf.Lerp(fromAngleL, toAngleL, t);
            float aR = Mathf.Lerp(fromAngleR, toAngleR, t);
            if (lL) lL.localRotation = baseL * Quaternion.AngleAxis(aL, axis);
            if (lR) lR.localRotation = baseR * Quaternion.AngleAxis(aR, axis);
            if (!midFired && t >= 0.5f) { midFired = true; onMid?.Invoke(); }
            yield return null;
        }
        if (lL) lL.localRotation = baseL * Quaternion.AngleAxis(toAngleL, axis);
        if (lR) lR.localRotation = baseR * Quaternion.AngleAxis(toAngleR, axis);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Gizmo
    // ══════════════════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        Vector3 pos  = GroundPos();
        Vector3 fwdN = FlatForward();

        // 발차기 범위 (주황)
        Gizmos.color = new Color(1f, 0.55f, 0f, 0.4f);
        DrawGizmoBox(pos + LocalOffset(fwdN, _kickOffset), fwdN, _kickSize);

        // 스톰프 범위 (노랑)
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.4f);
        Vector3 stompCtr = pos + LocalOffset(fwdN, _stompOffset);
        DrawGizmoBox(stompCtr - fwdN * (_stompSize.z * 0.5f), fwdN, _stompSize);

        // 스핀 범위 (주황 원)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(pos, _spinRadius);

        // 슬램 범위 (빨강)
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.4f);
        DrawGizmoBox(pos + LocalOffset(fwdN, _slamOffset), fwdN, _slamSize);

        // 구르기 범위 (보라)
        Gizmos.color = new Color(0.7f, 0.2f, 1f, 0.4f);
        Gizmos.DrawWireSphere(pos, _rollHitRadius);
    }

    void DrawGizmoBox(Vector3 origin, Vector3 forward, Vector3 size)
    {
        Vector3 fwd    = new Vector3(forward.x, 0f, forward.z).normalized;
        Vector3 center = origin + fwd * (size.z * 0.5f) + Vector3.up * (size.y * 0.5f);
        var     old    = Gizmos.matrix;
        Gizmos.matrix  = Matrix4x4.TRS(center, Quaternion.LookRotation(fwd), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix  = old;
    }
}
