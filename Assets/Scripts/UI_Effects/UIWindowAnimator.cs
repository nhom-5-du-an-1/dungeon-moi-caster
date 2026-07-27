using System.Collections;
using UnityEngine;

public class UIWindowAnimator : MonoBehaviour
{
    [Header("=== ANIMATION CONFIG ===")]
    [Tooltip("Duration of the open/close animation in seconds")]
    public float duration = 0.22f;

    [Tooltip("Animation curve for opening scale transition")]
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Animation curve for closing scale transition")]
    public AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine animRoutine;
    private Vector3 originalScale = Vector3.one;
    private bool isInitialized = false;

    void Awake()
    {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (!isInitialized)
        {
            originalScale = transform.localScale;
            if (originalScale == Vector3.zero)
            {
                originalScale = Vector3.one; // Fallback
            }
            isInitialized = true;
        }
    }

    void OnEnable()
    {
        InitializeIfNeeded();
        // Start opening scale animation
        transform.localScale = Vector3.zero;
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateScale(Vector3.zero, originalScale, openCurve));
    }

    /// <summary>
    /// Play close animation and execute callback when complete
    /// </summary>
    public void CloseWindow(System.Action onComplete)
    {
        InitializeIfNeeded();
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateScale(transform.localScale, Vector3.zero, closeCurve, () => {
            onComplete?.Invoke();
        }));
    }

    private IEnumerator AnimateScale(Vector3 start, Vector3 end, AnimationCurve curve, System.Action onComplete = null)
    {
        float time = 0;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            float curveValue = curve.Evaluate(t);
            transform.localScale = Vector3.LerpUnclamped(start, end, curveValue);
            yield return null;
        }
        transform.localScale = end;
        onComplete?.Invoke();
    }
}
