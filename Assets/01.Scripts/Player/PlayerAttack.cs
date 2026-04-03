using UnityEngine;
using UnityEngine.InputSystem;
using Assets.Scripts.SliceScripts;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    public Camera cam;

    [Header("좌클릭 슬라이스")]
    public float attackRange = 2f;

    [Header("우클릭 대시 슬라이스")]
    public float minAttackRange = 2f;
    public float maxAttackRange = 10f;
    public float chargeSpeed    = 3f;

    [Header("대시 설정")]
    public float dashSpeed    = 20f;
    public float dashTimeout  = 1f;
    public float stopDistance = 1f;

    [Header("연결")]
    public FirstPersonController fpsController;
    public PlayerJump            playerJump;
    public CharacterController   characterController;
    public InputSO input;

    private readonly int[] _obsticalAllowedDirs = { 0, 1, 3, 4, 5, 7 };

    private float _currentRange     = 2f;
    private bool  _isCharging       = false;
    private bool  _isDashing        = false;
    private bool  _isTargetInRange  = false;

    void OnEnable()
    {
        input.OnAttack += HandleLeftClick;
    }

    void OnDisable()
    {
        input.OnAttack -= HandleLeftClick;    
    }



    void Update()
    {
        if (_isDashing) return;
        HandleRightClick();
    }

    // 좌클릭 -> 랜덤 공격
    void HandleLeftClick()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Ray ray = cam.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f));

        if (Physics.Raycast(ray, out RaycastHit hit, attackRange))
        {
            Sliceable sliceable = hit.collider.GetComponent<Sliceable>();
            if (sliceable == null) return;

            int randomDir = GetRandomDir(hit.collider);
            SliceObject(hit.collider.gameObject, hit.point, GetSliceNormal(randomDir));
        }
    }

    // 우클릭 -> 대시 공격
    void HandleRightClick()
    {
        // 우클릭 충전
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            _isCharging      = true;
            _currentRange    = minAttackRange;
            _isTargetInRange = false;
        }

        // 우클릭 유지 → 사거리 증가 + 범위 체크
        if (_isCharging && Mouse.current.rightButton.isPressed)
        {
            _currentRange = Mathf.Min(
                _currentRange + chargeSpeed * Time.deltaTime,
                maxAttackRange);

            Ray ray = cam.ScreenPointToRay(
                new Vector3(Screen.width / 2f, Screen.height / 2f));

            _isTargetInRange = Physics.Raycast(ray, out RaycastHit hit, _currentRange)
                               && hit.collider.GetComponent<Sliceable>() != null;
        }

        // 우클릭 뗌 → 대시 슬라이스 실행
        if (_isCharging && Mouse.current.rightButton.wasReleasedThisFrame)
        {
            _isCharging      = false;
            _isTargetInRange = false;
            TryDashAndSlice();
        }
    }

    void TryDashAndSlice()
    {
        Ray ray = cam.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f));

        if (Physics.Raycast(ray, out RaycastHit hit, _currentRange))
        {
            Sliceable sliceable = hit.collider.GetComponent<Sliceable>();
            if (sliceable != null)
            {
                StartCoroutine(DashAndSlice(hit));
                return;
            }
        }

        _currentRange = minAttackRange;
    }

    IEnumerator DashAndSlice(RaycastHit hit)
    {
        _isDashing = true;

        if (fpsController != null) fpsController.enabled = false;
        if (playerJump    != null) playerJump.enabled    = false;

        Vector3 targetPos = hit.point - cam.transform.forward * stopDistance;
        targetPos.y       = characterController.transform.position.y;

        float timer = 0f;

        while (timer < dashTimeout)
        {
            float dist = Vector3.Distance(
                new Vector3(characterController.transform.position.x, 0f,
                            characterController.transform.position.z),
                new Vector3(targetPos.x, 0f, targetPos.z));

            if (dist <= 0.2f) break;

            Vector3 dir = (targetPos - characterController.transform.position).normalized;
            dir.y = 0f;

            characterController.Move(dir * dashSpeed * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }

        ExecuteSlice(hit);

        if (fpsController != null) fpsController.enabled = true;
        if (playerJump    != null) playerJump.enabled    = true;

        _currentRange = minAttackRange;
        _isDashing    = false;
    }

    void ExecuteSlice(RaycastHit hit)
    {
        if (hit.collider == null || hit.collider.gameObject == null) return;

        Sliceable sliceable = hit.collider.GetComponent<Sliceable>();
        if (sliceable == null) return;

        int randomDir = GetRandomDir(hit.collider);
        SliceObject(hit.collider.gameObject, hit.point, GetSliceNormal(randomDir));
    }

    // Obstical 태그 여부에 따라 허용 방향 선택
    int GetRandomDir(Collider col)
    {
        if (col.CompareTag("Obstical"))
        {
            return _obsticalAllowedDirs[Random.Range(0, _obsticalAllowedDirs.Length)];
        }
        return Random.Range(0, 8);
    }

    Vector3 GetSliceNormal(int dirIndex)
    {
        float   angle    = dirIndex * 45f * Mathf.Deg2Rad;
        Vector2 swingDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        return (cam.transform.right * (-swingDir.y)
              + cam.transform.up   * ( swingDir.x)).normalized;
    }

    void SliceObject(GameObject target, Vector3 hitPoint, Vector3 normal)
    {
        Vector3 transformedNormal = ((Vector3)(
            target.transform.localToWorldMatrix.transpose * normal)).normalized;

        Vector3 transformedPoint =
            target.transform.InverseTransformPoint(hitPoint);

        Plane plane = new Plane();
        plane.SetNormalAndPosition(transformedNormal, transformedPoint);

        if (Vector3.Dot(Vector3.up, transformedNormal) < 0)
            plane = plane.flipped;

        GameObject[] slices = Slicer.Slice(plane, target);
        Destroy(target);

        Vector3 force = transformedNormal + Vector3.up * 2f;
        slices[0].GetComponent<Rigidbody>().AddForce(force * 3f, ForceMode.Impulse);
    }

    void OnGUI()
    {
        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // 우클릭 충전 게이지
        if (_isCharging)
        {
            float progress = (_currentRange - minAttackRange)
                           / (maxAttackRange - minAttackRange);

            GUI.color = Color.gray;
            GUI.DrawTexture(new Rect(center.x - 75f, center.y + 40f, 150f, 10f),
                Texture2D.whiteTexture);

            GUI.color = _isTargetInRange
                ? Color.red
                : Color.Lerp(Color.white, Color.yellow, progress);
            GUI.DrawTexture(new Rect(center.x - 75f, center.y + 40f, 150f * progress, 10f),
                Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        // 대시 중 표시
        if (_isDashing)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(center.x - 100f, center.y - 60f, 200f, 40f),
                "DASH!", style);
        }

        // 조준점
        float dotSize;
        Color dotColor;

        if (_isCharging && _isTargetInRange)
        {
            dotSize  = 14f;
            dotColor = Color.red;
        }
        else if (_isCharging)
        {
            dotSize  = Mathf.Lerp(6f, 10f,
                (_currentRange - minAttackRange) / (maxAttackRange - minAttackRange));
            dotColor = Color.white;
        }
        else
        {
            dotSize  = 6f;
            dotColor = Color.white;
        }

        GUI.color = dotColor;
        GUI.DrawTexture(
            new Rect(center.x - dotSize / 2f, center.y - dotSize / 2f, dotSize, dotSize),
            Texture2D.whiteTexture);

        GUI.color = Color.white;
    }
}