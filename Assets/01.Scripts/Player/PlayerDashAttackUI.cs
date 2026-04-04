using UnityEngine;

public class PlayerDashAttackUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerDashAttack _playerDashAttack;
    [SerializeField] private DashAttackDataSO _dashAttackData;
    [SerializeField] private PlayerSliceExecutor _sliceExecutor;
    [SerializeField] private float _basicAttackRange = 2f;

    [Header("Layout")]
    [SerializeField] private Vector2 _stackOffset = new Vector2(0f, 60f);
    [SerializeField] private Vector2 _stackRechargeBarOffset = new Vector2(0f, 95f);
    [SerializeField] private Vector2 _chargeGaugeOffset = new Vector2(0f, 40f);
    [SerializeField] private Vector2 _dashLabelOffset = new Vector2(0f, -60f);

    [Header("Sizes")]
    [SerializeField] private float _stackLabelWidth = 120f;
    [SerializeField] private float _stackLabelHeight = 30f;
    [SerializeField] private float _stackRechargeBarWidth = 80f;
    [SerializeField] private float _stackRechargeBarHeight = 6f;
    [SerializeField] private float _chargeGaugeWidth = 150f;
    [SerializeField] private float _chargeGaugeHeight = 10f;
    [SerializeField] private float _dashLabelWidth = 200f;
    [SerializeField] private float _dashLabelHeight = 40f;

    [Header("Crosshair")]
    [SerializeField] private float _defaultCrosshairSize = 6f;
    [SerializeField] private float _maxChargeCrosshairSize = 10f;
    [SerializeField] private float _targetCrosshairSize = 14f;

    private void Awake()
    {
        // 같은 오브젝트 내부 참조 캐싱
        if (_playerDashAttack == null)
            _playerDashAttack = GetComponent<PlayerDashAttack>();
    }

    private bool IsBasicTargetInRange()
    {
        if(_sliceExecutor == null) return false;
        return _sliceExecutor.TryGetSliceHit(_basicAttackRange, out _);
    }

    private void OnGUI()
    {
        if (_playerDashAttack == null || _dashAttackData == null)
            return;

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        DrawStackUI(center);
        DrawChargeGauge(center);
        DrawDashLabel(center);
        DrawCrosshair(center);
    }

    private void DrawStackUI(Vector2 center)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };

        string stackDisplay = string.Empty;

        for (int i = 0; i < _dashAttackData.MaxStack; i++)
        {
            if (i < _playerDashAttack.CurrentStack)
            {
                // 보유 중인 스택
                stackDisplay += "<color=white>●</color> ";
            }
            else
            {
                // 비어 있는 스택
                stackDisplay += "<color=grey>○</color> ";
            }
        }

        Rect stackRect = new Rect(
            center.x - _stackLabelWidth * 0.5f + _stackOffset.x,
            center.y + _stackOffset.y,
            _stackLabelWidth,
            _stackLabelHeight);

        GUI.Label(stackRect, stackDisplay, style);

        if (_playerDashAttack.CurrentStack >= _dashAttackData.MaxStack)
            return;

        float rechargeProgress = _playerDashAttack.RechargeNormalized;

        GUI.color = Color.gray;
        GUI.DrawTexture(
            new Rect(
                center.x - _stackRechargeBarWidth * 0.5f + _stackRechargeBarOffset.x,
                center.y + _stackRechargeBarOffset.y,
                _stackRechargeBarWidth,
                _stackRechargeBarHeight),
            Texture2D.whiteTexture);

        GUI.color = Color.yellow;
        GUI.DrawTexture(
            new Rect(
                center.x - _stackRechargeBarWidth * 0.5f + _stackRechargeBarOffset.x,
                center.y + _stackRechargeBarOffset.y,
                _stackRechargeBarWidth * rechargeProgress,
                _stackRechargeBarHeight),
            Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    private void DrawChargeGauge(Vector2 center)
    {
        if (!_playerDashAttack.IsCharging)
            return;

        float progress = _playerDashAttack.ChargeNormalized;

        GUI.color = Color.gray;
        GUI.DrawTexture(
            new Rect(
                center.x - _chargeGaugeWidth * 0.5f + _chargeGaugeOffset.x,
                center.y + _chargeGaugeOffset.y,
                _chargeGaugeWidth,
                _chargeGaugeHeight),
            Texture2D.whiteTexture);
        
        GUI.color = (_playerDashAttack.IsTargetInRange && !_playerDashAttack.IsTargetBullet)
            ? Color.red
            : Color.Lerp(Color.white, Color.yellow, progress);

        GUI.DrawTexture(
            new Rect(
                center.x - _chargeGaugeWidth * 0.5f + _chargeGaugeOffset.x,
                center.y + _chargeGaugeOffset.y,
                _chargeGaugeWidth * progress,
                _chargeGaugeHeight),
            Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    private void DrawDashLabel(Vector2 center)
    {
        if (!_playerDashAttack.IsDashing)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = Color.red;

        Rect dashRect = new Rect(
            center.x - _dashLabelWidth * 0.5f + _dashLabelOffset.x,
            center.y + _dashLabelOffset.y,
            _dashLabelWidth,
            _dashLabelHeight);

        GUI.Label(dashRect, "DASH!", style);
    }

    private void DrawCrosshair(Vector2 center)
    {
        float dotSize = _defaultCrosshairSize;
        Color dotColor = Color.white;

        if(_playerDashAttack.IsCharging)
        {
            dotSize = Mathf.Lerp(
            _defaultCrosshairSize,
            _maxChargeCrosshairSize,
            _playerDashAttack.ChargeNormalized);

            dotColor = (_playerDashAttack.IsTargetInRange && !_playerDashAttack.IsTargetBullet)
            ? Color.red
            : Color.white;
        }
        else
        {
            if(_playerDashAttack.CurrentStack <= 0)
            {
                dotColor = Color.gray;
            }
            else
            {
                dotColor = IsBasicTargetInRange()
                ? Color.red
                : Color.white;    
            }
            
        }

        GUI.color = dotColor;
        GUI.DrawTexture(
            new Rect(
                center.x - dotSize * 0.5f,
                center.y - dotSize * 0.5f,
                dotSize,
                dotSize),
            Texture2D.whiteTexture);

        GUI.color = Color.white;
    }
}