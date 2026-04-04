using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SlowGaugeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Image fillImage;

    [Header("Fade")]
    [SerializeField] float fadeDelay    = 0.5f;
    [SerializeField] float fadeDuration = 0.5f;

    Coroutine _fadeCoroutine;

    void Awake()
    {
        canvasGroup.alpha = 0f;
    }

    void OnEnable()
    {
        EventBus<OnSlowGaugeChangedEvent>.Subscribe(OnGaugeChanged);
    }

    void OnDisable()
    {
        EventBus<OnSlowGaugeChangedEvent>.Unsubscribe(OnGaugeChanged);
    }

    void OnGaugeChanged(OnSlowGaugeChangedEvent e)
    {
        fillImage.fillAmount = e.max > 0f ? e.current / e.max : 0f;

        if (e.isSlowing)
        {
            Show();
        }
        else if (e.current >= e.max)
        {
            // 완전히 충전되면 페이드아웃
            TriggerFadeOut();
        }
        // 충전 중(슬로우 아님)엔 게이지를 계속 표시
    }

    void Show()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
        canvasGroup.alpha = 1f;
    }

    void TriggerFadeOut()
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSecondsRealtime(fadeDelay);

        float elapsed    = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed           += Time.unscaledDeltaTime;
            canvasGroup.alpha  = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        _fadeCoroutine    = null;
    }
}
