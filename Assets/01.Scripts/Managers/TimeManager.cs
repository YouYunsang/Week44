using UnityEngine;

public class TimeManager : MonoSingleton<TimeManager>
{

    public void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = 0.02f * scale;

        EventBus<OnTimeScaleChangedEvent>.Publish(new OnTimeScaleChangedEvent
        {
            timeScale = scale
        });
    }

    public void Pause()        => SetTimeScale(0f);
    public void Resume()       => SetTimeScale(1f);
    public void SlowMotion()   => SetTimeScale(0.3f);

    public void SetTimeScaleForTime(float scale, float duration)
    {
        StartCoroutine(TimeScaleRoutine(scale, duration));
    }

    System.Collections.IEnumerator TimeScaleRoutine(float scale, float duration)
    {
        SetTimeScale(scale);
        yield return new WaitForSecondsRealtime(duration);
        SetTimeScale(1f);
    }

    public void LerpTimeScale(float target, float duration)
    {
        StopCoroutine(nameof(LerpTimeScaleRoutine));
        StartCoroutine(LerpTimeScaleRoutine(target, duration));
    }

    System.Collections.IEnumerator LerpTimeScaleRoutine(float target, float duration)
    {
        float start   = Time.timeScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetTimeScale(Mathf.Lerp(start, target, elapsed / duration));
            yield return null;
        }

        SetTimeScale(target);
    }
}
