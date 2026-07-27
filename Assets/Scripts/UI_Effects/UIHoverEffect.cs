using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("=== CONFIG ===")]
    [Tooltip("The scale factor when hovered")]
    public float hoverScale = 1.2f;
    [Tooltip("The scale factor when clicked")]
    public float clickScale = 0.9f;
    [Tooltip("The smoothing speed of the scaling transition")]
    public float speed = 12f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void OnEnable()
    {
        transform.localScale = originalScale;
        targetScale = originalScale;
    }

    void OnDisable()
    {
        transform.localScale = originalScale;
        targetScale = originalScale;
    }

    void Update()
    {
        // Smoothly interpolate the scale
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }
}
