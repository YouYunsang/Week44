using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.SliceScripts;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 슬라이스 요청을 누적하고, 설정된 횟수에 도달하면 한꺼번에 잘리는 컴포넌트.
/// Sliceable 컴포넌트가 반드시 같이 있어야 함.
/// PlayerSliceExecutor 쪽에서 RequestSlice()를 호출하면 됨.
/// </summary>
[RequireComponent(typeof(Sliceable))]
public class StackedSliceable : MonoBehaviour
{
    // ───────────────────────────── Inspector ─────────────────────────────

    [Header("Slice Settings")]
    [SerializeField] private int _maxSliceCount = 8;

    [Header("Physics")]
    [SerializeField] private float _sliceForce      = 3f;
    [SerializeField] private float _explosionRadius = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool    _showDebugLog    = true;
    [SerializeField] private KeyCode _debugTriggerKey = KeyCode.X;

    // ───────────────────────────── Private ─────────────────────────────

    private readonly List<SlicePlaneData> _pendingSlices = new();
    private int  _currentSliceCount = 0;
    private bool _isSliced          = false;

    // ───────────────────────────── Unity Lifecycle ─────────────────────────────

    private void Update()
    {
        if (_showDebugLog && Input.GetKeyDown(_debugTriggerKey))
            ForceExecuteSlices();
    }

    // ───────────────────────────── Public API ─────────────────────────────

    /// <summary>
    /// PlayerSliceExecutor에서 슬라이스 요청 시 호출.
    /// planeOrigin : 슬라이스 평면의 월드 위치 (보통 hit.point)
    /// planeNormal : 슬라이스 평면의 법선 벡터 (보통 카메라 업 벡터 등)
    /// </summary>
    public void RequestSlice(Vector3 planeOrigin, Vector3 planeNormal)
    {
        if (_isSliced) return;

        _pendingSlices.Add(new SlicePlaneData(planeOrigin, planeNormal));
        _currentSliceCount++;

        if (_showDebugLog)
            Debug.Log($"[StackedSliceable] {gameObject.name} : {_currentSliceCount} / {_maxSliceCount} 누적");

        if (_currentSliceCount >= _maxSliceCount)
            ExecuteAllSlices();
    }

    /// <summary>
    /// 누적 횟수 무관하게 강제 실행 (디버그 키 / 외부 트리거)
    /// </summary>
    public void ForceExecuteSlices()
    {
        if (_isSliced) return;

        if (_pendingSlices.Count == 0)
        {
            Debug.LogWarning("[StackedSliceable] 누적된 슬라이스가 없습니다.");
            return;
        }

        ExecuteAllSlices();
    }

    // ───────────────────────────── Private Logic ─────────────────────────────

    private void ExecuteAllSlices()
    {
        _isSliced = true;

        if (_showDebugLog)
            Debug.Log($"[StackedSliceable] {gameObject.name} : {_pendingSlices.Count}회 슬라이스 실행!");

        StartCoroutine(SliceSequence());
    }

    private IEnumerator SliceSequence()
    {
        GameObject currentTarget = gameObject;
        List<GameObject> allSlicedPieces = new();

        foreach (SlicePlaneData slice in _pendingSlices)
        {
            if (currentTarget == null) break;

            // new Plane(법선, 평면 위의 점) 으로 커스텀 Slicer에 전달
            Plane plane = new Plane(slice.Normal, slice.Origin);

            GameObject[] pieces;
            try
            {
                // 커스텀 Slicer.Slice — 내부에서 Collider/Rigidbody/SliceFragment 자동 세팅
                pieces = Slicer.Slice(plane, currentTarget);
            }
            catch (NotSupportedException e)
            {
                // Sliceable 컴포넌트 누락 시
                if (_showDebugLog)
                    Debug.LogWarning($"[StackedSliceable] 슬라이스 실패 — Sliceable 없음: {e.Message}");
                break;
            }
            catch (Exception e)
            {
                if (_showDebugLog)
                    Debug.LogWarning($"[StackedSliceable] 슬라이스 실패: {e.Message}");
                continue;
            }

            if (pieces == null || pieces.Length < 2)
            {
                if (_showDebugLog)
                    Debug.LogWarning("[StackedSliceable] Slicer가 조각을 반환하지 않음.");
                continue;
            }

            GameObject positive = pieces[0]; // positive side
            GameObject negative = pieces[1]; // negative side

            allSlicedPieces.Add(positive);
            allSlicedPieces.Add(negative);

            // 원본이 아닌 중간 조각은 제거
            if (currentTarget != gameObject)
                Destroy(currentTarget);

            // 다음 슬라이스는 negative 조각에 이어서 적용
            currentTarget = negative;

            yield return null; // 프레임 분산
        }

        // 원본 오브젝트 제거
        Destroy(gameObject);

        // 조각들에 폭발력 적용
        ApplyExplosionForce(allSlicedPieces);
    }

    private void ApplyExplosionForce(List<GameObject> pieces)
    {
        foreach (GameObject piece in pieces)
        {
            if (piece == null) continue;
            if (!piece.TryGetComponent<Rigidbody>(out Rigidbody rb)) continue;

            Vector3 randomDir = (
                piece.transform.position - transform.position
                + Random.insideUnitSphere * _explosionRadius
            ).normalized;

            rb.AddForce(randomDir * _sliceForce, ForceMode.Impulse);
        }
    }

    // ───────────────────────────── Gizmos ─────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (SlicePlaneData slice in _pendingSlices)
        {
            Gizmos.DrawLine(slice.Origin, slice.Origin + slice.Normal * 0.5f);
            Gizmos.DrawWireSphere(slice.Origin, 0.05f);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────

public readonly struct SlicePlaneData
{
    public readonly Vector3 Origin;
    public readonly Vector3 Normal;

    public SlicePlaneData(Vector3 origin, Vector3 normal)
    {
        Origin = origin;
        Normal = normal;
    }
}