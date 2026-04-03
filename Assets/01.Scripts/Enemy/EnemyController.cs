using UnityEngine;

public enum EnemyType
{
    Roaming,     // 공중 순찰, 감지 시 어그로 범위 확장 + 전방위 조준 발사
    WallMounted  // 벽 고정, 전방위 조준 발사
}

public class EnemyController : MonoBehaviour
{
    [Header("적 종류")]
    public EnemyType EnemyType = EnemyType.Roaming;

    [Header("탐지 설정")]
    [Tooltip("기본 감지 반경")]
    public float DetectionRange = 10f;

    [Tooltip("플레이어 레이어 마스크 (선택 사항)")]
    public LayerMask PlayerLayer;

    [Header("Roaming 어그로 설정 (EnemyType.Roaming 전용)")]
    [Tooltip("플레이어 최초 감지 후 확장되는 감지 반경 (detectionRange보다 크게 설정)")]
    public float AggroDetectionRange = 15f;

    [Header("발사 설정")]
    [Tooltip("발사할 총알 프리팹 (BulletMover 컴포넌트 필요)")]
    public GameObject BulletPrefab;

    [Tooltip("총알이 발사될 위치 (총구)")]
    public Transform FirePoint;

    [Tooltip("초당 발사 횟수")]
    public float FireRate = 1f;

    [Tooltip("총알 속도")]
    public float BulletSpeed = 10f;

    [Header("포탑 회전 설정")]
    [Tooltip("포탑 머리 부분 Transform (없으면 오브젝트 자체가 회전)")]
    public Transform TurretHead;

    [Tooltip("회전 속도")]
    public float RotationSpeed = 5f;


    [Header("Roaming 순찰 설정 (EnemyType.Roaming 전용)")]
    [Tooltip("스폰 위치 기준 좌우 순찰 거리")]
    public float PatrolDistance = 5f;

    [Tooltip("순찰 이동 속도")]
    public float PatrolSpeed = 2f;

    // ── 내부 상태 ──────────────────────────────────
    private Transform _player;
    private float _nextFireTime;
    private bool _playerInRange;
    private bool _isAggroed;

    private Vector3 _patrolOrigin;
    private int _patrolDirection = 1;

    // ─────────────────────────────────────────────
    private void Start()
    {
        if (FirePoint == null) FirePoint = transform;
        if (TurretHead == null) TurretHead = transform;

        if (EnemyType == EnemyType.Roaming)
            _patrolOrigin = transform.position;
    }

    private void Update()
    {
        DetectPlayer();

        switch (EnemyType)
        {
            case EnemyType.Roaming:
                if (_playerInRange && _player != null)
                {
                    RotateTowardsPlayer();
                    TryFire();
                }
                else
                {
                    Patrol();
                }
                break;

            case EnemyType.WallMounted:
                if (_playerInRange && _player != null)
                {
                    RotateTowardsPlayer();
                    TryFire();
                }
                break;
        }
    }

    // ── 플레이어 감지 ──────────────────────────────
    private void DetectPlayer()
    {   //만약 어그로 상태면 감지범위가 aggroDetectionRange로 바뀜
        float currentRange = ( _isAggroed)
            ? AggroDetectionRange
            : DetectionRange;
        // 플레이어 레이어가 있으면 범위안에 플레이어 레이어만 찾음 아니면 전체 다 찾음, 전체 다 찾으면 성능 많이 먹음
        Collider[] hits = PlayerLayer != 0
            ? Physics.OverlapSphere(transform.position, currentRange, PlayerLayer)
            : Physics.OverlapSphere(transform.position, currentRange);

        _playerInRange = false;
        _player = null;
        /// 매 프레임 초기화후 탐색 player태그 가진 오브젝트 찾으면 저장하고 루프 종료 Enemy는 어그로 상태로 변경 
        foreach (Collider col in hits)
        {
            if (col.CompareTag("Player"))
            {
                _player = col.transform;
                _playerInRange = true;

                if (!_isAggroed)
                    _isAggroed = true;

                break;
            }
        }
        //어그로 해제 조건
        //어그로 범위 한번 더 확인, 이 범위 밖까지 완전히 벗어 났을 때만 어그로 해제

        if (_isAggroed && !_playerInRange)
        {
            Collider[] aggroHits = PlayerLayer != 0
                ? Physics.OverlapSphere(transform.position, AggroDetectionRange, PlayerLayer)
                : Physics.OverlapSphere(transform.position, AggroDetectionRange);

            bool stillInAggro = false;
            foreach (Collider col in aggroHits)
            {
                if (col.CompareTag("Player")) { stillInAggro = true; break; }
            }

            if (!stillInAggro) _isAggroed = false;
        }
    }

    // ── 전방위 조준 ────────────────────────────────
    private void RotateTowardsPlayer()
    {   //플레이어 방향의 벡터 구함
        Vector3 direction = (_player.position - TurretHead.position).normalized;

        //주어진 방향을 정면으로 바라봄
        if (direction != Vector3.zero)
        {
            TurretHead.rotation = Quaternion.Slerp(
                TurretHead.rotation,
                Quaternion.LookRotation(direction),
                RotationSpeed * Time.deltaTime
            );
        }
    }

    // ── 발사 ───────────────────────────────────────
    private void TryFire()
    {
        if (Time.time < _nextFireTime) return;

        if (BulletPrefab == null)
        {
            Debug.LogWarning("[EnemyController] bulletPrefab이 인스펙터에 할당되지 않았습니다.");
            return;
        }

        // firePoint 세팅과 무관하게 플레이어를 직접 겨냥하는 방향 계산
        Vector3 aimDir = (_player.position - FirePoint.position).normalized;
        //총알 프리팹 생성
        GameObject bullet = Instantiate(BulletPrefab, FirePoint.position, FirePoint.rotation);

        if (!bullet.TryGetComponent(out BulletMover mover))
            mover = bullet.AddComponent<BulletMover>();

        mover.Initialize(aimDir, BulletSpeed);

        _nextFireTime = Time.time + 1f / FireRate;
    }

    // ── Roaming 전용: 좌우 순찰 ────────────────────
    private void Patrol()
    {
        Vector3 target = _patrolOrigin + Vector3.right * (_patrolDirection * PatrolDistance);
        transform.position = Vector3.MoveTowards(transform.position, target, PatrolSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.05f)
            _patrolDirection *= -1;

    }

    // ── 기즈모 ─────────────────────────────────────
    private void OnDrawGizmosSelected()
    {   //플레이어 감지되면 레드 아니면 옐로우
        Gizmos.color = _playerInRange ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DetectionRange);

        if (EnemyType == EnemyType.Roaming)
        {   //어그로일때 색 바뀜
            Gizmos.color = _isAggroed ? new Color(1f, 0.5f, 0f) : new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, AggroDetectionRange);

            Gizmos.color = Color.green;

            // patrolOrigin은 start()에서 초기화 되기 때문에 실행전에는 값이 없어서 분기
            Vector3 origin = Application.isPlaying ? _patrolOrigin : transform.position;
            Gizmos.DrawLine(
                origin + Vector3.right * PatrolDistance,
                origin - Vector3.right * PatrolDistance
            );
        }
    }
}