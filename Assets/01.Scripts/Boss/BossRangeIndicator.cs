using UnityEngine;

/// <summary>
/// 런타임에 공격 범위를 바닥 원형으로 표시.
/// BossProceduralAnimator.Awake에서 자동으로 추가됩니다.
/// </summary>
public class BossRangeIndicator : MonoBehaviour
{
    [Header("Circle")]
    [SerializeField] int   _segments  = 48;
    [SerializeField] float _lineWidth = 0.08f;
    [SerializeField] float _yOffset   = 0.05f;

    LineRenderer[] _pool;

    void Awake()
    {
        _pool = new LineRenderer[4];
        for (int i = 0; i < _pool.Length; i++)
        {
            var go = new GameObject($"RangeCircle_{i}") { hideFlags = HideFlags.HideInHierarchy };
            go.transform.SetParent(transform);

            var lr = go.AddComponent<LineRenderer>();
            lr.material              = BuildMaterial();
            lr.useWorldSpace         = true;
            lr.loop                  = true;
            lr.positionCount         = _segments;
            lr.startWidth            = _lineWidth;
            lr.endWidth              = _lineWidth;
            lr.shadowCastingMode     = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows        = false;
            lr.enabled               = false;
            _pool[i] = lr;
        }
    }

    // 버텍스 컬러를 지원하는 셰이더를 RP에 관계없이 찾음
    static Material BuildMaterial()
    {
        // Hidden/Internal-Colored: Unity 내부 기즈모 셰이더, 모든 RP에서 버텍스 컬러 지원
        var shader = Shader.Find("Hidden/Internal-Colored");
        if (shader != null) return new Material(shader);

        shader = Shader.Find("Sprites/Default");
        if (shader != null) return new Material(shader);

        shader = Shader.Find("Unlit/Color");
        if (shader != null) return new Material(shader);

        Debug.LogWarning("[BossRangeIndicator] 적합한 셰이더를 찾지 못했습니다.");
        return new Material(Shader.Find("Standard"));
    }

    public void Show(int slot, Vector3 center, float radius, Color color)
    {
        if (_pool == null || (uint)slot >= _pool.Length) return;

        var lr = _pool[slot];

        // 머티리얼 색상 + 버텍스 컬러 양쪽 모두 설정 (셰이더에 따라 어느 쪽이 적용될지 다름)
        lr.material.color = color;
        lr.startColor     = color;
        lr.endColor       = color;

        float y = center.y + _yOffset;
        for (int i = 0; i < _segments; i++)
        {
            float a = i * Mathf.PI * 2f / _segments;
            lr.SetPosition(i, new Vector3(
                center.x + Mathf.Cos(a) * radius,
                y,
                center.z + Mathf.Sin(a) * radius));
        }
        lr.enabled = true;
    }

    public void Hide(int slot)
    {
        if (_pool != null && (uint)slot < _pool.Length)
            _pool[slot].enabled = false;
    }

    public void HideAll()
    {
        if (_pool == null) return;
        foreach (var lr in _pool) lr.enabled = false;
    }
}
