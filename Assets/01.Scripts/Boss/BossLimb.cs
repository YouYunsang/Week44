using UnityEngine;

/// <summary>
/// 보스 팔다리에 붙이는 컴포넌트.
/// - 같은 오브젝트에 Sliceable이 있으면 OnDestroy에서 감지
/// - 자식 오브젝트에 Sliceable이 있으면 Update에서 null 감지
/// </summary>
public class BossLimb : MonoBehaviour
{
    [SerializeField] LimbType _limbType;

    Sliceable _sliceable;
    bool      _sliced;
    bool      _watching;

    void Start()
    {
        _sliceable = GetComponentInChildren<Sliceable>();
        _watching  = _sliceable != null;
    }

    void Update()
    {
        // 자식 Sliceable이 파괴된 순간 감지
        if (_watching && !_sliced && _sliceable == null)
            Notify();
    }

    void OnDestroy()
    {
        // 이 오브젝트 자체가 슬라이스로 파괴될 때
        if (!gameObject.scene.isLoaded) return;
        Notify();
    }

    void Notify()
    {
        if (_sliced) return;
        _sliced = true;
        EventBus<OnBossLimbSlicedEvent>.Publish(new OnBossLimbSlicedEvent { limb = _limbType });
    }
}
