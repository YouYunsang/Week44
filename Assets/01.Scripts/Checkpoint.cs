using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";
    [Min(0)]
    [SerializeField] int progressionIndex;

    public bool IsActivated { get; private set; }
    public int ProgressionIndex => progressionIndex;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        CheckpointManager.Instance.ActivateCheckpoint(this);
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
