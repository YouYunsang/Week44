using UnityEngine;

/// <summary>
/// 보스 공격 이벤트를 받아 플레이어를 사망 처리합니다.
/// 사망 시스템 구현 전까지는 로그로 대체.
/// </summary>
public class PlayerHitReceiver : MonoBehaviour
{
    void OnEnable()  => EventBus<OnBossAttackHitEvent>.Subscribe(OnHit);
    void OnDisable() => EventBus<OnBossAttackHitEvent>.Unsubscribe(OnHit);

    void OnHit(OnBossAttackHitEvent e)
    {
        Debug.Log($"[Player] 보스 공격에 사망 — 방향: {e.direction}, 힘: {e.force}");
        // TODO: 실제 사망 처리 (GameOver 이벤트 등)
    }
}
