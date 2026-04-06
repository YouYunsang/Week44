using System.Collections;
using UnityEngine;
using Assets.Scripts.SliceScripts;
using Random = UnityEngine.Random;

public class PlayerSliceExecutor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _pushForcePower = 14f;
    [SerializeField] private float _invincetime = 0.5f;

    private static readonly int[] OBSTACLE_ALLOWED_DIRS = { 0, 1, 3, 4, 5, 7 };

    private void Awake()
    {
        if(_camera == null) _camera = Camera.main;
    }

    public bool TryGetSliceHit(float range, out RaycastHit hit)
    {
        hit = default;

        if(_camera == null) return false;

        Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));

        if(!Physics.Raycast(ray, out hit, range)) return false;

        return hit.collider.GetComponent<Sliceable>() != null;
    }

    public bool TrySliceAtCrosshair(float range, WeaponSwingType swingType)
    {
        if(!TryGetSliceHit(range, out RaycastHit hit)) return false;

        ExecuteSlice(hit, swingType);
        return true;
    }

    public void ExecuteSlice(RaycastHit hit, WeaponSwingType swingType)
    {
        if(hit.collider == null ||hit.collider.gameObject == null) return;

        Sliceable sliceable = hit.collider.gameObject.GetComponent<Sliceable>();
        if (sliceable == null) return;

        int randomDir = GetRandomDir(hit.collider);
        float angle = randomDir * 45f * Mathf.Deg2Rad;
        Vector2 swingDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        EventBus<OnWeaponSwingEvent>.Publish(new OnWeaponSwingEvent
        {
            direction = swingDir,
            swingType = swingType
        });

        Vector3 normal = GetSliceNormal(randomDir);

        StackedSliceable stacked = hit.collider.gameObject.GetComponent<StackedSliceable>();

        if(stacked != null)
        {
            Vector3 transformedNormal =
                ((Vector3)(hit.collider.transform.localToWorldMatrix.transpose * normal)).normalized;

            Vector3 transformedPoint = 
                hit.collider.transform.InverseTransformPoint(hit.point);

            stacked.RequestSlice(transformedPoint, transformedNormal);
        }
        else    SliceObject(hit.collider.gameObject, hit.point, normal);
    }

    public Vector3 GetCameraForward()
    {
        return _camera != null ? _camera.transform.forward : transform.forward;
    }

    private int GetRandomDir(Collider targetCollider)
    {
        if(targetCollider.CompareTag("Obstical"))
            return OBSTACLE_ALLOWED_DIRS[Random.Range(0, OBSTACLE_ALLOWED_DIRS.Length)];

        return Random.Range(0, 8);
    }

    private Vector3 GetSliceNormal(int dirIndex)
    {
        float angle = dirIndex * 45f * Mathf.Deg2Rad;
        Vector2 swingDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        // 카메라 right / up 기준 슬라이스 평면 노멀 계산
        return (_camera.transform.right * (-swingDir.y)
              + _camera.transform.up * swingDir.x).normalized;
    }

    private void SliceObject(GameObject target, Vector3 hitPoint, Vector3 normal)
    {
        // 슬라이스 평면을 타겟 로컬 기준으로 변환
        Vector3 transformedNormal =
            ((Vector3)(target.transform.localToWorldMatrix.transpose * normal)).normalized;

        Vector3 transformedPoint = target.transform.InverseTransformPoint(hitPoint);

        Plane plane = new Plane();
        plane.SetNormalAndPosition(transformedNormal, transformedPoint);

        if (Vector3.Dot(Vector3.up, transformedNormal) < 0f)
            plane = plane.flipped;

        // 실제 슬라이스 실행
        GameObject[] slices = Slicer.Slice(plane, target);
        Destroy(target);

        // 슬라이스 노멀(월드 기준)으로 두 조각을 반대 방향으로 날림
        // normal: 월드 공간 슬라이스 평면 노멀
        Vector3 flyN = normal.normalized;
        var rb0 = slices[0].GetComponent<Rigidbody>();
        var rb1 = slices[1].GetComponent<Rigidbody>();
        if (rb0 != null) rb0.AddForce(( flyN + Vector3.up * 1.5f) * _pushForcePower, ForceMode.Impulse);
        if (rb1 != null) rb1.AddForce((-flyN + Vector3.up * 1.5f) * _pushForcePower, ForceMode.Impulse);

        // 생성 직후 0.5초간 콜라이더 비활성화 (관통 방지)
        foreach (var slice in slices)
            StartCoroutine(DisableCollidersTemporarily(slice, _invincetime));
    }

    private IEnumerator DisableCollidersTemporarily(GameObject obj, float duration)
    {
        var colliders = obj.GetComponentsInChildren<Collider>();
        foreach (var c in colliders) c.enabled = false;
        yield return new WaitForSeconds(duration);
        if (obj != null)
            foreach (var c in colliders) if (c != null) c.enabled = true;
    }
}
