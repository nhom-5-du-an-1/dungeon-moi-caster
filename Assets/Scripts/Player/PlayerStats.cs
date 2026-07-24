using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    //==========================
    // HEALTH
    //==========================

    [Header("Health")]
    public int maxHP = 100;
    public int currentHP;

    //==========================
    // MANA
    //==========================

    [Header("Mana")]
    public int maxMana = 100;
    public int currentMana;

    //==========================
    // HP REGEN
    //==========================

    [Header("HP Regeneration")]
    public int hpRegenAmount = 1;
    public float hpRegenTime = 1f;

    //==========================
    // MANA REGEN
    //==========================

    [Header("Mana Regeneration")]
    public int manaRegenAmount = 3;
    public float manaRegenTime = 1f;

    //==========================
    // UI
    //==========================

    [Header("UI")]
    public Image hpBar;
    public Image manaBar;

    //==========================
    // DEBUG
    //==========================

    [Header("Debug")]
    public bool godMode = false;

    private float hpTimer;
    private float manaTimer;

    //-------------------------------------------------------

    private void Start()
    {
        currentHP = maxHP;
        currentMana = maxMana;

        UpdateUI();
    }

    //-------------------------------------------------------

    private void Update()
    {
        RegenerateHP();
        RegenerateMana();

        DebugControl();
    }

    //=======================================================
    // DEBUG
    //=======================================================

    void DebugControl()
    {
        // Nhấn H để mất 20 HP
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(20);
        }

        // Nhấn M để mất 20 Mana
        if (Input.GetKeyDown(KeyCode.M))
        {
            UseMana(20);
        }

        // Nhấn J để hồi 20 HP
        if (Input.GetKeyDown(KeyCode.J))
        {
            Heal(20);
        }

        // Nhấn K để hồi 20 Mana
        if (Input.GetKeyDown(KeyCode.K))
        {
            RestoreMana(20);
        }
    }

    //=======================================================
    // HP REGEN
    //=======================================================

    void RegenerateHP()
    {
        if (currentHP >= maxHP)
            return;

        hpTimer += Time.deltaTime;

        if (hpTimer >= hpRegenTime)
        {
            hpTimer = 0;

            currentHP += hpRegenAmount;

            currentHP = Mathf.Clamp(currentHP, 0, maxHP);

            UpdateUI();
        }
    }

    //=======================================================
    // MANA REGEN
    //=======================================================

    void RegenerateMana()
    {
        if (currentMana >= maxMana)
            return;

        manaTimer += Time.deltaTime;

        if (manaTimer >= manaRegenTime)
        {
            manaTimer = 0;

            currentMana += manaRegenAmount;

            currentMana = Mathf.Clamp(currentMana, 0, maxMana);

            UpdateUI();
        }
    }

    //=======================================================
    // TAKE DAMAGE
    //=======================================================

    public void TakeDamage(int damage)
    {
        if (godMode)
            return;

        currentHP -= damage;

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        UpdateUI();

        Debug.Log("Player Take Damage : " + damage);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    //=======================================================
    // HEAL
    //=======================================================

    public void Heal(int amount)
    {
        currentHP += amount;

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        UpdateUI();

        Debug.Log("Heal : " + amount);
    }

    //=======================================================
    // USE MANA
    //=======================================================

    public bool UseMana(int amount)
    {
        if (godMode)
            return true;

        if (currentMana < amount)
        {
            Debug.Log("Not Enough Mana");

            return false;
        }

        currentMana -= amount;

        currentMana = Mathf.Clamp(currentMana, 0, maxMana);

        UpdateUI();

        return true;
    }

    //=======================================================
    // RESTORE MANA
    //=======================================================

    public void RestoreMana(int amount)
    {
        currentMana += amount;

        currentMana = Mathf.Clamp(currentMana, 0, maxMana);

        UpdateUI();

        Debug.Log("Restore Mana : " + amount);
    }

    //=======================================================
    // LEVEL UP
    //=======================================================

    public void LevelUp(int level)
    {
        maxHP = 100 + (level - 1) * 10;
        maxMana = 100 + (level - 1) * 5;

        currentHP = maxHP;
        currentMana = maxMana;

        UpdateUI();

        Debug.Log("Level Up!");
        Debug.Log("HP : " + maxHP);
        Debug.Log("Mana : " + maxMana);
    }

    //=======================================================
    // UI
    //=======================================================

    void UpdateUI()
    {
        if (hpBar != null)
            hpBar.fillAmount = (float)currentHP / maxHP;

        if (manaBar != null)
            manaBar.fillAmount = (float)currentMana / maxMana;
    }

    //=======================================================
    // PLAYER DEAD
    //=======================================================

    void Die()
    {
        Debug.Log("Player Dead");

        Time.timeScale = 0;

        //GameOverUI gameOver = FindAnyObjectByType<GameOverUI>();

        //if (gameOver != null)
        //{
        //    gameOver.Show();
        //}
    }
}