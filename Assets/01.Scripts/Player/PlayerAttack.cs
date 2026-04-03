using UnityEngine;
using UnityEngine.InputSystem;
using Assets.Scripts.SliceScripts;

public class PlayerAttack : MonoBehaviour
{
    public float attackRange = 1f;
    public Camera cam;

    //수평 방향: 0 (→), 4 (←)
    //대각선:   1 (↗), 3 (↖), 5 (↙), 7 (↘)
    //수직 방향: 2 (↑), 6 (↓) ← Obstical은 이 두 방향 제외

    private readonly int[] _obsticalAllowedDirs = { 0, 1, 3, 4, 5, 7};
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            RandomSlice();
    }

    void RandomSlice()
    {
        Ray ray = cam.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f));

        if (Physics.Raycast(ray, out RaycastHit hit, attackRange))
        {
            Sliceable sliceable = hit.collider.GetComponent<Sliceable>();
            if(sliceable == null) return;

            int randomDir;

            if(hit.collider.CompareTag("Obstical"))
            {
                int randomIndex = Random.Range(0, _obsticalAllowedDirs.Length);
                randomDir = _obsticalAllowedDirs[randomIndex];
            }
            else
            {
                randomDir = Random.Range(0, 8);
            }

            Vector3 normal = GetSliceNormal(randomDir);
            SliceObject(hit.collider.gameObject, hit.point, normal);
        }
    }

    // 8방향 인덱스 → 절단 평면 법선
    Vector3 GetSliceNormal(int dirIndex)
    {
        float angle = dirIndex * 45f * Mathf.Deg2Rad;
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

        Vector3 force = transformedNormal + Vector3.up * 0.5f;
        slices[0].GetComponent<Rigidbody>().AddForce(force * 3f, ForceMode.Impulse);
    }
}