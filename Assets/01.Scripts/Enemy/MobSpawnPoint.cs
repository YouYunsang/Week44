using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MobEntry
{
    [Tooltip("씬에 배치된 몹 (EnemyBase). 한 번만 연결하면 됨")]
    public GameObject initialMob;

    [Tooltip("리스폰 시 사용할 프리팹")]
    public GameObject mobPrefab;

    [Tooltip("스폰된 몹을 붙일 부모 Transform. 없으면 최상위로 스폰")]
    public Transform spawnParent;

    [Tooltip("스폰 위치. 없으면 MobSpawnPoint 위치 사용")]
    public Transform spawnPoint;

    [NonSerialized] public GameObject spawnedMob;

    public bool IsAlive => spawnedMob != null;

    public void Init()
    {
        spawnedMob = initialMob;
    }
}

public class MobSpawnPoint : MonoBehaviour
{
    [Tooltip("이 몹들이 속한 체크포인트의 ProgressionIndex")]
    [SerializeField] int checkpointIndex;

    [SerializeField] List<MobEntry> mobs = new List<MobEntry>();

    void Start()
    {
        foreach (var mob in mobs)
            mob.Init();
    }

    public bool BelongsTo(int index) => checkpointIndex == index;

    public void TryRespawn()
    {
        foreach (var mob in mobs)
        {
            // 죽어가는 중(아직 Destroy 안 됨)이어도 강제로 정리하고 새로 스폰
            if (mob.spawnedMob != null)
            {
                // BossLimb의 OnDestroy가 phantom 이벤트를 쏘지 않도록 먼저 억제
                foreach (var limb in mob.spawnedMob.GetComponentsInChildren<BossLimb>())
                    limb.MarkForRespawn();
                DestroyImmediate(mob.spawnedMob);
                mob.spawnedMob = null;
            }

            SpawnMob(mob);
        }
    }

    void SpawnMob(MobEntry mob)
    {
        if (mob.mobPrefab == null) return;

        Vector3 pos = mob.spawnPoint != null ? mob.spawnPoint.position : transform.position;
        Quaternion rot = mob.spawnPoint != null ? mob.spawnPoint.rotation : transform.rotation;

        mob.spawnedMob = Instantiate(mob.mobPrefab, pos, rot, mob.spawnParent);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        foreach (var mob in mobs)
        {
            if (mob == null) continue;
            Vector3 pos = mob.spawnPoint != null ? mob.spawnPoint.position : transform.position;
            Gizmos.color = mob.IsAlive ? Color.green : Color.red;
            Gizmos.DrawWireSphere(pos, 0.4f);
            UnityEditor.Handles.Label(pos + Vector3.up * 0.6f,
                mob.mobPrefab != null ? mob.mobPrefab.name : "No Prefab");
        }
    }
#endif
}