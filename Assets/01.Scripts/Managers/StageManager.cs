using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoSingleton<StageManager>
{
    [SerializeField] InputSO inputSO;

    List<MobSpawnPoint> _spawnPoints = new List<MobSpawnPoint>();

    protected override void Awake()
    {
        base.Awake();
        CollectSpawnPoints();
    }

    void OnEnable()
    {
        if (inputSO != null)
            inputSO.OnRestart += OnRestart;
    }

    void OnDisable()
    {
        if (inputSO != null)
            inputSO.OnRestart -= OnRestart;
    }

    void CollectSpawnPoints()
    {
        _spawnPoints = new List<MobSpawnPoint>(
            FindObjectsByType<MobSpawnPoint>(FindObjectsSortMode.None));
    }

    void OnRestart()
    {
        RespawnDeadMobs();
    }

    void RespawnDeadMobs()
    {
        int activeIndex = GetActiveCheckpointIndex();
        if (activeIndex < 0) return;

        foreach (var spawnPoint in _spawnPoints)
        {
            if (spawnPoint == null) continue;
            if (!spawnPoint.BelongsTo(activeIndex)) continue;

            spawnPoint.TryRespawn();
        }
    }

    int GetActiveCheckpointIndex()
    {
        if (!CheckpointManager.Instance.HasCheckpoint()) return -1;

        Vector3 activePos = CheckpointManager.Instance.GetCheckpointPosition();
        var checkpoints = CheckpointManager.Instance.GetCheckpoints();

        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] != null && checkpoints[i].transform.position == activePos)
                return checkpoints[i].ProgressionIndex;
        }

        return -1;
    }
}