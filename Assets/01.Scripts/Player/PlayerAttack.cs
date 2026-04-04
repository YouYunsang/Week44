using System;
using System.Collections;
using System.Data.Common;
using Assets.Scripts.SliceScripts;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputSO _input;
    [SerializeField] private PlayerSliceExecutor _sliceExecutor;

    [Header("Attack Settings")]
    [SerializeField] private float _attackRange = 2f;

    private void Awake()
    {
        // 같은 오브젝트의 슬라이스 실행기 캐싱
        if (_sliceExecutor == null)
            _sliceExecutor = GetComponent<PlayerSliceExecutor>();
    }

    private void OnEnable()
    {
        // 좌클릭 공격 입력 구독
        if (_input != null)
            _input.OnAttack += HandleLeftClick;
    }

    private void OnDisable()
    {
        // 좌클릭 공격 입력 구독 해제
        if (_input != null)
            _input.OnAttack -= HandleLeftClick;
    }

    //! 좌클릭 : 즉시 랜덤 슬라이스 
    private void HandleLeftClick()
    {
        // 일반 공격 사거리로 슬라이스 시도
        if (_sliceExecutor == null)
            return;

        _sliceExecutor.TrySliceAtCrosshair(_attackRange);
    }
}