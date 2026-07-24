using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("=== HEALTH STATS ===")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("=== MANA STATS ===")]
    public float maxMana = 100f;
    public float currentMana;
    public float manaRegenRate = 5f; // Regenerate 5 mana per second

    [Header("=== RESPAWN SETTINGS ===")]
    public float respawnDelay = 3.0f;

    [Header("=== STATUS ===")]
    public bool isDead = false;
    public bool IsDead => isDead;

    // Events for UI update
    public event Action<int, int> OnHealthChanged;
    public event Action<float, float> OnManaChanged;
    public event Action OnPlayerDeath;
    public event Action OnPlayerRespawn;

    private Animator anim;
    private PlayerMovement pm;

    void Awake()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;
    }

    void Start()
    {
        anim = GetComponent<Animator>();
        pm = GetComponent<PlayerMovement>();

        // Sync initial UI
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    void Update()
    {
        if (isDead) return;

        // Automatic passive mana regeneration
        if (currentMana < maxMana)
        {
            currentMana += manaRegenRate * Time.deltaTime;
            currentMana = Mathf.Min(currentMana, maxMana);
            OnManaChanged?.Invoke(currentMana, maxMana);
        }

        // Debug shortcuts for testing combat UI
        if (Input.GetKeyDown(KeyCode.I))
        {
            TakeDamage(15);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            Heal(15);
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            UseMana(20f);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            RestoreMana(20f);
        }
    }

    /// <summary>
    /// Apply damage to the player
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"<color=red>[Player Stats]</color> Player nhận <color=red>-{damage} HP</color>! Máu hiện tại: {currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            TryTriggerHurtAnimation();
        }
    }

    /// <summary>
    /// Heal player by specific amount
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        Debug.Log($"<color=green>[Player Stats]</color> Player hồi <color=green>+{amount} HP</color>! Máu hiện tại: {currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Consume player mana
    /// </summary>
    public bool UseMana(float amount)
    {
        if (isDead) return false;

        if (currentMana >= amount)
        {
            currentMana -= amount;
            OnManaChanged?.Invoke(currentMana, maxMana);
            return true;
        }
        
        Debug.Log("<color=yellow>[Player Stats] Không đủ Mana!</color>");
        return false;
    }

    /// <summary>
    /// Restore a specific amount of mana
    /// </summary>
    public void RestoreMana(float amount)
    {
        if (isDead) return;
        currentMana += amount;
        currentMana = Mathf.Min(currentMana, maxMana);
        OnManaChanged?.Invoke(currentMana, maxMana);
        Debug.Log($"<color=blue>[Player Stats]</color> Player hồi <color=cyan>+{amount:F1} Mana</color>! Mana hiện tại: {currentMana:F1}/{maxMana}");
    }

    private bool HasParameter(string paramName)
    {
        if (anim == null) return false;
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("<color=red><b>[Player Stats] Player đã hy sinh! Bắt đầu chuỗi hoạt ảnh ngã xuống và tự động hồi sinh sau 3 giây.</b></color>");
        
        // Trigger death event
        OnPlayerDeath?.Invoke();

        // Disable PlayerMovement component & physics
        if (pm != null)
        {
            pm.enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Start death animation sequence and 3s auto respawn coroutine
        StartCoroutine(DeathAndRespawnRoutine());
    }

    private IEnumerator DeathAndRespawnRoutine()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        // Disable animator temporarily so sprite sequence isn't overwritten
        if (anim != null) anim.enabled = false;

        if (sr != null)
        {
            // Find sliced sprites Frame_35 to Frame_42
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            List<Sprite> dieFrames = new List<Sprite>();
            for (int i = 35; i <= 42; i++)
            {
                string fName = $"Frame_{i}";
                Sprite s = System.Array.Find(allSprites, x => x.name == fName);
                if (s != null) dieFrames.Add(s);
            }

            if (dieFrames.Count > 0)
            {
                float frameDuration = 0.12f; // ~8 fps
                foreach (Sprite f in dieFrames)
                {
                    sr.sprite = f;
                    yield return new WaitForSeconds(frameDuration);
                }
                // Stay collapsed on ground for remainder of timer
                sr.sprite = dieFrames[dieFrames.Count - 1];
            }
        }

        // Wait total 3.0 seconds delay before respawning
        yield return new WaitForSeconds(2.0f);

        // Respawn Player
        Respawn();
    }

    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        currentMana = maxMana;

        // Re-enable animator & PlayerMovement
        if (anim != null) anim.enabled = true;
        if (pm != null) pm.enabled = true;

        // Notify UI of full health/mana recovery
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnManaChanged?.Invoke(currentMana, maxMana);

        OnPlayerRespawn?.Invoke();

        Debug.Log("<color=green><b>[PlayerStats] Player đã tự động HỒI SINH thành công với 100% Máu & Mana!</b></color>");
    }

    private void TryTriggerHurtAnimation()
    {
        if (HasParameter("Hurt"))
        {
            anim.SetTrigger("Hurt");
        }
        else if (HasParameter("Hit"))
        {
            anim.SetTrigger("Hit");
        }
    }
}
