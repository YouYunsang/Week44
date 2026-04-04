using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어가 닿으면 지정된 씬으로 전환.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SceneChangeTrigger : MonoBehaviour
{
    [SerializeField] string sceneName;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        if (string.IsNullOrEmpty(sceneName)) return;

        SceneManager.LoadScene(sceneName);
    }

    bool IsPlayer(Collider other)
    {
        if (other.TryGetComponent<PlayerMovement>(out _)) return true;

        Transform root = other.transform.root;
        return root != null && root.TryGetComponent<PlayerMovement>(out _);
    }
}