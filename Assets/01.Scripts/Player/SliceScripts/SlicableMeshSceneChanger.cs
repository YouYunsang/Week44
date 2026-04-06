using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro; // TMP 사용을 위해 필수

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SliceableMeshSceneChanger : Sliceable
{
    [Header("Mesh Combine Settings")]
    [SerializeField] private bool _combineOnStart = true;
    [SerializeField] private bool _destroyOriginalChildren = false;

    [Header("Scene Change Settings")]
    [SerializeField] private string _nextSceneName;

    private MeshFilter _myFilter;
    private MeshRenderer _myRenderer;
    private bool _isAppQuitting = false;

    void Awake()
    {
        _myFilter = GetComponent<MeshFilter>();
        _myRenderer = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        if (_combineOnStart)
        {
            CombineAllChildren();
        }
    }

    /// <summary>
    /// 일반 메시와 TMP 메시를 모두 추출하여 하나로 합칩니다.
    /// </summary>
    private void CombineAllChildren()
    {
        // 1. 자식들에게서 모든 MeshFilter와 TMP 컴포넌트를 수집
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>();

        List<CombineInstance> combineList = new List<CombineInstance>();
        Material targetMat = null;

        // 2. 일반 MeshFilter 처리
        foreach (var filter in meshFilters)
        {
            if (filter == _myFilter) continue; // 자기 자신 제외
            if (filter.sharedMesh == null) continue;
            
            // TMP가 내부적으로 생성한 MeshFilter는 TMP 전용 로직에서 처리하므로 제외
            if (filter.GetComponent<TMP_Text>() != null) continue;

            // 대표 머티리얼 설정 (첫 번째 발견되는 것)
            if (targetMat == null)
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer != null) targetMat = renderer.sharedMaterial;
            }

            CombineInstance ci = new CombineInstance();
            ci.mesh = filter.sharedMesh;
            // 부모의 로컬 좌표계 기준으로 변환
            ci.transform = transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            combineList.Add(ci);

            if (_destroyOriginalChildren) Destroy(filter.gameObject);
            else filter.gameObject.SetActive(false);
        }

        // 3. TextMeshPro 메시 처리 (방법 B 적용)
        foreach (var tmp in tmpTexts)
        {
            // 현재 텍스트 상태로 메시를 강제 갱신
            tmp.ForceMeshUpdate();
            
            // TMP의 메시를 복제 (원본 보호 및 일반 Mesh 취급)
            Mesh meshCopy = Instantiate(tmp.mesh);

            // TMP 머티리얼을 우선적으로 대표 머티리얼로 설정 (글자 색상/폰트 유지 목적)
            if (targetMat == null || targetMat.name.Contains("Default"))
            {
                targetMat = tmp.fontSharedMaterial;
            }

            CombineInstance ci = new CombineInstance();
            ci.mesh = meshCopy;
            ci.transform = transform.worldToLocalMatrix * tmp.transform.localToWorldMatrix;
            combineList.Add(ci);

            if (_destroyOriginalChildren) Destroy(tmp.gameObject);
            else tmp.gameObject.SetActive(false);
        }

        // 4. 최종 합치기 실행
        if (combineList.Count > 0)
        {
            Mesh finalMesh = new Mesh();
            finalMesh.name = "CombinedSliceableMesh";
            // 정점 수가 많을 경우를 대비해 32비트 인덱스 사용
            finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            
            // 모든 메시를 하나의 서브메시로 합침 (true, true)
            finalMesh.CombineMeshes(combineList.ToArray(), true, true);
            
            _myFilter.mesh = finalMesh;
            if (targetMat != null) _myRenderer.sharedMaterial = targetMat;
            
            Debug.Log($"[Combined] {combineList.Count}개의 요소를 합쳤습니다. (TMP 포함)");
        }
    }

    private void OnApplicationQuit() => _isAppQuitting = true;

    private void OnDestroy()
    {
        // 앱 종료 중이 아니고 씬이 유효할 때만 실행
        if (_isAppQuitting || !gameObject.scene.isLoaded) return;

        if (!string.IsNullOrEmpty(_nextSceneName))
        {
            PerformSceneChange();
        }
    }

    private void PerformSceneChange()
    {
        // 씬 전환 실행
        SceneManager.LoadScene(_nextSceneName);
    }
}