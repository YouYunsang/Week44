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
    [SerializeField] private float _attackRange        = 2f;
    [SerializeField] private float _attackCooldown     = 0.5f;
    [SerializeField] private float _slowAttackCooldown = 0.2f;

    float _lastAttackTime = -999f;
    bool  _isSlowing;

    private void Awake()
    {
        // 같은 오브젝트의 슬라이스 실행기 캐싱
        if (_sliceExecutor == null)
            _sliceExecutor = GetComponent<PlayerSliceExecutor>();
    }

    private void OnEnable()
    {
        if (_input != null)
            _input.OnAttack += HandleLeftClick;
        EventBus<OnSlowGaugeChangedEvent>.Subscribe(OnSlowGaugeChanged);
    }

    private void OnDisable()
    {
        if (_input != null)
            _input.OnAttack -= HandleLeftClick;
        EventBus<OnSlowGaugeChangedEvent>.Unsubscribe(OnSlowGaugeChanged);
    }

    void OnSlowGaugeChanged(OnSlowGaugeChangedEvent e) => _isSlowing = e.isSlowing;

    //! 좌클릭 : 즉시 랜덤 슬라이스 
    private void HandleLeftClick()
    {
        if (_sliceExecutor == null) return;
        float cooldown = _isSlowing ? _slowAttackCooldown : _attackCooldown;
        if (Time.time - _lastAttackTime < cooldown) return;

        _lastAttackTime = Time.time;
        _sliceExecutor.TrySliceAtCrosshair(_attackRange);
    }
}