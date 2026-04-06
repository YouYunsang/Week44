using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 공격 범위를 3D 와이어프레임으로 표시.
///
/// 슬롯당 LineRenderer 2개 사용:
///   [0] 메인  — 박스 전체 엣지 / 원기둥 바닥링 + 수직선
///   [1] 보조  — 원기둥 상단링 (ShowRect는 사용 안 함)
/// </summary>
public class BossRangeIndicator : MonoBehaviour
{
    [Header("Style")]
    [SerializeField] float _lineWidth      = 0.08f;
    [SerializeField] float _yOffset        = 0.05f;

    [Header("Cylinder (ShowCircle)")]
    [SerializeField] float _cylinderHeight = 4f;  // 원기둥 높이
    [SerializeField] int   _cylinderSpokes = 8;   // 수직선 개수

    // 슬롯 × 2개 LineRenderer
    LineRenderer[][] _pool;

    const int SLOTS = 4;
    const int SEG   = 32; // 원호 분할 수

    void Awake()
    {
        _pool = new LineRenderer[SLOTS][];
        for (int s = 0; s < SLOTS; s++)
        {
            _pool[s] = new LineRenderer[2];
            for (int j = 0; j < 2; j++)
            {
                var go = new GameObject($"Range_{s}_{j}") { hideFlags = HideFlags.HideInHierarchy };
                go.transform.SetParent(transform);
                var lr              = go.AddComponent<LineRenderer>();
                lr.material         = BuildMaterial();
                lr.useWorldSpace    = true;
                lr.loop             = false;
                lr.positionCount    = 2;
                lr.startWidth       = _lineWidth;
                lr.endWidth         = _lineWidth;
                lr.shadowCastingMode= UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows   = false;
                lr.enabled          = false;
                _pool[s][j] = lr;
            }
        }
    }

    // ── 박스 (3D 와이어프레임) ────────────────────────────────────────────────
    /// <summary>
    /// 직육면체 범위 표시.
    /// origin: 박스 뒷면 하단 중앙, forward: 공격 방향, size: (너비, 높이, 깊이).
    /// </summary>
    public void ShowRect(int slot, Vector3 origin, Vector3 forward, Vector3 size, Color color)
    {
        if (_pool == null || (uint)slot >= _pool.Length) return;

        _pool[slot][1].enabled = false; // 원기둥용 상단링 비활성화

        var lr = _pool[slot][0];
        Apply(lr, color, false, 16);

        Vector3 fwd   = new Vector3(forward.x, 0f, forward.z).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        float   hw    = size.x * 0.5f;
        float   yB    = origin.y + _yOffset;
        float   yT    = yB + size.y;

        // 8개 꼭짓점 (B=뒤, F=앞, L=왼, R=오른, B_=바닥, T=천장)
        Vector3 BLB = new Vector3(origin.x, yB, origin.z) - right * hw;
        Vector3 BRB = new Vector3(origin.x, yB, origin.z) + right * hw;
        Vector3 FLB = BLB + fwd * size.z;
        Vector3 FRB = BRB + fwd * size.z;
        Vector3 BLT = new Vector3(BLB.x, yT, BLB.z);
        Vector3 BRT = new Vector3(BRB.x, yT, BRB.z);
        Vector3 FLT = new Vector3(FLB.x, yT, FLB.z);
        Vector3 FRT = new Vector3(FRB.x, yT, FRB.z);

        // 16포인트 단일 경로로 12개 엣지 전부 커버
        lr.SetPosition(0,  BLB); lr.SetPosition(1,  BRB);
        lr.SetPosition(2,  FRB); lr.SetPosition(3,  FLB);
        lr.SetPosition(4,  BLB); lr.SetPosition(5,  BLT);
        lr.SetPosition(6,  BRT); lr.SetPosition(7,  BRB);
        lr.SetPosition(8,  BRT); lr.SetPosition(9,  FRT);
        lr.SetPosition(10, FRB); lr.SetPosition(11, FRT);
        lr.SetPosition(12, FLT); lr.SetPosition(13, FLB);
        lr.SetPosition(14, FLT); lr.SetPosition(15, BLT);

        lr.enabled = true;
    }

    // ── 원기둥 ────────────────────────────────────────────────────────────────
    /// <summary>
    /// 원기둥 범위 표시.
    /// center: 원 중심 (바닥 기준), radius: 반지름.
    /// 높이·수직선 개수는 인스펙터 _cylinderHeight / _cylinderSpokes로 설정.
    /// </summary>
    public void ShowCircle(int slot, Vector3 center, float radius, Color color)
    {
        if (_pool == null || (uint)slot >= _pool.Length) return;

        float botY = center.y + _yOffset;
        float topY = botY + _cylinderHeight;

        // [0]: 바닥링 + 수직선 (스포크)
        DrawBottomWithSpokes(_pool[slot][0], center, radius, botY, topY, color);

        // [1]: 상단링
        DrawRing(_pool[slot][1], center, radius, topY, color);
    }

    // ── Hide ─────────────────────────────────────────────────────────────────
    public void Hide(int slot)
    {
        if (_pool == null || (uint)slot >= _pool.Length) return;
        _pool[slot][0].enabled = false;
        _pool[slot][1].enabled = false;
    }

    public void HideAll()
    {
        if (_pool == null) return;
        foreach (var s in _pool) { s[0].enabled = false; s[1].enabled = false; }
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 바닥 링을 그리되, 각 스포크 위치에서 상단까지 올라갔다 내려오는 경로를 삽입.
    /// 결과: 바닥 원 + N개의 수직선이 하나의 연속된 경로로 표현됨.
    /// </summary>
    void DrawBottomWithSpokes(LineRenderer lr, Vector3 center, float radius,
        float botY, float topY, Color color)
    {
        int period = Mathf.Max(1, SEG / Mathf.Max(1, _cylinderSpokes));

        // 점 개수: 바닥 SEG개 + 스포크마다 2개 추가(올라감/내려옴) + 닫기 1개
        int spokeCount = SEG / period;
        var pts = new List<Vector3>(SEG + spokeCount * 2 + 1);

        for (int i = 0; i < SEG; i++)
        {
            float a  = i * Mathf.PI * 2f / SEG;
            float cx = center.x + Mathf.Cos(a) * radius;
            float cz = center.z + Mathf.Sin(a) * radius;

            pts.Add(new Vector3(cx, botY, cz));

            // 스포크 위치: 위로 올라갔다가 다시 바닥으로
            if (i % period == 0)
            {
                pts.Add(new Vector3(cx, topY, cz));
                pts.Add(new Vector3(cx, botY, cz));
            }
        }

        // 바닥 링 닫기 (시작점으로 복귀)
        pts.Add(new Vector3(center.x + radius, botY, center.z));

        Apply(lr, color, false, pts.Count);
        lr.SetPositions(pts.ToArray());
        lr.enabled = true;
    }

    /// <summary>loop=true로 원 하나를 그림 (상단링 전용)</summary>
    void DrawRing(LineRenderer lr, Vector3 center, float radius, float y, Color color)
    {
        Apply(lr, color, true, SEG);
        for (int i = 0; i < SEG; i++)
        {
            float a = i * Mathf.PI * 2f / SEG;
            lr.SetPosition(i, new Vector3(
                center.x + Mathf.Cos(a) * radius,
                y,
                center.z + Mathf.Sin(a) * radius));
        }
        lr.enabled = true;
    }

    /// <summary>색상·loop·positionCount를 한 번에 적용</summary>
    static void Apply(LineRenderer lr, Color color, bool loop, int count)
    {
        lr.material.color = color;
        lr.startColor     = color;
        lr.endColor       = color;
        lr.loop           = loop;
        lr.positionCount  = count;
    }

    static Material BuildMaterial()
    {
        var shader = Shader.Find("Hidden/Internal-Colored");
        if (shader != null) return new Material(shader);

        shader = Shader.Find("Sprites/Default");
        if (shader != null) return new Material(shader);

        shader = Shader.Find("Unlit/Color");
        if (shader != null) return new Material(shader);

        Debug.LogWarning("[BossRangeIndicator] 적합한 셰이더를 찾지 못했습니다.");
        return new Material(Shader.Find("Standard"));
    }
}
