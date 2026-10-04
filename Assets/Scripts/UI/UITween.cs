using System.Collections;
using UnityEngine;

/// <summary>
/// 간단한 UI 연출용 코루틴. 일시정지(timeScale=0) 중에도 동작하도록 unscaledDeltaTime 사용.
/// </summary>
public static class UITween
{
    public static IEnumerator Fade(CanvasGroup group, float to, float duration)
    {
        float from = group.alpha;
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            if (group == null) yield break;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        if (group != null) group.alpha = to;
    }

    /// <summary>팝업이 톡 튀어나오는 연출</summary>
    public static IEnumerator PopIn(Transform target, float duration = 0.28f)
    {
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            if (target == null) yield break;
            target.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, EaseOutBack(t / duration));
            yield return null;
        }
        if (target != null) target.localScale = Vector3.one;
    }

    /// <summary>숫자가 바뀔 때 살짝 커졌다 돌아오는 연출</summary>
    public static IEnumerator Punch(Transform target, float amount = 0.25f, float duration = 0.2f)
    {
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            if (target == null) yield break;
            target.localScale = Vector3.one * (1f + amount * Mathf.Sin(t / duration * Mathf.PI));
            yield return null;
        }
        if (target != null) target.localScale = Vector3.one;
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
    }
}
