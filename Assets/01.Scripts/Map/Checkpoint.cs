using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    // Player detection uses TryGetComponent on player scripts instead of tags.
    [Min(0)]
    [SerializeField] int progressionIndex;

    public bool IsActivated { get; private set; }
    public int ProgressionIndex => progressionIndex;
    public virtual bool UseAsSavePoint => true;

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        OnCheckpointTriggered(other);
    }

    bool IsPlayer(Collider other)
    {
        if (other == null) return false;

        if (other.TryGetComponent<PlayerMovement>(out _))
            return true;

        Transform root = other.transform.root;
        return root != null && root.TryGetComponent<PlayerMovement>(out _);
    }

    protected virtual void OnCheckpointTriggered(Collider other)
    {
        TryActivateCheckpoint();
    }

    protected bool TryActivateCheckpoint()
    {
        if (CheckpointManager.Instance == null) return false;

        CheckpointManager.Instance.ActivateCheckpoint(this);
        return IsActivated;
    }

    public void SetActivated(bool activated)
    {
        IsActivated = activated;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsActivated ? Color.green : Color.yellow;

        if (TryGetComponent<BoxCollider>(out var box))
            Gizmos.DrawWireCube(transform.position + box.center, box.size);
        else if (TryGetComponent<SphereCollider>(out var sphere))
            Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
    }
}
