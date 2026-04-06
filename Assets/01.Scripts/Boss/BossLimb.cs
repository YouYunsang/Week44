using UnityEngine;

/// <summary>
/// 보스 팔다리에 붙이는 컴포넌트.
/// - 자식 Sliceable이 파괴되면 OnBossLimbSlicedEvent 발행
/// - 보스 사망 시 팔 limb는 스스로 비활성화
/// </summary>
public class BossLimb : MonoBehaviour
{
    [SerializeField] LimbType _limbType;

    Sliceable _sliceable;
    bool      _sliced;
    bool      _watching;

    public bool IsSliced => _sliced;

    static readonly bool[] _isArm = new bool[]
    {
        false, // Head
        true,  // LeftArm
        true,  // RightArm
        false, // LeftLeg
        false, // RightLeg
        false  // Torso
    };

    void Start()
    {
        _sliceable = GetComponentInChildren<Sliceable>();
        _watching  = _sliceable != null;
    }

    void OnEnable()  => EventBus<OnBossDiedEvent>.Subscribe(OnBossDied);
    void OnDisable() => EventBus<OnBossDiedEvent>.Unsubscribe(OnBossDied);

    void Update()
    {
        if (_watching && !_sliced && _sliceable == null)
            Notify();
    }

    void OnDestroy()
    {
        if (!gameObject.scene.isLoaded) return;
        Notify();
    }

    void OnBossDied(OnBossDiedEvent e)
    {
        if (_isArm[(int)_limbType])
            gameObject.SetActive(false);
    }

    void Notify()
    {
        if (_sliced) return;
        _sliced = true;
        EventBus<OnBossLimbSlicedEvent>.Publish(new OnBossLimbSlicedEvent { limb = _limbType });
    }
}
