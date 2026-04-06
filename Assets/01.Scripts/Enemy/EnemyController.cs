using System.Runtime.InteropServices;
using UnityEngine;

public enum EnemyType
{
    Roaming,
    WallMounted
}

public enum PatrolAxis
{
    X,
    Z,
    Custom
}

public class EnemyController : MonoBehaviour
{
    [Header("시야 설정")]
    public LayerMask ObstacleLayer;
    public float EyeHeight = 1f;

    [Header("적 종류")]
    public EnemyType EnemyType = EnemyType.Roaming;

    [Header("탐지 설정")]
    public float DetectionRange = 10f;
    public LayerMask PlayerLayer;

    [Header("Roaming 어그로 설정 (EnemyType.Roaming 전용)")]
    public float AggroDetectionRange = 15f;

    [Header("발사 설정")]
    public GameObject BulletPrefab;
    public Transform FirePoint;
    public float FireRate = 1f;
    public float BulletSpeed = 10f;
    public float FirstfireDelay = 2f;

    [Header("포탑 회전 설정")]
    public Transform TurretHead;
    public float RotationSpeed = 5f;

    [Header("Roaming 순찰 설정 (EnemyType.Roaming 전용)")]
    public PatrolAxis PatrolAxis = PatrolAxis.X;
    public Vector3 CustomPatrolDirection = Vector3.right;
    public float PatrolDistance = 5f;
    public float PatrolSpeed = 2f;

    private Transform _player;
    private float _nextFireTime;
    private bool _playerInRange;
    private bool _isAggroed;

    private Vector3 _patrolOrigin;
    private int _patrolDirection = 1;
    private Vector3 _patrolDir;
    private bool _isFiringDelayed = false;

    private void Start()
    {
        if (FirePoint == null) FirePoint = transform;
        if (TurretHead == null) TurretHead = transform;

        if (EnemyType == EnemyType.Roaming)
        {
            _patrolOrigin = transform.position;

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

    private void DetectPlayer()
    {
        float currentRange = _isAggroed ? AggroDetectionRange : DetectionRange;

        Collider[] hits = PlayerLayer != 0
            ? Physics.OverlapSphere(transform.position, currentRange, PlayerLayer)
            : Physics.OverlapSphere(transform.position, currentRange);

        _playerInRange = false;
        _player = null;

        foreach (Collider col in hits)
        {
            if (!col.CompareTag("Player")) continue;
            if (!HasLineOfSight(col.transform)) continue;

            _player = col.transform;
            _playerInRange = true;

            if (!_isAggroed)
                _isAggroed = true;

            break;
        }

        if (_isAggroed && !_playerInRange)
        {
            Collider[] aggroHits = PlayerLayer != 0
                ? Physics.OverlapSphere(transform.position, AggroDetectionRange, PlayerLayer)
                : Physics.OverlapSphere(transform.position, AggroDetectionRange);

            bool stillInAggro = false;
            foreach (Collider col in aggroHits)
            {
                if (!col.CompareTag("Player")) continue;
                if (HasLineOfSight(col.transform))
                {
                    stillInAggro = true;
                    break;
                }
            }

            if (!stillInAggro) _isAggroed = false;
        }
    }

    private bool HasLineOfSight(Transform target)
    {
        Vector3 eyePos = transform.position + Vector3.up * EyeHeight;
        Vector3 targetPos = target.position + Vector3.up * EyeHeight;
        Vector3 direction = (targetPos - eyePos).normalized;
        float distance = Vector3.Distance(eyePos, targetPos);

        if (ObstacleLayer != 0)
        {
            return !Physics.Raycast(eyePos, direction, distance, ObstacleLayer);
        }
        else
        {
            if (Physics.Raycast(eyePos, direction, out RaycastHit hit, distance))
                return hit.collider.CompareTag("Player");
            return true;
        }
    }

    private void RotateTowardsPlayer()
    {
        Vector3 direction = (_player.position - TurretHead.position).normalized;

        if (direction != Vector3.zero)
        {
            TurretHead.rotation = Quaternion.Slerp(
                TurretHead.rotation,
                Quaternion.LookRotation(direction),
                RotationSpeed * Time.deltaTime
            );
        }
    }

    private void TryFire()
    {
        if (Time.time < _nextFireTime) return;
        if (FirePoint == null) return;
        if (BulletPrefab == null)
        {
            Debug.LogWarning("[EnemyController] bulletPrefab이 인스펙터에 할당되지 않았습니다.");
            return;
        }
        if (_isFiringDelayed) return;
        StartCoroutine(FireWithDelay());
    }

    private System.Collections.IEnumerator FireWithDelay()
    {
        _isFiringDelayed = true;

        yield return new WaitForSeconds(FirstfireDelay);

        // 🔑 모든 null 체크를 접근 전에
        if (this == null || gameObject == null) yield break;
        if (!_playerInRange || _player == null) { _isFiringDelayed = false; yield break; }
        if (FirePoint == null) { _isFiringDelayed = false; yield break; }

        Vector3 aimDir = (_player.position - FirePoint.position).normalized;
        GameObject bullet = Instantiate(BulletPrefab, FirePoint.position, FirePoint.rotation);

        if (!bullet.TryGetComponent(out BulletMover mover))
            mover = bullet.AddComponent<BulletMover>();

        mover.Initialize(aimDir, BulletSpeed);
        _nextFireTime = Time.time + 1f / FireRate;

        _isFiringDelayed = false;
    }

    private void Patrol()
    {
        Vector3 target = _patrolOrigin + _patrolDir * (_patrolDirection * PatrolDistance);
        transform.position = Vector3.MoveTowards(transform.position, target, PatrolSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.05f)
            _patrolDirection *= -1;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _playerInRange ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DetectionRange);

        if (EnemyType == EnemyType.Roaming)
        {
            Gizmos.color = _isAggroed ? new Color(1f, 0.5f, 0f) : new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, AggroDetectionRange);

            Gizmos.color = Color.green;

            Vector3 origin = Application.isPlaying ? _patrolOrigin : transform.position;

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

        if (_player != null && Application.isPlaying)
        {
            Vector3 eyePos = transform.position + Vector3.up * EyeHeight;
            Gizmos.color = _playerInRange ? Color.green : Color.red;
            Gizmos.DrawLine(eyePos, _player.position + Vector3.up * EyeHeight);
        }
    }
}