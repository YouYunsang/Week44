using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.SliceScripts;
using UnityEngine;

[RequireComponent(typeof(Sliceable))]
public class StackedSliceable : MonoBehaviour
{
    [Header("Slice Settings")]
    [SerializeField] private int _maxSliceCount = 8;

    [Header("Physics")]
    [SerializeField] private float _sliceForce = 8f;
    [SerializeField] private float _separateForceRatio = 0.55f;
    [SerializeField] private float _torqueForceRatio = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLog = true;
    [SerializeField] private KeyCode _debugTriggerKey = KeyCode.X;

    private readonly List<SlicePlaneData> _pendingSlices = new();
    private int _currentSliceCount = 0;
    private bool _isSliced = false;

    private void Update()
    {
        if (_showDebugLog && Input.GetKeyDown(_debugTriggerKey))
            ForceExecuteSlices();
    }

    public void RequestSlice(Vector3 planeOrigin, Vector3 planeNormal, Vector3 sliceDirection)
    {
        if (_isSliced) return;

        Vector3 safeNormal = planeNormal.sqrMagnitude > 0.0001f
            ? planeNormal.normalized
            : Vector3.up;

        Vector3 safeDirection = sliceDirection.sqrMagnitude > 0.0001f
            ? sliceDirection.normalized
            : Vector3.right;

        _pendingSlices.Add(new SlicePlaneData(planeOrigin, safeNormal, safeDirection));
        _currentSliceCount++;

        if (_showDebugLog)
            Debug.Log($"[StackedSliceable] {gameObject.name} : {_currentSliceCount} / {_maxSliceCount} 누적");

        if (_currentSliceCount >= _maxSliceCount)
            ExecuteAllSlices();
    }

    public void RequestSlice(Vector3 planeOrigin, Vector3 planeNormal)
    {
        Vector3 fallbackDirection = Vector3.Cross(planeNormal.normalized, Vector3.up);
        if (fallbackDirection.sqrMagnitude <= 0.0001f)
            fallbackDirection = Vector3.right;

        RequestSlice(planeOrigin, planeNormal, fallbackDirection.normalized);
    }

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

        foreach (SlicePlaneData slice in _pendingSlices)
        {
            if (currentTarget == null) break;

            Plane plane = new Plane(slice.Normal, slice.Origin);

            GameObject[] pieces;
            try
            {
                pieces = Slicer.Slice(plane, currentTarget);
            }
            catch (NotSupportedException e)
            {
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

            GameObject positive = pieces[0];
            GameObject negative = pieces[1];

            ApplySliceForce(positive, negative, slice);

            if (currentTarget != gameObject)
                Destroy(currentTarget);

            currentTarget = negative;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void ApplySliceForce(GameObject positive, GameObject negative, SlicePlaneData slice)
    {
        Vector3 positiveDir = (slice.Direction + slice.Normal * _separateForceRatio).normalized;
        Vector3 negativeDir = (slice.Direction - slice.Normal * _separateForceRatio).normalized;

        ApplyForceToPiece(positive, positiveDir, slice.Normal);
        ApplyForceToPiece(negative, negativeDir, -slice.Normal);
    }

    private void ApplyForceToPiece(GameObject piece, Vector3 forceDir, Vector3 normalDir)
    {
        if (piece == null) return;
        if (!piece.TryGetComponent<Rigidbody>(out Rigidbody rb)) return;

        rb.AddForce(forceDir * _sliceForce, ForceMode.VelocityChange);

        Vector3 torqueAxis = Vector3.Cross(normalDir, forceDir);
        if (torqueAxis.sqrMagnitude > 0.0001f)
            rb.AddTorque(torqueAxis.normalized * (_sliceForce * _torqueForceRatio), ForceMode.VelocityChange);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (SlicePlaneData slice in _pendingSlices)
        {
            Gizmos.DrawLine(slice.Origin, slice.Origin + slice.Normal * 0.5f);
            Gizmos.DrawWireSphere(slice.Origin, 0.05f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(slice.Origin, slice.Origin + slice.Direction * 0.5f);
            Gizmos.color = Color.cyan;
        }
    }
}

public readonly struct SlicePlaneData
{
    public readonly Vector3 Origin;
    public readonly Vector3 Normal;
    public readonly Vector3 Direction;

    public SlicePlaneData(Vector3 origin, Vector3 normal, Vector3 direction)
    {
        Origin = origin;
        Normal = normal.normalized;
        Direction = direction.normalized;
    }
}
