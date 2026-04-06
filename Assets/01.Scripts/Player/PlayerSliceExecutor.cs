using System.Collections;
using UnityEngine;
using Assets.Scripts.SliceScripts;
using Random = UnityEngine.Random;

public class PlayerSliceExecutor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;

    [Header("Slice Force")]
    [SerializeField] private float _sliceForce = 8f;
    [SerializeField] private float _separateForceRatio = 0.55f;
    [SerializeField] private float _torqueForceRatio = 0.3f;
    [SerializeField] private float _forwardForceBias = 0.65f;

    [Header("Post Slice")]
    [SerializeField] private float _colliderDisableTime = 0.05f;

    private static readonly int[] OBSTACLE_ALLOWED_DIRS = { 0, 1, 3, 4, 5, 7 };

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;
    }

    public bool TryGetSliceHit(float range, out RaycastHit hit)
    {
        hit = default;

        if (_camera == null) return false;

        Ray ray = _camera.ScreenPointToRay(
            new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));

        if (!Physics.Raycast(ray, out hit, range)) return false;

        return hit.collider.GetComponent<Sliceable>() != null;
    }

    public bool TrySliceAtCrosshair(float range, WeaponSwingType swingType)
    {
        if (!TryGetSliceHit(range, out RaycastHit hit)) return false;

        ExecuteSlice(hit, swingType);
        return true;
    }

    public void ExecuteSlice(RaycastHit hit, WeaponSwingType swingType)
    {
        if (hit.collider == null || hit.collider.gameObject == null) return;

        GameObject target = hit.collider.gameObject;
        Sliceable sliceable = target.GetComponent<Sliceable>();
        if (sliceable == null) return;

        int randomDir = GetRandomDir(hit.collider);

        float angle = randomDir * 45f * Mathf.Deg2Rad;
        Vector2 swingDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        EventBus<OnWeaponSwingEvent>.Publish(new OnWeaponSwingEvent
        {
            direction = swingDir,
            swingType = swingType
        });

        Vector3 worldPlaneNormal = GetSliceNormal(swingDir);
        Vector3 worldSliceDirection = GetSliceDirection(swingDir);

        StackedSliceable stacked = target.GetComponent<StackedSliceable>();

        if (stacked != null)
        {
            Vector3 localPlaneNormal = target.transform.InverseTransformDirection(worldPlaneNormal).normalized;
            Vector3 localSliceDirection = target.transform.InverseTransformDirection(worldSliceDirection).normalized;
            Vector3 localHitPoint = target.transform.InverseTransformPoint(hit.point);

            stacked.RequestSlice(localHitPoint, localPlaneNormal, localSliceDirection);
        }
        else
        {
            SliceObject(target, hit.point, worldPlaneNormal, worldSliceDirection);
        }
    }

    public Vector3 GetCameraForward()
    {
        return _camera != null ? _camera.transform.forward : transform.forward;
    }

    private int GetRandomDir(Collider targetCollider)
    {
        if (targetCollider.CompareTag("Obstical"))
            return OBSTACLE_ALLOWED_DIRS[Random.Range(0, OBSTACLE_ALLOWED_DIRS.Length)];

        return Random.Range(0, 8);
    }

    private Vector3 GetSliceNormal(Vector2 swingDir)
    {
        return (_camera.transform.right * (-swingDir.y)
              + _camera.transform.up * swingDir.x).normalized;
    }

    private Vector3 GetSliceDirection(Vector2 swingDir)
    {
        Vector3 screenDir =
            (_camera.transform.right * swingDir.x) +
            (_camera.transform.up * swingDir.y);

        Vector3 forwardBias = _camera.transform.forward * _forwardForceBias;

        return (screenDir + forwardBias).normalized;
    }

    private void SliceObject(GameObject target, Vector3 hitPoint, Vector3 worldPlaneNormal, Vector3 worldSliceDirection)
    {
        Vector3 localPlaneNormal = target.transform.InverseTransformDirection(worldPlaneNormal).normalized;
        Vector3 localHitPoint = target.transform.InverseTransformPoint(hitPoint);

        Plane plane = new Plane();
        plane.SetNormalAndPosition(localPlaneNormal, localHitPoint);

        if (Vector3.Dot(Vector3.up, localPlaneNormal) < 0f)
            plane = plane.flipped;

        GameObject[] slices = Slicer.Slice(plane, target);
        Destroy(target);

        if (slices == null || slices.Length < 2) return;

        ApplySliceForce(slices[0], slices[1], worldSliceDirection, worldPlaneNormal);

        if (_colliderDisableTime > 0f)
        {
            foreach (GameObject slice in slices)
                StartCoroutine(DisableCollidersTemporarily(slice, _colliderDisableTime));
        }
    }

    private void ApplySliceForce(GameObject positive, GameObject negative, Vector3 worldSliceDirection, Vector3 worldPlaneNormal)
    {
        Vector3 positiveDir = (worldSliceDirection + worldPlaneNormal * _separateForceRatio).normalized;
        Vector3 negativeDir = (worldSliceDirection - worldPlaneNormal * _separateForceRatio).normalized;

        ApplyForceToPiece(positive, positiveDir, worldPlaneNormal);
        ApplyForceToPiece(negative, negativeDir, -worldPlaneNormal);
    }

    private void ApplyForceToPiece(GameObject piece, Vector3 forceDir, Vector3 normalDir)
    {
        if (piece == null) return;
        if (!piece.TryGetComponent<Rigidbody>(out Rigidbody rb)) return;

        rb.AddForce(forceDir * _sliceForce, ForceMode.VelocityChange);

        Vector3 torqueAxis = Vector3.Cross(normalDir, forceDir);
        if (torqueAxis.sqrMagnitude > 0.0001f)
            rb.AddTorque(torqueAxis.normalized * (_sliceForce * _torqueForceRatio), ForceMode.VelocityChange);
    }

    private IEnumerator DisableCollidersTemporarily(GameObject obj, float duration)
    {
        if (obj == null) yield break;

        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            if (c != null)
                c.enabled = false;
        }

        yield return new WaitForSeconds(duration);

        if (obj == null) yield break;

        foreach (Collider c in colliders)
        {
            if (c != null)
                c.enabled = true;
        }
    }
}
