using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("=== HEALTH STATS ===")]
    public int maxHealth = 20;
    public int currentHealth;

    [Header("=== MANA STATS ===")]
    public float maxMana = 100f;
    public float currentMana;
    public float manaRegenRate = 5f; // Regenerate 5 mana per second

    [Header("=== RESPAWN SETTINGS ===")]
    public float respawnDelay = 4.0f; // 4 seconds delay before respawning

    [Header("=== DEATH ANIMATION SPRITES ===")]
    public Sprite[] deathSprites; // Array containing Frame_35 to Frame_42

    [Header("=== STATUS ===")]
    public bool isDead = false;
    public bool IsDead => isDead;

    public static PlayerStats Instance { get; private set; }

    // Events for UI update
    public event Action<int, int> OnHealthChanged;
    public static event Action<int, int> OnAnyPlayerHealthChanged;
    public event Action<float, float> OnManaChanged;
    public event Action OnPlayerDeath;
    public event Action OnPlayerRespawn;

    private Animator anim;
    private PlayerMovement pm;
    private Vector3 deathPosition;

    void Awake()
    {
        // Singleton & Giữ Player không bị xóa khi đổi Scene
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            if (Instance.gameObject != gameObject)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                Destroy(this);
                return;
            }
        }

        // Tự động dọn dẹp các component PlayerStats bị trùng lặp trên cùng GameObject nếu có
        PlayerStats[] duplicates = GetComponents<PlayerStats>();
        if (duplicates.Length > 1 && duplicates[0] != this)
        {
            Destroy(this);
            return;
        }

        // Nếu người dùng chưa cấu hình maxHealth (<= 0), mặc định đặt là 20 HP (10 tim)
        if (maxHealth <= 0)
            maxHealth = 20;

        currentHealth = maxHealth;
        currentMana = maxMana;

        // Camera tách biệt khỏi Player - CameraFollow tự tìm Player
    }



    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (Instance == this)
        {
            // Camera tự tìm Player qua CameraFollow.FindPlayerTarget()

            // Tìm điểm SpawnPoint ở bản đồ mới
            GameObject spawnPoint = GameObject.FindWithTag("SpawnPoint") ?? GameObject.Find("SpawnPoint") ?? GameObject.Find("PlayerSpawn");
            if (spawnPoint != null)
            {
                transform.position = spawnPoint.transform.position;
            }
            else
            {
                // KHÔNG tự động đưa Player về Vector3.zero nữa vì sẽ đè lên vị trí người dùng kéo thả thủ công trong scene!
                Debug.Log("<color=yellow>[PlayerStats]</color> Không tìm thấy SpawnPoint trong Scene. Giữ nguyên vị trí hiện tại của Player.");
            }

            // Dừng vận tốc di chuyển
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            // Cập nhật lại UI cho Scene mới ngay sau khi load
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnAnyPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
            OnManaChanged?.Invoke(currentMana, maxMana);
        }
    }

    void Start()
    {
        anim = GetComponent<Animator>();
        pm = GetComponent<PlayerMovement>();

        // Sync initial UI
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnAnyPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
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

        // Debug & Item shortcuts for testing combat UI
        if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.B))
        {
            Heal(500); // Dùng Túi Đồ / Balo để hồi +500 HP
            Debug.Log("<color=green>[Túi Đồ] Đã sử dụng Túi Đồ để hồi +500 HP!</color>");
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            TakeDamage(15);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            Heal(100);
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
    /// Apply damage & knockback to the player
    /// </summary>
    public void TakeDamage(int damage, Vector2 attackerPos = default, float knockbackForce = 4.0f)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // Kích hoạt hiệu ứng Bật Lùi (Knockback) nếu có vị trí kẻ tấn công
        if (attackerPos != default)
        {
            Vector2 knockbackDir = ((Vector2)transform.position - attackerPos).normalized;
            if (knockbackDir.sqrMagnitude < 0.001f) knockbackDir = Vector2.up;

            PlayerMovement pm = GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.ApplyKnockback(knockbackDir, knockbackForce);
            }
        }

        // Hiển thị số sát thương nhảy lên trên đầu Player
        FloatingTextManager.SpawnWorldText($"-{damage}", transform.position + Vector3.up * 0.45f, new Color(1.0f, 0.2f, 0.2f, 1.0f));

        Debug.Log($"<color=red>[Player Stats]</color> Player nhận <color=red>-{damage} HP</color>! Máu hiện tại: {currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnAnyPlayerHealthChanged?.Invoke(currentHealth, maxHealth);

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

        // Hiển thị số hồi máu nhảy lên trên đầu Player
        FloatingTextManager.SpawnWorldText($"+{amount}", transform.position + Vector3.up * 0.45f, new Color(0.2f, 1.0f, 0.3f, 1.0f));

        Debug.Log($"<color=green>[Player Stats]</color> Player hồi <color=green>+{amount} HP</color>! Máu hiện tại: {currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnAnyPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
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
            
            // Hiển thị số hao mana nhảy lên trên đầu Player
            FloatingTextManager.SpawnWorldText($"-{(int)amount} MP", transform.position + Vector3.up * 0.45f, new Color(0.2f, 0.7f, 1.0f, 1.0f));

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
        deathPosition = transform.position;
        Debug.Log("<color=red><b>[Player Stats] Player đã hy sinh! Chạy hoạt ảnh ngã gục (Frame 35-42), đến frame cuối mới biến mất và hồi sinh sau 4s.</b></color>");
        
        // Trigger death event
        OnPlayerDeath?.Invoke();

        // 1. Disable PlayerMovement immediately so movement input doesn't interrupt death frames
        if (pm != null)
        {
            pm.enabled = false;
        }

        // 2. Disable physics simulation and collider during death
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // 3. Disable Animator component so AnimatorController doesn't overwrite sprite changes
        if (anim != null)
        {
            anim.enabled = false;
        }

        // Start Death Animation (Frame 35-42) & 4-second Respawn Routine
        StartCoroutine(DeathAnimationAndRespawnRoutine());
    }

    private IEnumerator DeathAnimationAndRespawnRoutine()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        // Phase 1: Play Death Animation Frames 35 to 42
        if (sr != null)
        {
            // Fallback runtime search if array is not assigned
            if (deathSprites == null || deathSprites.Length == 0)
            {
                Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                List<Sprite> dieFrames = new List<Sprite>();
                for (int i = 35; i <= 42; i++)
                {
                    string fName = $"Frame_{i}";
                    Sprite s = System.Array.Find(allSprites, x => x.name == fName);
                    if (s != null) dieFrames.Add(s);
                }
                if (dieFrames.Count > 0) deathSprites = dieFrames.ToArray();
            }

            if (deathSprites != null && deathSprites.Length > 0)
            {
                float frameDuration = 0.12f; // ~8 fps animation
                for (int i = 0; i < deathSprites.Length; i++)
                {
                    if (deathSprites[i] != null)
                    {
                        sr.sprite = deathSprites[i];
                        yield return new WaitForSeconds(frameDuration);
                    }
                }

                // Stay collapsed on the final frame (Frame_42) lying dead on the ground
                sr.sprite = deathSprites[deathSprites.Length - 1];
                yield return new WaitForSeconds(1.2f);
            }

            // Phase 2: Fade Out / Vanish ONLY AFTER death animation reaches final frame and holds!
            Color startColor = sr.color;
            float fadeTime = 0.4f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            sr.enabled = false;
        }

        // Phase 3: Wait remaining time until 4.0 seconds total since death
        yield return new WaitForSeconds(1.2f);

        // Phase 4: Respawn Player at death location
        Respawn();
    }

    public void Respawn()
    {
        transform.position = deathPosition;
        currentHealth = maxHealth;
        currentMana = maxMana;

        // Re-enable SpriteRenderer and reset color
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true;
            sr.color = Color.white;
        }

        // Re-enable Collider & Rigidbody physics
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }

        // Re-enable Animator & PlayerMovement
        if (anim != null) anim.enabled = true;
        if (pm != null) pm.enabled = true;

        isDead = false;

        // Notify UI of full health/mana recovery
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnAnyPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
        OnManaChanged?.Invoke(currentMana, maxMana);

        OnPlayerRespawn?.Invoke();

        Debug.Log("<color=green><b>[PlayerStats] Player đã HỒI SINH thành công tại chỗ cũ sau 4 giây! Máu & Mana 100%.</b></color>");
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
