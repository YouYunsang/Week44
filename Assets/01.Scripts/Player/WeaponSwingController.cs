using System.Collections;
using UnityEngine;

public class WeaponSwingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform _weaponPivot;
    [SerializeField] GameObject _trailObject;

    [Header("Swing Settings")]
    [SerializeField] float _swingAngle    = 180f;
    [SerializeField] float _swingDuration = 0.85f;

    Quaternion _restLocalRot;
    Coroutine  _swingRoutine;

    void Awake()
    {
        if (_weaponPivot != null)
            _restLocalRot = _weaponPivot.localRotation;

        if (_trailObject != null)
            _trailObject.SetActive(false);
    }

    void OnEnable()  => EventBus<OnWeaponSwingEvent>.Subscribe(OnWeaponSwing);
    void OnDisable() => EventBus<OnWeaponSwingEvent>.Unsubscribe(OnWeaponSwing);

    void OnWeaponSwing(OnWeaponSwingEvent e)
    {
        if (_weaponPivot == null) return;
        if (_swingRoutine != null) StopCoroutine(_swingRoutine);
        _swingRoutine = StartCoroutine(SwingRoutine(e.direction));
    }

    IEnumerator SwingRoutine(Vector2 swingDir)
    {
        if (_trailObject != null) _trailObject.SetActive(true);

        // 1. Z축으로 시작 방향 초기화 (이미 잘 됨)
        float      zAngle   = Mathf.Atan2(swingDir.y, swingDir.x) * Mathf.Rad2Deg;
        Quaternion startRot = _restLocalRot * Quaternion.Euler(0f, 0f, zAngle);
        _weaponPivot.localRotation = startRot;

        // 2. swingDir에 수직인 축으로 스윙
        // swingDir=(0,1) 수직 → axis=(-1,0,0) X축 → 위→아래 스윙
        // swingDir=(1,0) 수평 → axis=(0,1,0)  Y축 → 오→왼 스윙
        Vector3    swingAxis = new Vector3(-swingDir.y, swingDir.x, 0f).normalized;
        Quaternion endRot    = Quaternion.AngleAxis(_swingAngle, swingAxis) * startRot;

        // t → angle 직접 계산 (누적 오차 없음, 180° 초과 가능)
        float elapsed = 0f;
        while (elapsed < _swingDuration)
        {
            elapsed    += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / _swingDuration);
            float ease  = 1f - (1f - t) * (1f - t);            // ease-out quadratic
            float angle = -_swingAngle * ease;

            _weaponPivot.localRotation = Quaternion.AngleAxis(angle, swingAxis) * startRot;
            yield return null;
        }
        _weaponPivot.localRotation = Quaternion.AngleAxis(_swingAngle, swingAxis) * startRot;

        _weaponPivot.localRotation = _restLocalRot;

        if (_trailObject != null) _trailObject.SetActive(false);
        _swingRoutine = null;
    }
}
