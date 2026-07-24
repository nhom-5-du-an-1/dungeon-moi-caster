using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("=== PLAYER REFERENCES ===")]
    public PlayerStats playerStats;

    [Header("=== HUD COMPONENTS ===")]
    public RectTransform hudRectTransform;
    public Text avatarText;

    [Header("=== HEALTH BAR COMPONENTS ===")]
    public Image healthBarFill;
    public Image catchUpBarFill;
    public Text healthText;

    [Header("=== MANA BAR COMPONENTS ===")]
    public Image manaBarFill;
    public Text manaText;

    [Header("=== LERP & ANIMATION SETTINGS ===")]
    public float catchUpSpeed = 2.5f;
    public float entranceSpeed = 5.0f;

    private float targetHealthFill = 1f;
    private float targetManaFill = 1f;
    private Color originalHealthColor = new Color(0.95f, 0.18f, 0.18f, 1.0f);
    
    // Animation state
    private Vector2 restingPosition = new Vector2(30f, -30f);
    private float punchScale = 1.0f;
    private Vector2 shakeOffset = Vector2.zero;
    private int lastHealth = -1;
    private float lastMana = -1f;

    void Start()
    {
        if (hudRectTransform == null) hudRectTransform = GetComponent<RectTransform>();
        if (healthBarFill != null) originalHealthColor = healthBarFill.color;

        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        // Initialize HUD entrance animation off-screen to the left
        if (hudRectTransform != null)
        {
            hudRectTransform.anchoredPosition = new Vector2(-400f, -30f);
        }

        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealthBar;
            playerStats.OnManaChanged += UpdateManaBar;

            lastHealth = playerStats.currentHealth;
            lastMana = playerStats.currentMana;

            // Sync immediately
            UpdateHealthBar(playerStats.currentHealth, playerStats.maxHealth);
            UpdateManaBar(playerStats.currentMana, playerStats.maxMana);
        }
    }

    void Update()
    {
        // 1. Entrance Slide-In Animation at Start
        if (hudRectTransform != null)
        {
            hudRectTransform.anchoredPosition = Vector2.Lerp(hudRectTransform.anchoredPosition, restingPosition + shakeOffset, Time.deltaTime * entranceSpeed);
            hudRectTransform.localScale = Vector3.Lerp(hudRectTransform.localScale, Vector3.one * punchScale, Time.deltaTime * 12f);
        }

        // 2. Decay Punch Scale & Shake Offset
        punchScale = Mathf.Lerp(punchScale, 1.0f, Time.deltaTime * 10f);
        shakeOffset = Vector2.Lerp(shakeOffset, Vector2.zero, Time.deltaTime * 12f);

        // 3. Avatar Emblem Golden Breathing Glow Animation
        if (avatarText != null)
        {
            float t = (Mathf.Sin(Time.time * 3.5f) + 1f) * 0.5f;
            avatarText.color = Color.Lerp(new Color(1.0f, 0.85f, 0.2f, 1f), new Color(1.0f, 0.6f, 0.1f, 1f), t);
        }

        // 4. Smoothly lerp catchUpBarFill towards targetHealthFill for satisfying damage animation
        if (catchUpBarFill != null && Mathf.Abs(catchUpBarFill.fillAmount - targetHealthFill) > 0.001f)
        {
            catchUpBarFill.fillAmount = Mathf.Lerp(catchUpBarFill.fillAmount, targetHealthFill, catchUpSpeed * Time.deltaTime);
        }

        // 5. Low Health Warning Pulse Effect when HP <= 25%
        if (healthBarFill != null && targetHealthFill <= 0.25f && targetHealthFill > 0f)
        {
            float pulseAlpha = (Mathf.Sin(Time.time * 8f) + 1f) * 0.3f + 0.4f;
            Color c = originalHealthColor;
            c.a = pulseAlpha;
            healthBarFill.color = c;
        }
        else if (healthBarFill != null)
        {
            healthBarFill.color = originalHealthColor;
        }
    }

    void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthBar;
            playerStats.OnManaChanged -= UpdateManaBar;
        }
    }

    private void UpdateHealthBar(int current, int max)
    {
        float previousFill = targetHealthFill;
        targetHealthFill = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = targetHealthFill;
        }

        // Check if health dropped -> Trigger Impact Punch & Shake
        if (lastHealth >= 0 && current < lastHealth)
        {
            int damage = lastHealth - current;
            TriggerHUDImpact();
            SpawnFloatingText($"-{damage}", new Color(1.0f, 0.2f, 0.2f, 1f), new Vector2(240f, -25f));
        }
        else if (lastHealth >= 0 && current > lastHealth)
        {
            int heal = current - lastHealth;
            SpawnFloatingText($"+{heal}", new Color(0.2f, 1.0f, 0.3f, 1f), new Vector2(240f, -25f));
            if (catchUpBarFill != null) catchUpBarFill.fillAmount = targetHealthFill;
        }

        lastHealth = current;

        if (healthText != null)
        {
            healthText.text = $"{current} / {max}";
        }
    }

    private void UpdateManaBar(float current, float max)
    {
        targetManaFill = max > 0 ? Mathf.Clamp01(current / max) : 0f;

        if (manaBarFill != null)
        {
            manaBarFill.fillAmount = targetManaFill;
        }

        if (lastMana >= 0 && current < lastMana - 1f)
        {
            float spent = lastMana - current;
            SpawnFloatingText($"-{(int)spent} MP", new Color(0.2f, 0.7f, 1.0f, 1f), new Vector2(240f, -65f));
        }

        lastMana = current;

        if (manaText != null)
        {
            manaText.text = $"{(int)current} / {(int)max}";
        }
    }

    private void TriggerHUDImpact()
    {
        punchScale = 1.12f; // Scale up HUD slightly
        shakeOffset = new Vector2(Random.Range(-6f, 6f), Random.Range(-4f, 4f)); // Quick jitter
    }

    private void SpawnFloatingText(string content, Color textColor, Vector2 localPos)
    {
        if (transform == null) return;

        GameObject popObj = new GameObject("PopupText");
        popObj.transform.SetParent(transform, false);

        RectTransform rect = popObj.AddComponent<RectTransform>();
        rect.anchoredPosition = localPos;
        rect.sizeDelta = new Vector2(100f, 30f);

        Text txt = popObj.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = 22;
        txt.fontStyle = FontStyle.Bold;
        txt.color = textColor;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = popObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        StartCoroutine(AnimateFloatingText(popObj, rect, txt));
    }

    private IEnumerator AnimateFloatingText(GameObject obj, RectTransform rect, Text txt)
    {
        float duration = 0.85f;
        float elapsed = 0f;
        Vector2 startPos = rect.anchoredPosition;
        Color initialColor = txt.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            
            // Move up
            rect.anchoredPosition = startPos + new Vector2(0f, percent * 35f);
            
            // Fade out
            Color c = initialColor;
            c.a = 1f - percent;
            txt.color = c;

            yield return null;
        }

        Destroy(obj);
    }
}
