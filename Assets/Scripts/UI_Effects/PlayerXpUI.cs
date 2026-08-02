using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerXpUI : MonoBehaviour
{
    [Header("=== UI ELEMENTS ===")]
    [Tooltip("Thanh điền XP màu xanh lá")]
    public Image xpBarFill;

    [Tooltip("Chữ hiển thị Level ở tâm thanh XP")]
    public TextMeshProUGUI levelText;

    private PlayerStats playerStats;

    private void OnEnable()
    {
        if (PlayerStats.Instance != null)
        {
            playerStats = PlayerStats.Instance;
            playerStats.OnXpChanged -= UpdateXpUI;
            playerStats.OnXpChanged += UpdateXpUI;

            UpdateXpUI(playerStats.currentXp, playerStats.xpToNextLevel, playerStats.xpLevel);
        }
        else
        {
            Invoke("LateInit", 0.1f);
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnXpChanged -= UpdateXpUI;
        }
    }

    private void Start()
    {
        LateInit();
    }

    private void LateInit()
    {
        if (playerStats == null && PlayerStats.Instance != null)
        {
            playerStats = PlayerStats.Instance;
            playerStats.OnXpChanged -= UpdateXpUI;
            playerStats.OnXpChanged += UpdateXpUI;

            UpdateXpUI(playerStats.currentXp, playerStats.xpToNextLevel, playerStats.xpLevel);
        }
    }

    private void UpdateXpUI(int currentXp, int xpToNextLevel, int level)
    {
        if (xpBarFill != null)
        {
            float fillRatio = xpToNextLevel > 0 ? (float)currentXp / xpToNextLevel : 0f;
            xpBarFill.fillAmount = fillRatio;
        }

        if (levelText != null)
        {
            levelText.text = level.ToString();
        }
    }
}
