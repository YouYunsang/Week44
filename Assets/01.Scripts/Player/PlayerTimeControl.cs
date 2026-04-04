using UnityEngine;

public class PlayerTimeControl : MonoBehaviour
{
    [Header("References")]
    [SerializeField] InputSO _input;

    [Header("Time")]
    [SerializeField] float _slowTimeScale  = 0.3f;
    [SerializeField] float _lerpDuration   = 0.3f;

    [Header("Gauge")]
    [SerializeField] float _maxGauge      = 3f;
    [SerializeField] float _drainRate     = 1f;   // 초당 차감량
    [SerializeField] float _rechargeRate  = 0.5f; // 초당 충전량
    [SerializeField] float _rechargeDelay = 1f;   // 해제 후 충전 시작까지 대기 시간

    float _gauge;
    bool  _isSlowing;
    bool  _menuOpen;
    float _rechargeTimer;

    void Awake()
    {
        _gauge = _maxGauge;
    }

    void OnEnable()
    {
        if (_input != null)
        {
            _input.OnSlowTimeStarted += HandleStarted;
            _input.OnSlowTimeStopped += HandleStopped;
        }
        EventBus<OnMenuOpenEvent>.Subscribe(OnMenuOpen);
        EventBus<OnMenuCloseEvent>.Subscribe(OnMenuClose);
    }

    void OnDisable()
    {
        if (_input != null)
        {
            _input.OnSlowTimeStarted -= HandleStarted;
            _input.OnSlowTimeStopped -= HandleStopped;
        }
        EventBus<OnMenuOpenEvent>.Unsubscribe(OnMenuOpen);
        EventBus<OnMenuCloseEvent>.Unsubscribe(OnMenuClose);
    }

    void Update()
    {
        if (_isSlowing)
        {
            DrainGauge();
        }
        else
        {
            RechargeGauge();
        }
    }

    void HandleStarted()
    {
        if (_menuOpen)        return;
        if (_gauge <= 0f)     return;

        _isSlowing     = true;
        _rechargeTimer = 0f;
        EventBus<OnLerpTimeScaleEvent>.Publish(new OnLerpTimeScaleEvent { target = _slowTimeScale, duration = _lerpDuration });
        PublishGauge();
    }

    void HandleStopped()
    {
        StopSlow();
    }

    void DrainGauge()
    {
        // 슬로우 중엔 실제 경과 시간 기준으로 차감 (unscaled)
        _gauge -= _drainRate * Time.unscaledDeltaTime;

        if (_gauge <= 0f)
        {
            _gauge = 0f;
            StopSlow();
        }

        PublishGauge();
    }

    void RechargeGauge()
    {
        if (_gauge >= _maxGauge) return;

        _rechargeTimer += Time.unscaledDeltaTime;
        if (_rechargeTimer < _rechargeDelay) return;

        _gauge = Mathf.Min(_gauge + _rechargeRate * Time.unscaledDeltaTime, _maxGauge);
        PublishGauge();
    }

    void StopSlow()
    {
        if (!_isSlowing) return;

        _isSlowing     = false;
        _rechargeTimer = 0f;
        EventBus<OnLerpTimeScaleEvent>.Publish(new OnLerpTimeScaleEvent { target = 1f, duration = _lerpDuration });
        PublishGauge();
    }

    void PublishGauge()
    {
        EventBus<OnSlowGaugeChangedEvent>.Publish(new OnSlowGaugeChangedEvent
        {
            current   = _gauge,
            max       = _maxGauge,
            isSlowing = _isSlowing
        });
    }

    void OnMenuOpen(OnMenuOpenEvent e)
    {
        _menuOpen = true;
        StopSlow();
    }

    void OnMenuClose(OnMenuCloseEvent e) => _menuOpen = false;
}
