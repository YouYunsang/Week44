using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SliceableMeshSceneChanger : Sliceable
{
    [Header("Scene Change Settings")]
    [SerializeField] private string _nextSceneName;
    [SerializeField] private float _sceneChangeDelay = 2f;

    private MeshFilter _myFilter;
    private MeshRenderer _myRenderer;
    private bool _isAppQuitting = false;
    private static bool _sceneChangeScheduled = false;

    void Awake()
    {
        _myFilter = GetComponent<MeshFilter>();
        _myRenderer = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        SetupTMPMesh();
        EventBus<OnSliceableReadyEvent>.Publish(new OnSliceableReadyEvent());
    }

    private void SetupTMPMesh()
    {
        TMP_Text tmp = GetComponentInChildren<TMP_Text>();
        if (tmp == null)
        {
            Debug.LogWarning("[SliceableMeshSceneChanger] TMP_Text 자식을 찾지 못했습니다.");
            return;
        }

        tmp.ForceMeshUpdate();

        if (tmp.mesh == null || tmp.mesh.vertexCount == 0)
        {
            Debug.LogWarning("[SliceableMeshSceneChanger] TMP 메시가 비어있습니다.");
            return;
        }

        // TMP 메시와 머티리얼을 루트에 직접 적용
        _myFilter.mesh = Instantiate(tmp.mesh);
        _myRenderer.sharedMaterial = tmp.fontSharedMaterial;

        // 루트에서 TMP를 렌더링하므로 자식 TMP는 숨김
        tmp.gameObject.SetActive(false);

        UpdateCollider(_myFilter.mesh);
    }

    private void UpdateCollider(Mesh mesh)
    {
        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc != null) { mc.sharedMesh = mesh; return; }

        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null) { bc.center = mesh.bounds.center; bc.size = mesh.bounds.size; return; }

        BoxCollider newBox = gameObject.AddComponent<BoxCollider>();
        newBox.center = mesh.bounds.center;
        newBox.size   = mesh.bounds.size;
    }

    private void OnApplicationQuit() => _isAppQuitting = true;

    private void OnDestroy()
    {
        if (_isAppQuitting || !gameObject.scene.isLoaded) return;
        if (string.IsNullOrEmpty(_nextSceneName)) return;
        if (_sceneChangeScheduled) return;

        _sceneChangeScheduled = true;

        var runner = new GameObject("_SceneChangeRunner");
        DontDestroyOnLoad(runner);
        runner.AddComponent<SceneChangeRunner>().Init(_nextSceneName, _sceneChangeDelay);
    }
}

internal class SceneChangeRunner : MonoBehaviour
{
    public void Init(string sceneName, float delay)
    {
        StartCoroutine(Run(sceneName, delay));
    }

    private IEnumerator Run(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        EventBus<OnSceneChangeBeginEvent>.Publish(new OnSceneChangeBeginEvent());
        SceneManager.LoadScene(sceneName);
        Destroy(gameObject);
    }
}
