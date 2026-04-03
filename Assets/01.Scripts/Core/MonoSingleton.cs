using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this as T;

        GameObject persistTarget = transform.root != null ? transform.root.gameObject : gameObject;
        DontDestroyOnLoad(persistTarget);
    }
}
