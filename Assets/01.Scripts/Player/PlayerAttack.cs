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
    [SerializeField] private Camera _camera;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private PlayerLook _playerLook;
    [SerializeField] private PlayerJump _playerJump;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private InputSO _input;

    [Header("Data")]
    [SerializeField] private DashAttackDataSO _data;        //수치 데이터 SO
    [SerializeField] private float _attackRange = 2f;

    //? ---스택 상태 -------
    private int _currentStack = 0;
    private float _rechargeTimer = 0f;

    //? ---대시 충전 상태 -----
    private float _currentRange = 0f;
    private bool _isCharging = false;
    private bool _isDashing = false;
    private bool _isTargetInRange = false;
    private static readonly int[] OBSTACLE_ALLOWED_DIRS = { 0, 1, 3, 4, 5, 7 };

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
    }

    private void Start()
    {
        // 시작 시 스택 최대치로 초기화
        _currentStack = _data.MaxStack;
        _currentRange = _data.MinAttackRange;
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

        RechargeStack();        // 스택 자동 충전
        HandleRightClick();
    }

    private void RechargeStack()
    {
        if(_currentStack >= _data.MaxStack) return;

        _rechargeTimer += Time.deltaTime;

        // 충전 시간 초과 시 스택 1 추가
        if(_rechargeTimer >= _data.StackRechargeTime)
        {
            _currentStack = Mathf.Min(_currentStack + 1, _data.MaxStack);
            _rechargeTimer = 0f;
        }
    }

    //! 좌클릭 : 즉시 랜덤 슬라이스 
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
        if (!Physics.Raycast(ray, out RaycastHit hit, _attackRange)) return;
        
         Sliceable sliceable = hit.collider.GetComponent<Sliceable>();
        if (sliceable == null) return;

        int randomDir = GetRandomDir(hit.collider);
        SliceObject(hit.collider.gameObject, hit.point, GetSliceNormal(randomDir));
        
    }

    //! 우클릭 : 스택 소비 후 대시 슬라이스
    private void HandleRightClick()
    {
        if (_camera == null)
            return;

        // 우클릭 시작 시 충전 진입
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if(_currentStack <= 0) return;

            _isCharging = true;
            _currentRange = _data.MinAttackRange;
            _isTargetInRange = false;
        }

        // 우클릭 유지 중 충전 및 타겟 판정
        if (_isCharging && Mouse.current.rightButton.isPressed)
        {
            _currentRange = Mathf.Min(
                _currentRange + _data.ChargeSpeed * Time.deltaTime,
                _data.MaxAttackRange);

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
                ConsumeStack();
                StartCoroutine(DashAndSlice(hit));
                return;
            }
        }

        _currentRange = _data.MinAttackRange;
    }

    private void ConsumeStack()
    {
        _currentStack = Mathf.Max(_currentStack - 1, 0);
        _rechargeTimer = 0f;        //소비 시 충전 타이머 리셋
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
        Vector3 targetPos = hit.point - _camera.transform.forward * _data.StopDistance;

        float timer = 0f;

        while (timer < _data.DashTimeout)
        {
            Vector3 currentPos = _characterController.transform.position;

            float distance = Vector3.Distance(currentPos, targetPos);

            // 목표 지점에 충분히 가까우면 종료
            if (distance <= 0.2f)
                break;

            Vector3 moveDir = (targetPos - currentPos).normalized;

            // CharacterController를 이용한 대시 이동
            _characterController.Move(moveDir * _data.DashSpeed * Time.deltaTime);

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

        _currentRange = _data.MinAttackRange;
        _isDashing = false;
    }

    //! 슬라이스 실행
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

    //* GUI ------------------------------------------
    private void OnGUI()
    {
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

       DrawStackUI(center);
       DrawChargeGauge(center);
       DrawDashLabel(center);
       DrawCrosshair(center);
    }

    private void DrawStackUI(Vector2 center)
    {
        // 스택 아이콘 표시 (● 채움 / ○ 비움)
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        // 충전 중 스택은 노란색, 보유 스택은 흰색, 빈 스택은 회색
        String stackDisplay = "";
        for(int i = 0; i < _data.MaxStack; i++)
        {
            if (i < _currentStack)
                stackDisplay += "<color=white>●</color> ";
            else if (i == _currentStack && _currentStack < _data.MaxStack)
                stackDisplay += "<color=yellow>●</color> ";
            else
                stackDisplay += "<color=grey>○</color> ";
        }

        style.richText = true;
        GUI.Label(new Rect(center.x - 60f, center.y + 60f, 120f, 30f), stackDisplay, style);

        // 충전 진행 게이지 (다음 스택까지 남은 시간)
        if (_currentStack < _data.MaxStack)
        {
            float rechargeProgress = _rechargeTimer / _data.StackRechargeTime;

            GUI.color = Color.gray;
            GUI.DrawTexture(
                new Rect(center.x - 40f, center.y + 95f, 80f, 6f),
                Texture2D.whiteTexture);

            GUI.color = Color.yellow;
            GUI.DrawTexture(
                new Rect(center.x - 40f, center.y + 95f, 80f * rechargeProgress, 6f),
                Texture2D.whiteTexture);

            GUI.color = Color.white;
        }
    }

    private void DrawChargeGauge(Vector2 center)
    {
        if (!_isCharging) return;

        float progress = (_currentRange - _data.MinAttackRange)
                       / (_data.MaxAttackRange - _data.MinAttackRange);

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

    private void DrawDashLabel(Vector2 center)
    {
        if (!_isDashing) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = Color.red;

        GUI.Label(
            new Rect(center.x - 100f, center.y - 60f, 200f, 40f),
            "DASH!", style);
    }

    private void DrawCrosshair(Vector2 center)
    {
        float dotSize;
        Color dotColor;

        if (_isCharging && _isTargetInRange)
        {
            dotSize  = 14f;
            dotColor = Color.red;
        }
        else if (_isCharging)
        {
            dotSize = Mathf.Lerp(
                6f, 10f,
                (_currentRange - _data.MinAttackRange)
              / (_data.MaxAttackRange - _data.MinAttackRange));
            dotColor = Color.white;
        }
        else if (_currentStack <= 0)
        {
            // 스택 없으면 조준점 회색
            dotSize  = 6f;
            dotColor = Color.gray;
        }
        else
        {
            dotSize  = 6f;
            dotColor = Color.white;
        }

        GUI.color = dotColor;
        GUI.DrawTexture(
            new Rect(center.x - dotSize * 0.5f,
                     center.y - dotSize * 0.5f,
                     dotSize, dotSize),
            Texture2D.whiteTexture);

        GUI.color = Color.white;
    }
}