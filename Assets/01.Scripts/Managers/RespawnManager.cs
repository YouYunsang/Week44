using UnityEngine;

public class RespawnManager : MonoSingleton<RespawnManager>
{
    [SerializeField] InputSO inputSO;
    [SerializeField] string playerTag = "Player";

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

    void OnRestart()
    {
        TryRespawnByTag();
    }

    public bool TryRespawnByTag()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return false;

        return TryRespawn(player.transform);
    }

    public bool TryRespawn(Transform target)
    {
        if (target == null) return false;
        if (!CheckpointManager.Instance.HasCheckpoint()) return false;

        Vector3 checkpointPosition = CheckpointManager.Instance.GetCheckpointPosition();

        if (target.TryGetComponent<CharacterController>(out var characterController))
            characterController.enabled = false;

        target.position = checkpointPosition;

        if (characterController != null)
            characterController.enabled = true;

        return true;
    }
}
