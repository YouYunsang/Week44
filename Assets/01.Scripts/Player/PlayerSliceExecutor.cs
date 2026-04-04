using UnityEngine;
using Assets.Scripts.SliceScripts;
using Random = UnityEngine.Random;

public class PlayerSliceExecutor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;

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

    public bool TrySliceAtCrosshair(float range)
    {
        if(!TryGetSliceHit(range, out RaycastHit hit)) return false;

        ExecuteSlice(hit);
        return true;
    }

    public void ExecuteSlice(RaycastHit hit)
    {
        if(hit.collider == null ||hit.collider.gameObject == null) return;

        Sliceable sliceable = hit.collider.gameObject.GetComponent<Sliceable>();
        if (sliceable == null) return;

        int randomDir = GetRandomDir(hit.collider);
        Vector3 normal = GetSliceNormal(randomDir);

        //? StackedSliceable이 있으면 스택 누적
        //? 없으면 즉시 슬라이스

        StackedSliceable stacked = hit.collider.gameObject.GetComponent<StackedSliceable>();
        if(stacked != null)
        {
            // 슬라이스 평면을 로컬 좌표로 변환해서 전달
            Vector3 trasnformedNormal = ((Vector3)(
                hit.collider.transform.localToWorldMatrix.transpose * normal)).normalized;
            
            Vector3 transformedPoint = 
                hit.collider.transform.InverseTransformPoint(hit.point);
            
            stacked.RequestSlice(transformedPoint, trasnformedNormal);
        }
        else
        {
            SliceObject(hit.collider.gameObject, hit.point, normal, _camera.transform.forward);
        }
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

    private void SliceObject(GameObject target, Vector3 hitPoint, Vector3 normal, Vector3 forceDirection)
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

        // 절단 조각에 힘 부여
        Vector3 force = transformedNormal + forceDirection * 1.4f;
        slices[0].GetComponent<Rigidbody>().AddForce(force * 3f, ForceMode.Impulse);
    }
}
