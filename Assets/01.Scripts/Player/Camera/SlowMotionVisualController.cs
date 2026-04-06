using UnityEngine;
using UnityEngine.Rendering;

public class SlowMotionVisualController : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Volume _slowBlackAndWhiteVolume;

    [Header("Blend")]
    [SerializeField] private float _blendSpeed = 8f;

    private float _targetWeight = 0f;

    private void Awake()
    {
        if (_slowBlackAndWhiteVolume != null)
            _slowBlackAndWhiteVolume.weight = 0f;
    }

    private void OnEnable()
    {
        EventBus<OnSlowGaugeChangedEvent>.Subscribe(HandleSlowGaugeChanged);
    }

    private void OnDisable()
    {
        EventBus<OnSlowGaugeChangedEvent>.Unsubscribe(HandleSlowGaugeChanged);
    }

    private void Update()
    {
        if (_slowBlackAndWhiteVolume == null) return;

        _slowBlackAndWhiteVolume.weight = Mathf.Lerp(
            _slowBlackAndWhiteVolume.weight,
            _targetWeight,
            _blendSpeed * Time.unscaledDeltaTime);
    }

    private void HandleSlowGaugeChanged(OnSlowGaugeChangedEvent evt)
    {
        _targetWeight = evt.isSlowing ? 1f : 0f;
    }
}
