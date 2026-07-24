using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script quản lý Máu & Thanh máu trực quan 2D cho Quái vật.
/// Tự động căn đỉnh đầu theo Sprite bounds, không bị lật hình khi quái xoay mặt.
/// Hiển thị trực quan trong cả Scene View (Editor) lẫn Game View (Runtime).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyHealth : MonoBehaviour
{
    [Header("=== THÔNG SỐ MÁU ===")]
    [Tooltip("Máu tối đa của quái vật")]
    public int maxHealth = 50;

    [SerializeField] private int _currentHealth = 50;
    public int currentHealth
    {
        get
        {
            if (_currentHealth <= 0 && !isDead && maxHealth > 0) _currentHealth = maxHealth;
            return _currentHealth;
        }
        set => _currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }

    [Header("=== THANH MÁU TRỰC QUAN (WORLD SPRITE) ===")]
    [Tooltip("Luôn hiển thị thanh máu trên đầu quái")]
    public bool alwaysShowHealthBar = true;

    [Tooltip("Chiều rộng tổng thể của thanh máu (chuẩn Pixel Art 0.28 unit)")]
    public float barWidth = 0.28f;

    [Tooltip("Chiều cao tổng thể của thanh máu (chuẩn Pixel Art 0.04 unit)")]
    public float barHeight = 0.04f;

    [Tooltip("Khoảng cách hở trên đỉnh đầu của quái vật (unit)")]
    public float barHeightOffset = 0.06f;

    // Thuộc tính hỗ trợ tương thích ngược (Backward compatibility)
    public float healthBarOffsetY
    {
        get => barHeightOffset;
        set => barHeightOffset = value;
    }

    [Tooltip("Tốc độ mượt của thanh giảm máu đỏ (Catch-up bar)")]
    public float catchUpSpeed = 3f;

    // Thành phần Unity
    private SpriteRenderer sr;
    private Animator anim;
    private EnemyAI enemyAI;
    private Collider2D col;
    private Rigidbody2D rb;

    // GameObjects cho Thanh Máu Sprite
    private GameObject healthBarParent;
    private SpriteRenderer bgSpriteRenderer;
    private SpriteRenderer catchUpSpriteRenderer;
    private SpriteRenderer fillSpriteRenderer;

    // Sprites tạo động
    private static Sprite centerUnitSprite; // Pivot center (0.5, 0.5)
    private static Sprite leftUnitSprite;   // Pivot left (0.0, 0.5)

    private float catchUpFillRatio = 1f;
    private bool isDead = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        enemyAI = GetComponent<EnemyAI>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (maxHealth <= 0) maxHealth = 50;

        catchUpFillRatio = maxHealth > 0 ? (float)currentHealth / maxHealth : 1f;
        CreateSpriteHealthBar();
        UpdateHealthBarUI(true);
    }

    void Start()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (maxHealth <= 0) maxHealth = 50;

        catchUpFillRatio = maxHealth > 0 ? (float)currentHealth / maxHealth : 1f;
        CreateSpriteHealthBar();
        UpdateHealthBarUI(true);
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            // Hiệu ứng thanh đỏ tuột từ từ khi nhận sát thương trong lúc chơi Game
            float targetFill = Mathf.Clamp01((float)currentHealth / maxHealth);
            if (catchUpFillRatio > targetFill)
            {
                catchUpFillRatio = Mathf.MoveTowards(catchUpFillRatio, targetFill, Time.deltaTime * catchUpSpeed);
                UpdateCatchUpBarUI();
            }
            else
            {
                catchUpFillRatio = targetFill;
            }
        }
        else
        {
            // Trong Editor mode: Luôn giữ thanh máu tạo chuẩn và hiển thị đúng
            CreateSpriteHealthBar();
            UpdateHealthBarUI(true);
        }
    }

    void LateUpdate()
    {
        UpdateHealthBarPosition();
    }

    /// <summary>
    /// Cập nhật vị trí đỉnh đầu và giữ thanh máu không bị xoay/lật theo quái vật
    /// </summary>
    private void UpdateHealthBarPosition()
    {
        if (healthBarParent == null || isDead) return;
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        // 1. Tính toán vị trí đỉnh đầu theo SpriteRenderer bounds
        Vector3 targetWorldPos;
        if (sr != null && sr.sprite != null)
        {
            float topY = sr.bounds.max.y + barHeightOffset;
            float centerX = sr.bounds.center.x;
            targetWorldPos = new Vector3(centerX, topY, transform.position.z);
        }
        else
        {
            targetWorldPos = transform.position + new Vector3(0f, 0.6f + barHeightOffset, 0f);
        }

        // Đặt vị trí World cho healthBarParent
        healthBarParent.transform.position = targetWorldPos;

        // 2. Chống lật (Anti-Flip): Giữ rotation phẳng và scale dương độc lập với scale quái vật
        healthBarParent.transform.rotation = Quaternion.identity;

        // Tải scale tổng của parent quái vật để triệt tiêu lật scale X
        Vector3 parentLossyScale = transform.lossyScale;
        float signX = parentLossyScale.x < 0 ? -1f : 1f;
        float signY = parentLossyScale.y < 0 ? -1f : 1f;

        healthBarParent.transform.localScale = new Vector3(
            Mathf.Abs(parentLossyScale.x) > 0.001f ? signX / parentLossyScale.x : 1f,
            Mathf.Abs(parentLossyScale.y) > 0.001f ? signY / parentLossyScale.y : 1f,
            1f
        );
    }

    /// <summary>
    /// Khởi tạo cấu trúc GameObject Thanh máu chuẩn
    /// </summary>
    public void CreateSpriteHealthBar()
    {
        // Xóa child cũ hỏng nếu có
        Transform oldChild = transform.Find("HealthBar_SpriteOverlay");
        if (oldChild != null)
        {
            if (Application.isPlaying) Destroy(oldChild.gameObject);
            else DestroyImmediate(oldChild.gameObject);
        }

        // Kiểm tra nếu đã tồn tại GameObject thanh máu mới
        Transform existingChild = transform.Find("HealthBar_Overlay");
        if (existingChild != null)
        {
            healthBarParent = existingChild.gameObject;
            Transform bg = healthBarParent.transform.Find("BG");
            if (bg != null) bgSpriteRenderer = bg.GetComponent<SpriteRenderer>();

            Transform catchUp = healthBarParent.transform.Find("CatchUp");
            if (catchUp != null) catchUpSpriteRenderer = catchUp.GetComponent<SpriteRenderer>();

            Transform fill = healthBarParent.transform.Find("Fill");
            if (fill != null) fillSpriteRenderer = fill.GetComponent<SpriteRenderer>();

            if (bgSpriteRenderer != null && fillSpriteRenderer != null && catchUpSpriteRenderer != null)
            {
                return;
            }
        }

        // 1. Tạo GameObject Cha
        healthBarParent = new GameObject("HealthBar_Overlay");
        healthBarParent.transform.SetParent(transform, false);

        EnsureSpritesCreated();

        float borderPadding = 0.008f;
        float outerWidth = barWidth + (borderPadding * 2f);
        float outerHeight = barHeight + (borderPadding * 2f);

        // 2. Tạo Khung Nền Đen (BG) - Pivot Center
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(healthBarParent.transform, false);
        bgSpriteRenderer = bgObj.AddComponent<SpriteRenderer>();
        bgSpriteRenderer.sprite = centerUnitSprite;
        bgSpriteRenderer.color = new Color(0.05f, 0.05f, 0.05f, 0.92f);
        bgSpriteRenderer.sortingOrder = 997;
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(outerWidth, outerHeight, 1f);

        // 3. Tạo Thanh Giảm Máu Đỏ (CatchUp) - Pivot Left
        GameObject catchUpObj = new GameObject("CatchUp");
        catchUpObj.transform.SetParent(healthBarParent.transform, false);
        catchUpSpriteRenderer = catchUpObj.AddComponent<SpriteRenderer>();
        catchUpSpriteRenderer.sprite = leftUnitSprite;
        catchUpSpriteRenderer.color = new Color(0.9f, 0.2f, 0.2f, 0.9f);
        catchUpSpriteRenderer.sortingOrder = 998;
        catchUpObj.transform.localPosition = new Vector3(-barWidth * 0.5f, 0f, 0f);
        catchUpObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        // 4. Tạo Thanh Điền Máu (Fill Bar) - Pivot Left
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(healthBarParent.transform, false);
        fillSpriteRenderer = fillObj.AddComponent<SpriteRenderer>();
        fillSpriteRenderer.sprite = leftUnitSprite;
        fillSpriteRenderer.color = new Color(0.15f, 0.85f, 0.2f, 1.0f);
        fillSpriteRenderer.sortingOrder = 999;
        fillObj.transform.localPosition = new Vector3(-barWidth * 0.5f, 0f, 0f);
        fillObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        healthBarParent.SetActive(alwaysShowHealthBar);
    }

    private static void EnsureSpritesCreated()
    {
        if (centerUnitSprite == null || leftUnitSprite == null)
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] colors = new Color[16];
            for (int i = 0; i < 16; i++) colors[i] = Color.white;
            tex.SetPixels(colors);
            tex.Apply();

            // Sprite có Pivot tại (0.5, 0.5) cho Nền
            centerUnitSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            // Sprite có Pivot tại (0.0, 0.5) cho Fill Bar (Left-aligned)
            leftUnitSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.0f, 0.5f), 4f);
        }
    }

    /// <summary>
    /// Cập nhật hiển thị tỷ lệ máu và màu sắc
    /// </summary>
    public void UpdateHealthBarUI(bool immediateCatchUp = false)
    {
        float fillRatio = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 1f;

        if (fillSpriteRenderer != null)
        {
            fillSpriteRenderer.transform.localPosition = new Vector3(-barWidth * 0.5f, 0f, 0f);
            fillSpriteRenderer.transform.localScale = new Vector3(barWidth * fillRatio, barHeight, 1f);

            // Chuyển màu linh hoạt: Máu > 50% (Green -> Yellow), Máu < 50% (Yellow -> Red)
            if (fillRatio > 0.5f)
            {
                fillSpriteRenderer.color = Color.Lerp(Color.yellow, new Color(0.15f, 0.88f, 0.25f), (fillRatio - 0.5f) * 2f);
            }
            else
            {
                fillSpriteRenderer.color = Color.Lerp(new Color(0.95f, 0.15f, 0.15f), Color.yellow, fillRatio * 2f);
            }
        }

        if (immediateCatchUp)
        {
            catchUpFillRatio = fillRatio;
            UpdateCatchUpBarUI();
        }

        if (healthBarParent != null)
        {
            if (alwaysShowHealthBar || currentHealth < maxHealth)
            {
                healthBarParent.SetActive(!isDead);
            }
        }
    }

    private void UpdateCatchUpBarUI()
    {
        if (catchUpSpriteRenderer != null)
        {
            catchUpSpriteRenderer.transform.localPosition = new Vector3(-barWidth * 0.5f, 0f, 0f);
            catchUpSpriteRenderer.transform.localScale = new Vector3(barWidth * catchUpFillRatio, barHeight, 1f);
        }
    }

    /// <summary>
    /// Nhận sát thương từ Player
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"<color=red>[Damage System]</color> Quái <color=yellow>{gameObject.name}</color> nhận <color=red>-{damage} HP</color>! Máu còn: {currentHealth}/{maxHealth}");

        StartCoroutine(DamageFlashRoutine());

        TriggerAnim("Hurt");
        TriggerAnim("GetHit");

        UpdateHealthBarUI(false);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Hồi máu cho quái vật
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth += amount;
        UpdateHealthBarUI(true);
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"<color=red>[Enemy Defeated]</color> Quái <color=yellow>{gameObject.name}</color> đã bị tiêu diệt!");

        TriggerAnim("Death");
        TriggerAnim("Die");

        if (enemyAI != null) enemyAI.enabled = false;
        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (healthBarParent != null)
        {
            Destroy(healthBarParent);
        }

        Destroy(gameObject, 1.5f);
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (sr != null)
        {
            Color originalColor = sr.color;
            sr.color = new Color(1f, 0.35f, 0.35f, 1f);
            yield return new WaitForSeconds(0.12f);
            sr.color = originalColor;
        }
    }

    private void TriggerAnim(string paramName)
    {
        if (anim != null)
        {
            foreach (AnimatorControllerParameter param in anim.parameters)
            {
                if (param.name == paramName && param.type == AnimatorControllerParameterType.Trigger)
                {
                    anim.SetTrigger(paramName);
                    break;
                }
            }
        }
    }
}
