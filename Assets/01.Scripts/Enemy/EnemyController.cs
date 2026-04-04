using System.Runtime.InteropServices;
using UnityEngine;

public enum EnemyType
{
    Roaming,     // 공중 순찰, 감지 시 어그로 범위 확장 + 전방위 조준 발사
    WallMounted  // 벽 고정, 전방위 조준 발사
}

public enum PatrolAxis
{
    X,      // 좌우 (기본)
    Z,      // 앞뒤
    Custom  // 인스펙터에서 직접 방향 지정
}

public class EnemyController : MonoBehaviour
{
    [Header("시야 설정")]
    [Tooltip("플레이어와 적 사이를 막는 레이어 (벽, 장애물 등)")]
    public LayerMask ObstacleLayer;

    [Tooltip("시야 높이 오프셋 (눈 위치 보정)")]
    public float EyeHeight = 1f;

    [Header("적 종류")]
    public EnemyType EnemyType = EnemyType.Roaming;

    [Header("탐지 설정")]
    [Tooltip("기본 감지 반경")]
    public float DetectionRange = 10f;

    [Tooltip("플레이어 레이어 마스크 (선택 사항)")]
    public LayerMask PlayerLayer;

    [Header("Roaming 어그로 설정 (EnemyType.Roaming 전용)")]
    [Tooltip("플레이어 최초 감지 후 확장되는 감지 반경 (DetectionRange보다 크게 설정)")]
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

    [Tooltip("플레이어 감지 후 첫 발사까지 선딜레이 (초)")]
    public float FirstfireDelay = 2f;      

    [Header("포탑 회전 설정")]
    [Tooltip("포탑 머리 부분 Transform (없으면 오브젝트 자체가 회전)")]
    public Transform TurretHead;

    [Tooltip("회전 속도")]
    public float RotationSpeed = 5f;

    [Header("Roaming 순찰 설정 (EnemyType.Roaming 전용)")]
    [Tooltip("순찰 축 방향")]
    public PatrolAxis PatrolAxis = PatrolAxis.X;

    [Tooltip("PatrolAxis.Custom 선택 시 순찰 방향 벡터 (자동 정규화됨)")]
    public Vector3 CustomPatrolDirection = Vector3.right;

    [Tooltip("스폰 위치 기준 순찰 거리")]
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
    private Vector3 _patrolDir; // 실제 사용할 정규화된 순찰 방향
    private bool _isFiringDelayed = false;      //선딜레이 코루틴 실행 중 여부

    // ─────────────────────────────────────────────
    private void Start()
    {
        if (FirePoint == null) FirePoint = transform;
        if (TurretHead == null) TurretHead = transform;

        if (EnemyType == EnemyType.Roaming)
        {
            _patrolOrigin = transform.position;

            // 선택한 축에 따라 순찰 방향 결정
            _patrolDir = PatrolAxis switch
            {
                PatrolAxis.X => Vector3.right,
                PatrolAxis.Z => Vector3.forward,
                PatrolAxis.Custom => CustomPatrolDirection.normalized,
                _ => Vector3.right
            };
        }
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
    {   //만약 어그로 상태면 감지범위가 AggroDetectionRange로 바뀜
        float currentRange = _isAggroed ? AggroDetectionRange : DetectionRange;

        // 플레이어 레이어가 있으면 범위안에 플레이어 레이어만 찾음 아니면 전체 다 찾음, 전체 다 찾으면 성능 많이 먹음
        Collider[] hits = PlayerLayer != 0
            ? Physics.OverlapSphere(transform.position, currentRange, PlayerLayer)
            : Physics.OverlapSphere(transform.position, currentRange);

        _playerInRange = false;
        _player = null;

        /// 매 프레임 초기화후 탐색 player태그 가진 오브젝트 찾으면 저장하고 루프 종료 Enemy는 어그로 상태로 변경
        foreach (Collider col in hits)
        {
            if (!col.CompareTag("Player")) continue;

            if(!HasLineOfSight(col.transform)) continue;

            _player = col.transform;
            _playerInRange = true;

            if(!_isAggroed)
                _isAggroed = true;
            
            break;
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
                if (!col.CompareTag("Player")) continue;

                if(HasLineOfSight(col.transform))
                {
                    stillInAggro = true;
                    break;
                }
            }

            if (!stillInAggro) _isAggroed = false;
        }
    }

    // 플레이어와 적 사이에 장애물잉 없는지 확인
    private bool HasLineOfSight(Transform target)
    {
        //눈 위치 보정
        Vector3 eyePos = transform.position + Vector3.up * EyeHeight;
        Vector3 targetPos = target.position + Vector3.up * EyeHeight;
        Vector3 direction = (targetPos - eyePos).normalized;
        float distance = Vector3.Distance(eyePos, targetPos);

        //장애물 레이어가 설정되어 있으면 해당 레이어만 체크
        //없으면 모든 레이어 체크
        if(ObstacleLayer != 0)
        {
            return !Physics.Raycast(eyePos, direction, distance, ObstacleLayer);
        }
        else
        {
            //ObstacleLayer 미설정 시 플레이어 레이어 제외하고 체크
            if(Physics.Raycast(eyePos, direction, out RaycastHit hit, distance))
            {
                return hit.collider.CompareTag("Player");
            }
            return true;
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

        if(FirePoint == null) return;

        if (BulletPrefab == null)
        {
            Debug.LogWarning("[EnemyController] bulletPrefab이 인스펙터에 할당되지 않았습니다.");
            return;
        }
        
        if(_isFiringDelayed) return;
        StartCoroutine(FireWithDelay());
    }

    private System.Collections.IEnumerator FireWithDelay()
    {
        _isFiringDelayed = true;

        //선딜레이 대기
        yield return new WaitForSeconds(FirstfireDelay);

        // 딜레이 후 플레이어가 여전히 범위 안에 있는지 확인
        if(_playerInRange && _player != null)
        {
            Vector3 aimDir = (_player.position - FirePoint.position).normalized;
            GameObject bullet = Instantiate(BulletPrefab, FirePoint.position, FirePoint.rotation);
        
            if(!bullet.TryGetComponent(out BulletMover mover))
                mover = bullet.AddComponent<BulletMover>();
            
            mover.Initialize(aimDir, BulletSpeed);
            _nextFireTime = Time.time + 1f / FireRate;
        }
        _isFiringDelayed = false;
    }

    // ── Roaming 전용: 순찰 ─────────────────────────
    private void Patrol()
    {
        Vector3 target = _patrolOrigin + _patrolDir * (_patrolDirection * PatrolDistance);
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

            // patrolOrigin은 Start()에서 초기화 되기 때문에 실행전에는 값이 없어서 분기
            Vector3 origin = Application.isPlaying ? _patrolOrigin : transform.position;

            // 에디터 정지 상태에서는 _patrolDir이 초기화 안 됐으므로 직접 계산
            Vector3 dir = Application.isPlaying ? _patrolDir : PatrolAxis switch
            {
                PatrolAxis.X => Vector3.right,
                PatrolAxis.Z => Vector3.forward,
                PatrolAxis.Custom => CustomPatrolDirection.normalized,
                _ => Vector3.right
            };

            Gizmos.DrawLine(
                origin + dir * PatrolDistance,
                origin - dir * PatrolDistance
            );
        }

        if(_player != null && Application.isPlaying)
        {
            Vector3 eyePos = transform.position + Vector3.up * EyeHeight;
            Gizmos.color   = _playerInRange ? Color.green : Color.red;
            Gizmos.DrawLine(eyePos, _player.position + Vector3.up * EyeHeight);
        }
    }
}