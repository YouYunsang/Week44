using System.Collections;
using UnityEngine;

public partial class BossProceduralAnimator
{
    // ── 다리 절단 → 쓰러지며 ArmsOnly 전환 ────────────────────────────────
    IEnumerator FallDownRoutine()
    {
        Vector3    startPos = transform.position;
        // 다리 높이만큼 아래로 착지
        Vector3    landPos  = new Vector3(startPos.x, startPos.y - _legHeight, startPos.z);
        Quaternion bodyTilt = _bodyRest * Quaternion.Euler(_fallBodyPitch, 0f, 0f);
        Quaternion buckleL  = _lLegRest * Quaternion.Euler(0f, 0f, 60f);
        Quaternion buckleR  = _rLegRest * Quaternion.Euler(0f, 0f, 60f);

        float e = 0f;
        while (e < _fallTime)
        {
            e += Time.deltaTime;
            float t  = Mathf.Clamp01(e / _fallTime);
            float et = t * t; // ease-in — 중력처럼 가속
            if (_leftLeg)  _leftLeg.localRotation  = Quaternion.Slerp(_lLegRest, buckleL, t);
            if (_rightLeg) _rightLeg.localRotation = Quaternion.Slerp(_rLegRest, buckleR, t);
            if (_body)     _body.localRotation      = Quaternion.Slerp(_bodyRest, bodyTilt, et);
            transform.position = Vector3.Lerp(startPos, landPos, et);
            yield return null;
        }
        transform.position = landPos;
        _groundY = landPos.y;

        yield return LandShake();
    }

    // ── 사망 → CoreOnly 페이즈로 쓰러짐 ──────────────────────────────────
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

    // ── 착지 진동 ─────────────────────────────────────────────────────────
    IEnumerator LandShake()
    {
        float e = 0f;
        while (e < _fallShakeDur)
        {
            e += Time.deltaTime;
            float fade = 1f - e / _fallShakeDur;
            if (_body) _body.localPosition =
                _bodyRestPos + UnityEngine.Random.insideUnitSphere * _fallShakeMag * fade;
            yield return null;
        }
        if (_body) _body.localPosition = _bodyRestPos;
    }

    // ── 착지 충격 스쿼시 ──────────────────────────────────────────────────
    IEnumerator ImpactSquash()
    {
        Vector3 squashed = _bodyRestPos + Vector3.down * 0.15f;
        float   e        = 0f;

        while (e < 0.1f)
        {
            e += Time.deltaTime;
            _body.localPosition = Vector3.Lerp(_bodyRestPos, squashed, e / 0.1f);
            yield return null;
        }
        e = 0f;
        while (e < 0.3f)
        {
            e += Time.deltaTime;
            _body.localPosition = Vector3.Lerp(squashed, _bodyRestPos, e / 0.3f);
            yield return null;
        }
        _body.localPosition = _bodyRestPos;
    }
}
