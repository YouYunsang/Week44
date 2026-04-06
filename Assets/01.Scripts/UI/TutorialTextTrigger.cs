using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어가 진입하면 텍스트 오브젝트가 땅에서 위로 슥 올라옴
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialTextTrigger : MonoBehaviour
{
    [Header("텍스트 오브젝트")]
    [Tooltip("월드 스페이스 TextMeshPro 오브젝트")]
    [SerializeField] GameObject _textObject;

    [Header("위치 설정")]
    [Tooltip("텍스트 시작 위치 (땅)")]
    [SerializeField] Transform _startPoint;
    [Tooltip("텍스트 최종 위치 (눈높이보다 살짝 위)")]
    [SerializeField] Transform _endPoint;

    [Header("애니메이션 설정")]
    [SerializeField] float _riseDuration = 0.6f;        // 올라오는 시간
    [SerializeField] AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    bool _triggered = false;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        // 시작 시 텍스트를 땅 위치에 숨겨둠
        if (_textObject != null && _startPoint != null)
        {
            _textObject.transform.position = _startPoint.position;
            _textObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!IsPlayer(other)) return;

        _triggered = true;
        StartCoroutine(RiseUp());
    }

    IEnumerator RiseUp()
    {
        if (_textObject == null || _startPoint == null || _endPoint == null) yield break;

        _textObject.SetActive(true);
        _textObject.transform.position = _startPoint.position;

        float elapsed = 0f;

        while (elapsed < _riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = _riseCurve.Evaluate(Mathf.Clamp01(elapsed / _riseDuration));
            _textObject.transform.position = Vector3.Lerp(
                _startPoint.position,
                _endPoint.position,
                t
            );
            yield return null;
        }

        _textObject.transform.position = _endPoint.position;
    }

    bool IsPlayer(Collider other)
    {
        if (other.TryGetComponent<PlayerMovement>(out _)) return true;
        Transform root = other.transform.root;
        return root != null && root.TryGetComponent<PlayerMovement>(out _);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (_startPoint != null && _endPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_startPoint.position, _endPoint.position);
            Gizmos.DrawWireSphere(_startPoint.position, 0.1f);
            Gizmos.DrawWireSphere(_endPoint.position, 0.1f);
        }
    }
#endif
}