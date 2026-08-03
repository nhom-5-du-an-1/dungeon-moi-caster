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
    [Tooltip("Máu tối đa của quái vật (Chuẩn Minecraft: 10 HP = 5 Trái tim)")]
    public int maxHealth = 10;

    [SerializeField] private int _currentHealth = 10;
    public int currentHealth
    {
        get => _currentHealth;
        set => _currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }

    [Header("=== KINH NGHIỆM (XP) ===")]
    [Tooltip("Lượng kinh nghiệm quái vật rơi ra khi chết")]
    public int xpValue = 15;
    [Tooltip("Sprite hạt kinh nghiệm (Nếu trống, tự động tìm xp_orb.png)")]
    public Sprite xpOrbSprite;

    [Header("=== HỒI SINH (RESPAWN) ===")]
    [Tooltip("Tự động hồi sinh tại vị trí ban đầu sau khi chết")]
    public bool autoRespawn = true;

    [Tooltip("Thời gian chờ hồi sinh sau khi hoạt ảnh chết kết thúc (giây)")]
    public float respawnDelay = 5.0f;

    private Vector3 spawnPoint;

    [Header("=== THANH MÁU TRỰC QUAN (WORLD SPRITE) ===")]
    [Tooltip("Luôn hiển thị thanh máu trên đầu quái")]
    public bool alwaysShowHealthBar = false;

    [Tooltip("Chiều rộng tổng thể của thanh máu (chuẩn Pixel Art 0.20 unit)")]
    public float barWidth = 0.20f;

    [Tooltip("Chiều cao tổng thể của thanh máu (chuẩn Pixel Art 0.03 unit)")]
    public float barHeight = 0.03f;

    [Tooltip("Khoảng cách hở trên đỉnh đầu của quái vật (unit)")]
    public float barHeightOffset = 0.06f;

    private float showHealthBarTimer = 0f;

    // Thuộc tính hỗ trợ tương thích ngược (Backward compatibility)
    public float healthBarOffsetY
    {
        get => barHeightOffset;
        set => barHeightOffset = value;
    }

    [Tooltip("Tốc độ mượt của thanh giảm máu đỏ (Catch-up bar)")]
    public float catchUpSpeed = 3f;

    [Header("=== MINECRAFT HEART DISPLAY ===")]
    [Tooltip("Hiển thị máu quái dạng Trái tim Minecraft")]
    public bool useHeartDisplay = true;
    public Sprite enemyHeartFull;
    public Sprite enemyHeartHalf;
    public Sprite enemyHeartEmpty;
    [Tooltip("Số lượng trái tim hiển thị trên đầu quái (mặc định 5 tim)")]
    public int heartCount = 5;

    [Tooltip("Khoảng cách dịch ngang X của tim so với tâm quái")]
    public float heartOffsetX = 0f;

    [Tooltip("Khoảng cách dịch cao Y của tim so với đỉnh đầu quái")]
    public float heartOffsetY = 0.12f;

    [Tooltip("Kích thước mỗi trái tim trên đầu quái")]
    public float heartScale = 0.14f;

    [Tooltip("Khoảng cách giữa các trái tim")]
    public float heartSpacing = 0.045f;

    // Thành phần Unity
    private SpriteRenderer sr;
    private Animator anim;
    private EnemyAI enemyAI;
    private BossOrcAI bossAI;
    private OrcWarriorAI orcWarriorAI;
    private OrcShamanAI orcShamanAI;
    private OrcBerserkerAI orcBerserkerAI;
    private Collider2D col;
    private Rigidbody2D rb;

    // GameObjects cho Thanh Máu Sprite
    private GameObject healthBarParent;
    private SpriteRenderer bgSpriteRenderer;
    private SpriteRenderer catchUpSpriteRenderer;
    private SpriteRenderer fillSpriteRenderer;

    private List<SpriteRenderer> heartRenderers = new List<SpriteRenderer>();

    // Sprites tạo động
    private static Sprite centerUnitSprite; // Pivot center (0.5, 0.5)
    private static Sprite leftUnitSprite;   // Pivot left (0.0, 0.5)

    private float catchUpFillRatio = 1f;
    private bool isDead = false;
    private Vector3 baseScale;
    private Quaternion baseRotation;
    private Coroutine hitReactionCoroutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        enemyAI = GetComponent<EnemyAI>();
        bossAI = GetComponent<BossOrcAI>();
        orcWarriorAI = GetComponent<OrcWarriorAI>();
        orcShamanAI = GetComponent<OrcShamanAI>();
        orcBerserkerAI = GetComponent<OrcBerserkerAI>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
    }

    void OnEnable()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (maxHealth <= 0) maxHealth = 10;

        if (_currentHealth <= 0 && !isDead)
        {
            _currentHealth = maxHealth;
        }

        catchUpFillRatio = maxHealth > 0 ? (float)currentHealth / maxHealth : 1f;
        CreateSpriteHealthBar();
        UpdateHealthBarUI(true);
    }

    void Start()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (maxHealth <= 0) maxHealth = 10;

        if (Application.isPlaying)
        {
            spawnPoint = transform.position;
        }

        if (_currentHealth <= 0 && !isDead)
        {
            _currentHealth = maxHealth;
        }

        catchUpFillRatio = maxHealth > 0 ? (float)currentHealth / maxHealth : 1f;
        CreateSpriteHealthBar();
        UpdateHealthBarUI(true);
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            // Tự động ẩn thanh máu sau 3.5 giây không nhận sát thương
            if (showHealthBarTimer > 0f)
            {
                showHealthBarTimer -= Time.deltaTime;
                if (showHealthBarTimer <= 0f && !alwaysShowHealthBar && healthBarParent != null)
                {
                    healthBarParent.SetActive(false);
                }
            }

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

        float offsetY = useHeartDisplay ? heartOffsetY : barHeightOffset;
        float offsetX = useHeartDisplay ? heartOffsetX : 0f;

        // 1. Tính toán vị trí đỉnh đầu theo SpriteRenderer bounds
        Vector3 targetWorldPos;
        if (sr != null && sr.sprite != null)
        {
            float topY = sr.bounds.max.y + offsetY;
            float centerX = sr.bounds.center.x + offsetX;
            targetWorldPos = new Vector3(centerX, topY, transform.position.z);
        }
        else
        {
            targetWorldPos = transform.position + new Vector3(offsetX, 0.6f + offsetY, 0f);
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

        if (useHeartDisplay)
        {
            CreateHeartHealthBar();
        }
    }

    private void EnsureHeartSpritesLoaded()
    {
        #if UNITY_EDITOR
        if (enemyHeartFull == null)
            enemyHeartFull = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_enemy.png") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_full.png");
        if (enemyHeartHalf == null)
            enemyHeartHalf = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_half.png");
        if (enemyHeartEmpty == null)
            enemyHeartEmpty = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_empty.png");
        #endif
    }

    private void CreateHeartHealthBar()
    {
        EnsureHeartSpritesLoaded();
        heartRenderers.Clear();

        if (healthBarParent == null) return;

        Transform existingContainerTrans = healthBarParent.transform.Find("HeartsContainer");
        GameObject heartsContainerObj;
        if (existingContainerTrans != null)
        {
            heartsContainerObj = existingContainerTrans.gameObject;
        }
        else
        {
            heartsContainerObj = new GameObject("HeartsContainer");
            heartsContainerObj.transform.SetParent(healthBarParent.transform, false);
            heartsContainerObj.transform.localPosition = Vector3.zero;
        }

        float startX = -((heartCount - 1) * heartSpacing * 0.5f);

        for (int i = 0; i < heartCount; i++)
        {
            Transform existingHeartTrans = heartsContainerObj.transform.Find($"Heart_{i}");
            GameObject hObj;
            if (existingHeartTrans != null)
            {
                hObj = existingHeartTrans.gameObject;
            }
            else
            {
                hObj = new GameObject($"Heart_{i}");
                hObj.transform.SetParent(heartsContainerObj.transform, false);
            }

            hObj.transform.localPosition = new Vector3(startX + (i * heartSpacing), 0f, 0f);
            hObj.transform.localScale = new Vector3(heartScale, heartScale, 1f);

            SpriteRenderer hSr = hObj.GetComponent<SpriteRenderer>();
            if (hSr == null) hSr = hObj.AddComponent<SpriteRenderer>();

            if (hSr.sprite == null && enemyHeartFull != null)
            {
                hSr.sprite = enemyHeartFull;
            }
            hSr.sortingOrder = 1000;
            heartRenderers.Add(hSr);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (healthBarParent != null)
        {
            Transform container = healthBarParent.transform.Find("HeartsContainer");
            if (container != null && container.childCount > 0)
            {
                Transform firstHeart = container.GetChild(0);
                if (firstHeart != null && firstHeart.localScale.x > 0.01f)
                {
                    heartScale = firstHeart.localScale.x;
                }
            }
        }

        if (useHeartDisplay)
        {
            CreateHeartHealthBar();
            UpdateHealthBarUI(true);
        }
        UpdateHealthBarPosition();
    }
#endif

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

        if (useHeartDisplay)
        {
            bool hasNullRef = false;
            for (int i = 0; i < heartRenderers.Count; i++)
            {
                if (heartRenderers[i] == null)
                {
                    hasNullRef = true;
                    break;
                }
            }

            if (heartRenderers.Count != heartCount || hasNullRef)
            {
                CreateHeartHealthBar();
            }

            int totalHeartUnits = heartCount * 2;
            int currentUnits = Mathf.RoundToInt(fillRatio * totalHeartUnits);

            for (int i = 0; i < heartRenderers.Count; i++)
            {
                if (heartRenderers[i] == null) continue;

                int val = (i + 1) * 2;
                if (currentUnits >= val)
                {
                    heartRenderers[i].sprite = enemyHeartFull;
                    heartRenderers[i].enabled = true;
                }
                else if (currentUnits == val - 1)
                {
                    heartRenderers[i].sprite = enemyHeartHalf;
                    heartRenderers[i].enabled = true;
                }
                else
                {
                    heartRenderers[i].sprite = enemyHeartEmpty;
                    heartRenderers[i].enabled = true;
                }
            }

            if (bgSpriteRenderer != null) bgSpriteRenderer.enabled = false;
            if (catchUpSpriteRenderer != null) catchUpSpriteRenderer.enabled = false;
            if (fillSpriteRenderer != null) fillSpriteRenderer.enabled = false;
        }
        else
        {
            if (bgSpriteRenderer != null) bgSpriteRenderer.enabled = true;
            if (catchUpSpriteRenderer != null) catchUpSpriteRenderer.enabled = true;
            if (fillSpriteRenderer != null) fillSpriteRenderer.enabled = true;

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
    /// <summary>
    /// Nhận sát thương từ Player (Tự động nhấp nháy đỏ & Bật lùi về phía sau)
    /// </summary>
    public void TakeDamage(int damage, Vector2 attackerPos = default, float knockbackForce = 3.8f)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"<color=red>[Damage System]</color> Quái <color=yellow>{gameObject.name}</color> nhận <color=red>-{damage} HP</color>! Máu còn: {currentHealth}/{maxHealth}");

        // Hiển thị số sát thương nhảy lên trên đầu quái vật
        FloatingTextManager.SpawnWorldText($"-{damage}", transform.position + Vector3.up * (barHeightOffset + 0.25f), new Color(1.0f, 0.9f, 0.2f, 1.0f));

        // Hiện thanh máu trong 3.5s khi bị đánh
        showHealthBarTimer = 3.5f;
        if (healthBarParent != null)
        {
            healthBarParent.SetActive(true);
        }

        // Dừng coroutine cũ nếu đang chạy để không bị nhân dồn biến dạng
        if (hitReactionCoroutine != null)
        {
            StopCoroutine(hitReactionCoroutine);
        }
        hitReactionCoroutine = StartCoroutine(HitReactionRoutine(attackerPos, knockbackForce));

        TriggerAnim("Hurt");
        TriggerAnim("GetHit");

        UpdateHealthBarUI(false);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Hiệu ứng Bật Lùi (Knockback) & Nhấp Nháy Đỏ chuẩn xác (Không bao giờ bị biến dạng)
    /// </summary>
    private IEnumerator HitReactionRoutine(Vector2 attackerPos, float knockbackForce)
    {
        // Khôi phục ngay lập tục về kích thước và góc xoay chuẩn ban đầu
        transform.localScale = baseScale;
        transform.localRotation = baseRotation;

        // 1. Nhấp nháy màu đỏ rực tươi khi trúng đòn
        if (sr != null)
        {
            sr.color = new Color(1.0f, 0.25f, 0.25f, 1.0f);
        }

        // 2. Tính hướng bị đánh bật lùi về phía sau
        Vector2 pushDir = Vector2.zero;
        if (attackerPos != default)
        {
            pushDir = ((Vector2)transform.position - attackerPos).normalized;
        }
        if (pushDir == Vector2.zero)
        {
            pushDir = sr != null && sr.flipX ? Vector2.right : Vector2.left;
        }

        // 3. Lực đẩy bật lùi (Knockback Physics Push)
        float duration = 0.12f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float step = (knockbackForce * (1f - (timer / duration))) * Time.deltaTime;

            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                rb.position += pushDir * step;
            }
            else
            {
                transform.position += (Vector3)(pushDir * step);
            }

            yield return null;
        }

        // 4. Khôi phục màu sắc, kích thước và góc xoay ban đầu chuẩn 100%
        if (sr != null) sr.color = Color.white;
        transform.localRotation = baseRotation;
        transform.localScale = baseScale;
        hitReactionCoroutine = null;
    }

    /// <summary>
    /// Nội tại Hỏa Thiêu: Đốt cháy quái vật trừ máu liên tục theo thời gian
    /// </summary>
    public void ApplyBurn(int totalDamage = 25, float duration = 2.5f)
    {
        if (isDead) return;
        StartCoroutine(BurnRoutine(totalDamage, duration));
    }

    private IEnumerator BurnRoutine(int totalDamage, float duration)
    {
        float timer = 0f;
        float tickInterval = 0.5f;
        int tickCount = Mathf.CeilToInt(duration / tickInterval);
        int damagePerTick = Mathf.Max(1, totalDamage / tickCount);

        FloatingTextManager.SpawnWorldText("🔥 HỎA THIÊU!", transform.position + Vector3.up * 0.4f, new Color(1.0f, 0.4f, 0.0f, 1.0f));

        while (timer < duration && !isDead)
        {
            timer += tickInterval;
            if (sr != null) sr.color = new Color(1.0f, 0.35f, 0.1f, 1.0f); // Màu da cam rực lửa

            currentHealth -= damagePerTick;
            FloatingTextManager.SpawnWorldText($"-{damagePerTick}", transform.position + Vector3.up * 0.25f, new Color(1.0f, 0.3f, 0.0f, 1.0f));
            UpdateHealthBarUI(false);

            if (currentHealth <= 0)
            {
                Die();
                yield break;
            }

            yield return new WaitForSeconds(tickInterval);
        }

        if (!isDead && sr != null) sr.color = Color.white;
    }

    public bool isFrozen { get; private set; }

    /// <summary>
    /// Đóng băng quái vật tê liệt không thể di chuyển hay tấn công trong duration (giây)
    /// </summary>
    public void Freeze(float duration)
    {
        if (isDead) return;
        StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        isFrozen = true;
        if (enemyAI != null) enemyAI.enabled = false;
        if (bossAI != null) bossAI.enabled = false;
        if (orcWarriorAI != null) orcWarriorAI.enabled = false;
        if (orcShamanAI != null) orcShamanAI.enabled = false;
        if (orcBerserkerAI != null) orcBerserkerAI.enabled = false;
        if (sr != null) sr.color = new Color(0.3f, 0.75f, 1.0f, 1.0f); // Màu xanh băng tuyết

        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Hiển thị chữ ĐÓNG BẰNG
        FloatingTextManager.SpawnWorldText("❄️ ĐÓNG BẰNG!", transform.position + Vector3.up * 0.4f, new Color(0.3f, 0.8f, 1.0f, 1.0f));

        yield return new WaitForSeconds(duration);

        isFrozen = false;
        if (!isDead)
        {
            if (enemyAI != null) enemyAI.enabled = true;
            if (bossAI != null) bossAI.enabled = true;
            if (orcWarriorAI != null) orcWarriorAI.enabled = true;
            if (orcShamanAI != null) orcShamanAI.enabled = true;
            if (orcBerserkerAI != null) orcBerserkerAI.enabled = true;
            if (sr != null) sr.color = Color.white;
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

        // Rơi hạt kinh nghiệm XP Orbs
        SpawnXpOrbs();

        if (enemyAI != null) enemyAI.enabled = false;
        if (bossAI != null) bossAI.enabled = false;
        if (orcWarriorAI != null) orcWarriorAI.enabled = false;
        if (orcShamanAI != null) orcShamanAI.enabled = false;
        if (orcBerserkerAI != null) orcBerserkerAI.enabled = false;
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

        if (autoRespawn)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            Destroy(gameObject, 1.5f);
        }
    }

    private IEnumerator RespawnRoutine()
    {
        // 1. Chờ xem xong hoạt ảnh chết
        yield return new WaitForSeconds(1.5f);

        // 2. Ẩn hình ảnh và tắt va chạm hoàn toàn
        if (sr != null) sr.enabled = false;

        // 3. Đợi thời gian hồi sinh
        yield return new WaitForSeconds(respawnDelay);

        // 4. Đưa quái về vị trí xuất phát ban đầu
        transform.position = spawnPoint;

        // 5. Reset lại máu
        _currentHealth = maxHealth;
        isDead = false;

        // 6. Kích hoạt lại hiển thị, va chạm và vật lý
        if (sr != null)
        {
            sr.enabled = true;
            sr.color = Color.white; // Reset màu nếu bị nhuộm đỏ/đóng băng
        }
        if (col != null) col.enabled = true;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }

        // 7. Tạo lại thanh máu
        CreateSpriteHealthBar();
        UpdateHealthBarUI(true);

        // 8. Bật lại các Script AI để quái hoạt động tiếp
        if (enemyAI != null) enemyAI.enabled = true;
        if (bossAI != null) bossAI.enabled = true;

        if (orcWarriorAI != null)
        {
            orcWarriorAI.enabled = true;
            orcWarriorAI.ResetState();
        }
        if (orcShamanAI != null)
        {
            orcShamanAI.enabled = true;
            orcShamanAI.ResetState();
        }
        if (orcBerserkerAI != null)
        {
            orcBerserkerAI.enabled = true;
            orcBerserkerAI.ResetState();
        }

        // Kích hoạt lại trạng thái Idle
        TriggerAnim("Idle");
        if (anim != null)
        {
            anim.Play("Idle"); // Phát trực tiếp clip Idle phòng khi trôi trigger
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

    /// <summary>
    /// Rơi các hạt kinh nghiệm xung quanh vị trí quái bị tiêu diệt.
    /// </summary>
    private void SpawnXpOrbs()
    {
        if (xpValue <= 0) return;

        int remainingXp = xpValue;
        Sprite xpSprite = xpOrbSprite;
        if (xpSprite == null)
        {
            xpSprite = Resources.Load<Sprite>("xp_orb");
#if UNITY_EDITOR
            if (xpSprite == null)
            {
                xpSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/xp_orb.png");
            }
#endif
        }

        while (remainingXp > 0)
        {
            int orbValue = Random.Range(2, 6); // Mỗi hạt từ 2 -> 5 XP
            if (orbValue > remainingXp) orbValue = remainingXp;
            remainingXp -= orbValue;

            GameObject orbObj = new GameObject($"XpOrb_{orbValue}Xp", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(XpOrb));
            
            // Độ lệch nhỏ
            Vector3 offset = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.1f, 0.25f), 0f);
            orbObj.transform.position = transform.position + offset;

            SpriteRenderer orbSr = orbObj.GetComponent<SpriteRenderer>();
            if (orbSr != null)
            {
                orbSr.sprite = xpSprite;
                if (sr != null)
                {
                    orbSr.sortingOrder = sr.sortingOrder + 1;
                }
                else
                {
                    orbSr.sortingOrder = 10;
                }
            }

            XpOrb orbScript = orbObj.GetComponent<XpOrb>();
            if (orbScript != null)
            {
                orbScript.xpValue = orbValue;
                orbScript.magnetRange = 3.5f + Random.Range(0f, 1f);
            }
        }
    }
}
