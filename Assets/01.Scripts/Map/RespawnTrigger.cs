using UnityEngine;

/// <summary>
/// 플레이어가 닿으면 체크포인트로 즉시 리스폰.
/// Collider를 Trigger로 설정해야 함.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RespawnTrigger : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        bool respawned = RespawnManager.Instance.TryRespawnByTag();
        if (respawned)
            StageManager.Instance.TryRespawnAll();
    }

    bool IsPlayer(Collider other)
    {
        if (other.TryGetComponent<PlayerMovement>(out _)) return true;

        Transform root = other.transform.root;
        return root != null && root.TryGetComponent<PlayerMovement>(out _);
    }
}