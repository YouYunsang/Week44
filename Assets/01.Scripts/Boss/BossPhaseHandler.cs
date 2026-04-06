using UnityEngine;

/// <summary>
/// 잘린 팔다리를 추적하여 페이즈를 결정하고 이벤트로 알립니다.
///
/// Phase 전환 조건:
///   FullBody → ArmsOnly : 양쪽 다리 모두 잘림
///   ArmsOnly → CoreOnly : 양쪽 팔 모두 잘림
/// </summary>
public class BossPhaseHandler : MonoBehaviour
{
    BossPhase _phase = BossPhase.FullBody;

    bool _leftLegCut;
    bool _rightLegCut;
    bool _leftArmCut;
    bool _rightArmCut;

    public BossPhase CurrentPhase => _phase;

    public int  LegsCut     => (_leftLegCut  ? 1 : 0) + (_rightLegCut  ? 1 : 0);
    public int  ArmsCut     => (_leftArmCut  ? 1 : 0) + (_rightArmCut  ? 1 : 0);
    public bool LeftArmCut  => _leftArmCut;
    public bool RightArmCut => _rightArmCut;

    void OnEnable()  => EventBus<OnBossLimbSlicedEvent>.Subscribe(OnLimbSliced);
    void OnDisable() => EventBus<OnBossLimbSlicedEvent>.Unsubscribe(OnLimbSliced);

    void OnLimbSliced(OnBossLimbSlicedEvent e)
    {
        switch (e.limb)
        {
            case LimbType.LeftLeg:  _leftLegCut  = true; break;
            case LimbType.RightLeg: _rightLegCut = true; break;
            case LimbType.LeftArm:  _leftArmCut  = true; break;
            case LimbType.RightArm: _rightArmCut = true; break;
            case LimbType.Torso:
                if (_phase == BossPhase.CoreOnly) return;
                _phase = BossPhase.CoreOnly;
                Debug.Log("[Boss] 몸통 파괴 → CoreOnly 전환");
                EventBus<OnBossPhaseChangedEvent>.Publish(new OnBossPhaseChangedEvent { phase = _phase });
                return;
            case LimbType.Head:
                Debug.Log("[Boss] 머리 파괴 → 사망");
                EventBus<OnBossDiedEvent>.Publish(new OnBossDiedEvent());
                return;
        }

        Debug.Log($"[Boss] 팔다리 절단: {e.limb} / 상태 — 왼다리:{_leftLegCut} 오른다리:{_rightLegCut} 왼팔:{_leftArmCut} 오른팔:{_rightArmCut}");

        EvaluatePhase();
    }

    void EvaluatePhase()
    {
        BossPhase next;

        if (_leftArmCut && _rightArmCut)
            next = BossPhase.CoreOnly;
        else if (_leftLegCut && _rightLegCut)
            next = BossPhase.ArmsOnly;
        else
            return;

        if (next == _phase) return;

        _phase = next;
        Debug.Log($"[Boss] 페이즈 전환 → {_phase}");
        EventBus<OnBossPhaseChangedEvent>.Publish(new OnBossPhaseChangedEvent { phase = _phase });
    }
}
