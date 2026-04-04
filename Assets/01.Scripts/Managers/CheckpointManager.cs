using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoSingleton<CheckpointManager>
{
    [SerializeField] List<Checkpoint> checkpoints = new List<Checkpoint>();
    Checkpoint _activeCheckpoint;

    #region Editor

    void Reset()
    {
        RebuildCheckpointList();
    }

    void OnValidate()
    {
        if (Application.isPlaying) return;
        RebuildCheckpointList();
    }

    #endregion

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        RebuildCheckpointList();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebuildCheckpointList();
        _activeCheckpoint = null;
    }

    public void RebuildCheckpointList()
    {
        checkpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None)
            .Where(c => c != null && c.UseAsSavePoint)
            .OrderBy(c => c.ProgressionIndex)
            .ToList();

        foreach (var checkpoint in checkpoints)
            checkpoint.SetActivated(false);
    }

    public void ActivateCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null) return;
        if (!checkpoint.UseAsSavePoint) return;

        if (!checkpoints.Contains(checkpoint))
            RebuildCheckpointList();

        int targetOrder = checkpoints.IndexOf(checkpoint);
        if (targetOrder < 0) return;

        if (_activeCheckpoint != null)
        {
            int activeOrder = checkpoints.IndexOf(_activeCheckpoint);
            if (activeOrder >= 0 && targetOrder < activeOrder)
                return;
        }

        if (_activeCheckpoint == checkpoint) return;

        if (_activeCheckpoint != null)
            _activeCheckpoint.SetActivated(false);

        _activeCheckpoint = checkpoint;
        _activeCheckpoint.SetActivated(true);
    }

    public Vector3 GetCheckpointPosition()
    {
        return _activeCheckpoint != null ? _activeCheckpoint.transform.position : Vector3.zero;
    }

    public bool HasCheckpoint()
    {
        return _activeCheckpoint != null;
    }

    #region unused

    public IReadOnlyList<Checkpoint> GetCheckpoints()
    {
        return checkpoints;
    }

    public string GetCheckpointSceneName()
    {
        return _activeCheckpoint != null ? _activeCheckpoint.gameObject.scene.name : string.Empty;
    }

    public void DeleteCheckpoint()
    {
        if (_activeCheckpoint != null)
            _activeCheckpoint.SetActivated(false);

        _activeCheckpoint = null;
    }
    #endregion
}
