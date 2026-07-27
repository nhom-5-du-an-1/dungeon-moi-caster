using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FloatingTextManager : MonoBehaviour
{
    private static FloatingTextManager instance;

    /// <summary>
    /// Spawn floating text in world space above a target position (e.g., damage over Player or Enemy head)
    /// </summary>
    public static void SpawnWorldText(string content, Vector3 worldPosition, Color textColor, float fontSize = 14f)
    {
        EnsureInstance();
        instance.CreateText(content, worldPosition, textColor, fontSize);
    }

    private static void EnsureInstance()
    {
        if (instance == null)
        {
            GameObject obj = new GameObject("[FloatingTextManager]");
            instance = obj.AddComponent<FloatingTextManager>();
            DontDestroyOnLoad(obj);
        }
    }

    private void CreateText(string content, Vector3 worldPos, Color color, float size)
    {
        GameObject popObj = new GameObject("FloatingDamageText");
        popObj.transform.position = worldPos + new Vector3(Random.Range(-0.05f, 0.05f), 0.15f, 0f);

        Canvas canvas = popObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 9999; // Render on top of sprites

        RectTransform rect = popObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2f, 1f);
        rect.localScale = new Vector3(0.0048f, 0.0048f, 1f); // Ultra-compact world scale

        Text txt = popObj.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = (int)size;
        txt.fontStyle = FontStyle.Bold;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = popObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(0.8f, -0.8f);

        StartCoroutine(AnimateFloatingText(popObj, color));
    }

    private IEnumerator AnimateFloatingText(GameObject obj, Color initialColor)
    {
        float duration = 0.85f;
        float elapsed = 0f;
        Vector3 startPos = obj.transform.position;
        Text txt = obj.GetComponent<Text>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            // Move upward in world space
            if (obj != null)
            {
                obj.transform.position = startPos + new Vector3(0f, percent * 0.45f, 0f);
            }

            // Fade out alpha
            if (txt != null)
            {
                Color c = initialColor;
                c.a = 1f - percent;
                txt.color = c;
            }

            yield return null;
        }

        if (obj != null)
        {
            Destroy(obj);
        }
    }
}
