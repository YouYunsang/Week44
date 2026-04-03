using System.Collections;
using Assets.Scripts.SliceScripts;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private PlayerLook _playerLook;
    [SerializeField] private PlayerJump _playerJump;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private InputSO _input;

    [Header("Left Click Slice")]
    [SerializeField] private float _attackRange = 2f;

    [Header("Right Click Dash Slice")]
    [SerializeField] private float _minAttackRange = 2f;
    [SerializeField] private float _maxAttackRange = 10f;
    [SerializeField] private float _chargeSpeed = 3f;

    [Header("Dash Settings")]
    [SerializeField] private float _dashSpeed = 20f;
    [SerializeField] private float _dashTimeout = 1f;
    [SerializeField] private float _stopDistance = 1f;

    private static readonly int[] OBSTACLE_ALLOWED_DIRS = { 0, 1, 3, 4, 5, 7 };

    private float _currentRange = 2f;
    private bool _isCharging = false;
    private bool _isDashing = false;
    private bool _isTargetInRange = false;

    private void Awake()
    {
        // 자기 오브젝트의 필수 컴포넌트 캐싱
        if (_characterController == null)
            _characterController = GetComponent<CharacterController>();

        if (_playerMovement == null)
            _playerMovement = GetComponent<PlayerMovement>();

        if (_playerLook == null)
            _playerLook = GetComponent<PlayerLook>();

        if (_playerJump == null)
            _playerJump = GetComponent<PlayerJump>();

        // 메인 카메라 자동 연결
        if (_camera == null)
            _camera = Camera.main;

        // 기본 사거리 초기화
        _currentRange = _minAttackRange;
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

    private void Update()
    {
        // 대시 중에는 우클릭 충전/해제 처리 차단
        if (_isDashing)
            return;

        HandleRightClick();
    }

    private void HandleLeftClick()
    {
        // 실제 좌클릭 프레임인지 확인
        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (_camera == null)
            return;

        Ray ray = _camera.ScreenPointToRay(
            new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));

        // 화면 중앙 기준 슬라이스 판정
        if (Physics.Raycast(ray, out RaycastHit hit, _attackRange))
        {
            Sliceable sliceable = hit.collider.GetComponent<Sliceable>();
            if (sliceable == null)
                return;

            int randomDir = GetRandomDir(hit.collider);
            SliceObject(hit.collider.gameObject, hit.point, GetSliceNormal(randomDir));
        }
    }

    private void HandleRightClick()
    {
        if (_camera == null)
            return;

        // 우클릭 시작 시 충전 진입
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            _isCharging = true;
            _currentRange = _minAttackRange;
            _isTargetInRange = false;
        }

        // 우클릭 유지 중 충전 및 타겟 판정
        if (_isCharging && Mouse.current.rightButton.isPressed)
        {
            _currentRange = Mathf.Min(
                _currentRange + _chargeSpeed * Time.deltaTime,
                _maxAttackRange);

            Ray ray = _camera.ScreenPointToRay(
                new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));

            _isTargetInRange = Physics.Raycast(ray, out RaycastHit hit, _currentRange)
                               && hit.collider.GetComponent<Sliceable>() != null;
        }

        // 우클릭 해제 시 대시 슬라이스 시도
        if (_isCharging && Mouse.current.rightButton.wasReleasedThisFrame)
        {
            _isCharging = false;
            _isTargetInRange = false;
            TryDashAndSlice();
        }
    }

    private void TryDashAndSlice()
    {
        if (_camera == null)
            return;

        Ray ray = _camera.ScreenPointToRay(
            new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));

        if (Physics.Raycast(ray, out RaycastHit hit, _currentRange))
        {
            Sliceable sliceable = hit.collider.GetComponent<Sliceable>();
            if (sliceable != null)
            {
                StartCoroutine(DashAndSlice(hit));
                return;
            }
        }

        _currentRange = _minAttackRange;
    }

    private IEnumerator DashAndSlice(RaycastHit hit)
    {
        _isDashing = true;

        // 대시 중 이동 / 시야 회전 잠금
        if (_playerMovement != null)
            _playerMovement.SetMoveEnabled(false);

        if (_playerLook != null)
            _playerLook.SetLookEnabled(false);

        // 필요 시 점프도 차단
        if (_playerJump != null)
            _playerJump.enabled = false;

        // 카메라 정면 기준으로 멈출 위치 계산
        Vector3 targetPos = hit.point - _camera.transform.forward * _stopDistance;
        targetPos.y = _characterController.transform.position.y;

        float timer = 0f;

        while (timer < _dashTimeout)
        {
            Vector3 currentPos = _characterController.transform.position;

            float distance = Vector3.Distance(
                new Vector3(currentPos.x, 0f, currentPos.z),
                new Vector3(targetPos.x, 0f, targetPos.z));

            // 목표 지점에 충분히 가까우면 종료
            if (distance <= 0.2f)
                break;

            Vector3 moveDirection = (targetPos - currentPos).normalized;
            moveDirection.y = 0f;

            // CharacterController를 이용한 대시 이동
            _characterController.Move(moveDirection * _dashSpeed * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }

        ExecuteSlice(hit);

        // 대시 종료 후 조작 복구
        if (_playerMovement != null)
            _playerMovement.SetMoveEnabled(true);

        if (_playerLook != null)
            _playerLook.SetLookEnabled(true);

        if (_playerJump != null)
            _playerJump.enabled = true;

        _currentRange = _minAttackRange;
        _isDashing = false;
    }

    private void ExecuteSlice(RaycastHit hit)
    {
        if (hit.collider == null || hit.collider.gameObject == null)
            return;

        Sliceable sliceable = hit.collider.GetComponent<Sliceable>();
        if (sliceable == null)
            return;

        int randomDir = GetRandomDir(hit.collider);
        SliceObject(hit.collider.gameObject, hit.point, GetSliceNormal(randomDir));
    }

    private int GetRandomDir(Collider targetCollider)
    {
        // 장애물 태그는 일부 방향만 허용
        if (targetCollider.CompareTag("Obstical"))
            return OBSTACLE_ALLOWED_DIRS[Random.Range(0, OBSTACLE_ALLOWED_DIRS.Length)];

        return Random.Range(0, 8);
    }

    private Vector3 GetSliceNormal(int dirIndex)
    {
        float angle = dirIndex * 45f * Mathf.Deg2Rad;
        Vector2 swingDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        // 카메라 right / up 기준으로 슬라이스 평면 계산
        return (_camera.transform.right * (-swingDir.y)
              + _camera.transform.up * swingDir.x).normalized;
    }

    private void SliceObject(GameObject target, Vector3 hitPoint, Vector3 normal)
    {
        Vector3 transformedNormal =
            ((Vector3)(target.transform.localToWorldMatrix.transpose * normal)).normalized;

        Vector3 transformedPoint = target.transform.InverseTransformPoint(hitPoint);

        Plane plane = new Plane();
        plane.SetNormalAndPosition(transformedNormal, transformedPoint);

        if (Vector3.Dot(Vector3.up, transformedNormal) < 0f)
            plane = plane.flipped;

        GameObject[] slices = Slicer.Slice(plane, target);
        Destroy(target);

        Vector3 force = transformedNormal + Vector3.up * 2f;
        slices[0].GetComponent<Rigidbody>().AddForce(force * 3f, ForceMode.Impulse);
    }

    private void OnGUI()
    {
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        // 우클릭 충전 게이지 표시
        if (_isCharging)
        {
            float progress = (_currentRange - _minAttackRange)
                           / (_maxAttackRange - _minAttackRange);

            GUI.color = Color.gray;
            GUI.DrawTexture(
                new Rect(center.x - 75f, center.y + 40f, 150f, 10f),
                Texture2D.whiteTexture);

            GUI.color = _isTargetInRange
                ? Color.red
                : Color.Lerp(Color.white, Color.yellow, progress);

            GUI.DrawTexture(
                new Rect(center.x - 75f, center.y + 40f, 150f * progress, 10f),
                Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        // 대시 상태 텍스트 표시
        if (_isDashing)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            style.normal.textColor = Color.red;

            GUI.Label(
                new Rect(center.x - 100f, center.y - 60f, 200f, 40f),
                "DASH!",
                style);
        }

        // 조준점 표시
        float dotSize;
        Color dotColor;

        if (_isCharging && _isTargetInRange)
        {
            dotSize = 14f;
            dotColor = Color.red;
        }
        else if (_isCharging)
        {
            dotSize = Mathf.Lerp(
                6f,
                10f,
                (_currentRange - _minAttackRange) / (_maxAttackRange - _minAttackRange));

            dotColor = Color.white;
        }
        else
        {
            dotSize = 6f;
            dotColor = Color.white;
        }

        GUI.color = dotColor;
        GUI.DrawTexture(
            new Rect(center.x - dotSize * 0.5f, center.y - dotSize * 0.5f, dotSize, dotSize),
            Texture2D.whiteTexture);

        GUI.color = Color.white;
    }
}