using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QUÁI QUỶ 3 - ORC CUỒNG NỘ (Orc Berserker)
/// Loại quái cận chiến hung hãn với cơ chế "Càng ít máu càng mạnh".
/// - Chém Rìu (Axe Slash): Đòn chém cận chiến cơ bản
/// - Lao Đâm (Berserk Charge): Lao thẳng về phía Player với tốc độ cực cao
/// - Cuồng Nộ (Rage Passive): Tự động tăng tốc độ + sát thương khi máu giảm dần
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class OrcBerserkerAI : MonoBehaviour
{
    public enum BerserkerState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Charging,      // Đang lao đâm
        ReturnToSpawn
    }

    [Header("=== MỤC TIÊU & NHẬN DIỆN ===")]
    [Tooltip("Mục tiêu theo dõi (Nếu để trống, tự tìm Player có tag 'Player')")]
    public Transform playerTarget;
    [Tooltip("Bán kính phát hiện Player")]
    public float detectionRadius = 6.0f;
    [Tooltip("Bán kính tối đa trước khi bỏ đuổi")]
    public float loseSightRadius = 10.0f;
    [Tooltip("Khoảng cách tầm đánh cận chiến")]
    public float attackRange = 0.48f;

    [Header("=== DI CHUYỂN ===")]
    [Tooltip("Tốc độ di chuyển cơ bản khi đuổi theo")]
    public float moveSpeed = 3.0f;
    [Tooltip("Tốc độ đi tuần")]
    public float patrolSpeed = 1.3f;
    [Range(0f, 0.5f)] public float movementSmoothing = 0.04f;

    [Header("=== CHÉM RÌU (AXE SLASH) ===")]
    [Tooltip("Sát thương đòn chém cơ bản")]
    public int slashDamage = 3;
    [Tooltip("Thời gian hồi chiêu chém")]
    public float slashCooldown = 0.9f;
    [Tooltip("Thời gian thực hiện đòn chém")]
    public float slashDuration = 0.4f;
    [Tooltip("Thời gian trễ trước khi gây sát thương")]
    public float slashDelay = 0.25f;
    [Tooltip("Lực lao tới khi chém")]
    public float slashLungeForce = 2.0f;
    [Tooltip("Bán kính hitbox chém")]
    public float slashRadius = 0.38f;

    [Header("=== LAO ĐÂM (BERSERK CHARGE) ===")]
    [Tooltip("Khoảng cách kích hoạt Lao Đâm (đủ xa để dùng charge)")]
    public float chargeMinDistance = 2.5f;
    [Tooltip("Khoảng cách tối đa để kích hoạt Lao Đâm")]
    public float chargeMaxDistance = 7.0f;
    [Tooltip("Sát thương lao đâm")]
    public int chargeDamage = 5;
    [Tooltip("Tốc độ lao đâm")]
    public float chargeSpeed = 10.0f;
    [Tooltip("Thời gian lao đâm tối đa (giây)")]
    public float chargeMaxDuration = 0.6f;
    [Tooltip("Thời gian hồi chiêu lao đâm")]
    public float chargeCooldown = 6.0f;
    [Tooltip("Thời gian cảnh báo trước khi lao (giây)")]
    public float chargeWindup = 0.5f;
    [Tooltip("Lực knockback khi lao đâm trúng")]
    public float chargeKnockback = 8.0f;
    [Tooltip("Bán kính va chạm khi lao đâm")]
    public float chargeHitRadius = 0.5f;

    [Header("=== CUỒNG NỘ (RAGE PASSIVE) ===")]
    [Tooltip("Hệ số tăng tốc độ tối đa khi máu = 0% (VD: 1.8 = +80% speed)")]
    public float maxRageSpeedMultiplier = 1.8f;
    [Tooltip("Hệ số tăng sát thương tối đa khi máu = 0% (VD: 2.0 = x2 damage)")]
    public float maxRageDamageMultiplier = 2.0f;
    [Tooltip("Tỷ lệ HP bắt đầu kích hoạt Rage (VD: 0.75 = bắt đầu rage khi dưới 75% HP)")]
    public float rageStartThreshold = 0.75f;

    [Header("=== HITBOX & HIỂN THỊ ===")]
    [Tooltip("Khoảng cách lệch từ tâm đến tâm đòn chém")]
    public float attackOffset = 0.22f;
    [Tooltip("Độ lệch tâm hình ảnh")]
    public Vector2 spriteOffset = Vector2.zero;
    public LayerMask playerLayer;
    public bool showHitbox = true;

    // Thành phần Unity
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private LineRenderer hitboxLineRenderer;
    private EnemyHealth healthComponent;

    // Trạng thái AI
    private BerserkerState currentState = BerserkerState.Idle;
    private Vector3 spawnPoint;
    private Vector3 patrolTarget;
    private float stateTimer = 0f;
    private bool isAttacking = false;
    private float nextSlashTime = 0f;
    private float nextChargeTime = 0f;
    private Vector2 lastFacingDirection = new Vector2(1f, 0f);
    private Vector2 smoothMovement;
    private Vector2 velocityWorkspace = Vector2.zero;
    private HashSet<string> animParamsCache;
    private PlayerStats playerStatsTarget;

    // Rage tracking
    private float currentRageMultiplier = 1.0f;
    private Color rageColor = Color.white;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        spawnPoint = transform.position;
        patrolTarget = GetRandomPatrolPoint();

        CacheAnimatorParameters();
        SetupHitboxVisualizer();
        EnsureEnemyHealthComponent();
        FindPlayerTarget();
    }

    private void EnsureEnemyHealthComponent()
    {
        healthComponent = GetComponent<EnemyHealth>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<EnemyHealth>();
        }

        // Quái Quỷ 3: 20 HP = 10 trái tim (Cuồng chiến cực bền bỉ)
        if (healthComponent.maxHealth <= 0 || healthComponent.maxHealth == 50)
        {
            healthComponent.maxHealth = 20;
            healthComponent.currentHealth = 20;
        }
        healthComponent.useHeartDisplay = true;
        healthComponent.heartCount = 10;
        healthComponent.alwaysShowHealthBar = true;
        healthComponent.barHeightOffset = 0.08f;
        healthComponent.barWidth = 0.36f;
        healthComponent.barHeight = 0.04f;
    }

    public Vector3 GetCenterPosition()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            return transform.position + (Vector3)col.offset;
        }
        return transform.position + (Vector3)spriteOffset;
    }

    public Vector3 GetPlayerCenterPosition()
    {
        if (playerTarget == null) return transform.position;

        PlayerMovement pm = playerTarget.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            return pm.GetCenterPosition();
        }

        SpriteRenderer playerSr = playerTarget.GetComponent<SpriteRenderer>();
        if (playerSr != null && playerSr.sprite != null)
        {
            return playerSr.bounds.center;
        }

        Collider2D col = playerTarget.GetComponent<Collider2D>();
        if (col != null)
        {
            return playerTarget.position + (Vector3)col.offset;
        }

        return playerTarget.position;
    }

    public bool IsPlayerInAttackZone()
    {
        if (playerTarget == null) return false;

        Vector3 myPos = GetCenterPosition();
        Vector3 playerPos = GetPlayerCenterPosition();
        float dist = Vector2.Distance(myPos, playerPos);

        return dist <= attackRange;
    }

    /// <summary>
    /// Cập nhật hệ số Cuồng Nộ (Rage) dựa trên % máu hiện tại.
    /// Càng ít máu → Càng mạnh (tốc độ + sát thương tăng dần)
    /// </summary>
    private void UpdateRageState()
    {
        if (healthComponent == null || healthComponent.maxHealth <= 0) return;

        float hpRatio = (float)healthComponent.currentHealth / healthComponent.maxHealth;

        if (hpRatio <= rageStartThreshold)
        {
            // Tính tỷ lệ rage: 0.0 (vừa bắt đầu rage) → 1.0 (gần chết)
            float rageProgress = 1.0f - (hpRatio / rageStartThreshold);
            rageProgress = Mathf.Clamp01(rageProgress);

            // Nội suy hệ số tăng từ 1.0 → max
            float speedMult = Mathf.Lerp(1.0f, maxRageSpeedMultiplier, rageProgress);
            float damageMult = Mathf.Lerp(1.0f, maxRageDamageMultiplier, rageProgress);
            currentRageMultiplier = damageMult;

            // Cập nhật màu da: Trắng → Đỏ rực dần theo rage
            float redIntensity = Mathf.Lerp(1.0f, 1.0f, rageProgress);
            float greenBlue = Mathf.Lerp(1.0f, 0.3f, rageProgress);
            rageColor = new Color(redIntensity, greenBlue, greenBlue, 1.0f);

            if (sr != null && !isAttacking)
            {
                sr.color = rageColor;
            }
        }
        else
        {
            currentRageMultiplier = 1.0f;
            rageColor = Color.white;
        }
    }

    /// <summary>
    /// Lấy tốc độ di chuyển hiện tại (đã tính Rage)
    /// </summary>
    private float GetCurrentMoveSpeed()
    {
        if (healthComponent == null || healthComponent.maxHealth <= 0) return moveSpeed;

        float hpRatio = (float)healthComponent.currentHealth / healthComponent.maxHealth;
        if (hpRatio <= rageStartThreshold)
        {
            float rageProgress = 1.0f - (hpRatio / rageStartThreshold);
            return moveSpeed * Mathf.Lerp(1.0f, maxRageSpeedMultiplier, rageProgress);
        }
        return moveSpeed;
    }

    /// <summary>
    /// Lấy sát thương hiện tại (đã tính Rage)
    /// </summary>
    private int GetRageDamage(int baseDmg)
    {
        return Mathf.RoundToInt(baseDmg * currentRageMultiplier);
    }

    void Update()
    {
        if (playerTarget == null)
        {
            FindPlayerTarget();
        }

        if (playerTarget != null && playerStatsTarget == null)
        {
            playerStatsTarget = playerTarget.GetComponent<PlayerStats>();
            if (playerStatsTarget == null) playerStatsTarget = playerTarget.GetComponentInParent<PlayerStats>();
        }

        // Khi Player đã chết, quái bỏ chiến đấu đi tuần bình thường
        if (playerStatsTarget != null && playerStatsTarget.isDead)
        {
            if (currentState == BerserkerState.Chase || currentState == BerserkerState.Attack || currentState == BerserkerState.Charging)
            {
                ChangeState(BerserkerState.Patrol);
                isAttacking = false;
            }
        }

        // Cập nhật trạng thái Cuồng Nộ liên tục
        UpdateRageState();

        if (isAttacking) return;

        Vector3 myPos = GetCenterPosition();
        Vector3 playerPos = GetPlayerCenterPosition();
        float distanceToPlayer = Vector2.Distance(myPos, playerPos);

        switch (currentState)
        {
            case BerserkerState.Idle:
                stateTimer += Time.deltaTime;
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(BerserkerState.Chase);
                }
                else if (stateTimer >= 2.0f) // Ít kiên nhẫn hơn quái thường
                {
                    patrolTarget = GetRandomPatrolPoint();
                    ChangeState(BerserkerState.Patrol);
                }
                break;

            case BerserkerState.Patrol:
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(BerserkerState.Chase);
                }
                else if (Vector2.Distance(myPos, patrolTarget) < 0.2f)
                {
                    ChangeState(BerserkerState.Idle);
                }
                break;

            case BerserkerState.Chase:
                // Ưu tiên 1: Lao Đâm nếu Player ở cự ly phù hợp và chiêu sẵn sàng
                if (distanceToPlayer >= chargeMinDistance && distanceToPlayer <= chargeMaxDistance && Time.time >= nextChargeTime)
                {
                    StartCoroutine(PerformChargeRoutine());
                }
                // Ưu tiên 2: Chém Rìu khi áp sát
                else if (IsPlayerInAttackZone() && Time.time >= nextSlashTime)
                {
                    StartCoroutine(PerformSlashRoutine());
                }
                else if (distanceToPlayer > loseSightRadius)
                {
                    ChangeState(BerserkerState.ReturnToSpawn);
                }
                break;

            case BerserkerState.ReturnToSpawn:
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(BerserkerState.Chase);
                }
                else if (Vector2.Distance(myPos, spawnPoint) < 0.3f)
                {
                    ChangeState(BerserkerState.Idle);
                }
                break;
        }

        // Hướng nhìn về phía Player
        if (distanceToPlayer <= detectionRadius || isAttacking)
        {
            Vector2 dirToPlayer = (playerPos - myPos).normalized;
            if (dirToPlayer.sqrMagnitude > 0.001f)
            {
                lastFacingDirection = dirToPlayer;
                sr.flipX = (lastFacingDirection.x < -0.01f);
            }
        }
        if (smoothMovement.sqrMagnitude > 0.001f)
        {
            lastFacingDirection = smoothMovement.normalized;
            sr.flipX = (lastFacingDirection.x < -0.01f);
        }
    }

    void LateUpdate()
    {
        if (sr != null)
        {
            sr.sortingOrder = 10000 - Mathf.RoundToInt((transform.position.y + spriteOffset.y) * 100);
        }
    }

    void FixedUpdate()
    {
        if (isAttacking)
        {
            SetAnimFloat("Speed", 0f);
            return;
        }

        Vector3 myPos = GetCenterPosition();
        Vector3 playerPos = GetPlayerCenterPosition();
        Vector2 targetInput = Vector2.zero;
        float currentSpeed = GetCurrentMoveSpeed();

        switch (currentState)
        {
            case BerserkerState.Idle:
                targetInput = Vector2.zero;
                break;

            case BerserkerState.Patrol:
                targetInput = (patrolTarget - myPos).normalized;
                currentSpeed = patrolSpeed;
                break;

            case BerserkerState.Chase:
                if (IsPlayerInAttackZone())
                {
                    targetInput = Vector2.zero;
                }
                else
                {
                    targetInput = (playerPos - myPos).normalized;
                }
                break;

            case BerserkerState.ReturnToSpawn:
                targetInput = (spawnPoint - myPos).normalized;
                currentSpeed = patrolSpeed;
                break;
        }

        targetInput += CalculateSeparationForce();

        SetAnimFloat("Speed", targetInput.sqrMagnitude);

        smoothMovement = Vector2.SmoothDamp(smoothMovement, targetInput, ref velocityWorkspace, movementSmoothing);
        rb.MovePosition(rb.position + smoothMovement * currentSpeed * Time.fixedDeltaTime);
    }

    // ================================
    // CHIÊU 1: CHÉM RÌU (Axe Slash)
    // Sát thương tăng theo Rage
    // ================================
    private IEnumerator PerformSlashRoutine()
    {
        currentState = BerserkerState.Attack;
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;
        smoothMovement = Vector2.zero;
        velocityWorkspace = Vector2.zero;

        AimAtPlayer();

        SetAnimTrigger("Attack1");

        // Đợi vung rìu
        float elapsedTime = 0f;
        while (elapsedTime < slashDelay)
        {
            elapsedTime += Time.deltaTime;
            AimAtPlayer();
            yield return null;
        }

        // Lao tới khi chém (rage tăng lực lao)
        float rageLunge = slashLungeForce * currentRageMultiplier;
        if (rageLunge > 0f)
        {
            rb.linearVelocity = lastFacingDirection.normalized * rageLunge;
        }

        Vector3 basePosition = GetCenterPosition();
        Vector3 attackPoint = basePosition + (Vector3)(lastFacingDirection.normalized * attackOffset);

        ShowHitboxVisual(attackPoint, slashRadius);

        // Tính sát thương có Rage
        int finalDamage = GetRageDamage(slashDamage);

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPoint, slashRadius, playerLayer);
        bool hasHit = false;
        foreach (Collider2D col in hitColliders)
        {
            if (col.transform == playerTarget || col.transform.IsChildOf(playerTarget) || col.CompareTag("Player") || col.GetComponentInParent<PlayerMovement>() != null)
            {
                hasHit = true;
                PlayerStats stats = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
                if (stats == null && playerTarget != null) stats = playerTarget.GetComponent<PlayerStats>();
                if (stats == null) stats = FindFirstObjectByType<PlayerStats>();

                if (stats != null)
                {
                    stats.TakeDamage(finalDamage, GetCenterPosition(), 4.5f);
                    Debug.Log($"<color=red>[Orc Berserker]</color> Chém Rìu gây {finalDamage} DMG! (Rage x{currentRageMultiplier:F1})");
                }
                break;
            }
        }

        // Dự phòng khoảng cách gần
        if (!hasHit && playerTarget != null)
        {
            float distToPlayer = Vector3.Distance(attackPoint, GetPlayerCenterPosition());
            if (distToPlayer <= slashRadius + 0.25f)
            {
                PlayerStats stats = playerTarget.GetComponent<PlayerStats>() ?? FindFirstObjectByType<PlayerStats>();
                if (stats != null)
                {
                    stats.TakeDamage(finalDamage, GetCenterPosition(), 4.5f);
                }
            }
        }

        if (rageLunge > 0f)
        {
            yield return new WaitForSeconds(0.06f);
            rb.linearVelocity = Vector2.zero;
        }

        float remainingDuration = slashDuration - slashDelay - 0.06f;
        if (remainingDuration > 0f)
        {
            yield return new WaitForSeconds(remainingDuration);
        }

        HideHitboxVisual();

        // Rage cũng giảm cooldown chém
        float rageCooldown = slashCooldown / currentRageMultiplier;
        nextSlashTime = Time.time + Mathf.Max(0.3f, rageCooldown);
        isAttacking = false;

        // Khôi phục màu rage
        if (sr != null) sr.color = rageColor;

        ChangeState(BerserkerState.Chase);
    }

    // ==========================================
    // CHIÊU 2: LAO ĐÂM (Berserk Charge)
    // Lao thẳng về phía Player với tốc độ cực cao
    // ==========================================
    private IEnumerator PerformChargeRoutine()
    {
        currentState = BerserkerState.Charging;
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;
        smoothMovement = Vector2.zero;
        velocityWorkspace = Vector2.zero;

        AimAtPlayer();

        SetAnimTrigger("Attack2");

        // ===== GIAI ĐOẠN 1: LẤY ĐÀ (Windup) =====
        // Nhấp nháy đỏ cảnh báo trước khi lao tới
        string rageText = currentRageMultiplier > 1.3f ? "🔥 LAO ĐÂM CUỒNG NỘ!" : "⚡ LAO ĐÂM!";
        Color textColor = currentRageMultiplier > 1.3f ? new Color(1.0f, 0.2f, 0.1f, 1.0f) : new Color(1.0f, 0.6f, 0.1f, 1.0f);
        FloatingTextManager.SpawnWorldText(rageText, transform.position + Vector3.up * 0.5f, textColor);

        float windupTimer = 0f;
        while (windupTimer < chargeWindup)
        {
            windupTimer += Time.deltaTime;
            AimAtPlayer(); // Vẫn theo dõi hướng Player trong lúc lấy đà

            // Nhấp nháy cảnh báo
            if (sr != null)
            {
                sr.color = Color.Lerp(rageColor, new Color(1f, 0.15f, 0.15f), Mathf.PingPong(windupTimer * 8f, 1f));
            }
            yield return null;
        }

        // ===== GIAI ĐOẠN 2: LAO THẲNG (Charge) =====
        // Khóa hướng lao (không đổi hướng giữa chừng)
        Vector2 chargeDirection = lastFacingDirection.normalized;
        float chargeTimer = 0f;
        bool hasHitPlayer = false;

        if (sr != null)
        {
            sr.color = new Color(1f, 0.1f, 0.1f, 1.0f); // Đỏ rực khi lao
        }

        float rageChargeSpeed = chargeSpeed * Mathf.Lerp(1.0f, 1.3f, (currentRageMultiplier - 1.0f));

        while (chargeTimer < chargeMaxDuration && !hasHitPlayer)
        {
            chargeTimer += Time.deltaTime;

            // Di chuyển theo hướng đã khóa
            rb.MovePosition(rb.position + chargeDirection * rageChargeSpeed * Time.deltaTime);

            // Hiển thị hitbox lao đâm
            ShowHitboxVisual(GetCenterPosition(), chargeHitRadius);

            // Kiểm tra va chạm với Player trong quá trình lao
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(GetCenterPosition(), chargeHitRadius, playerLayer);
            foreach (Collider2D col in hitColliders)
            {
                if (col.CompareTag("Player") || col.GetComponent<PlayerMovement>() != null || col.GetComponentInParent<PlayerMovement>() != null)
                {
                    hasHitPlayer = true;
                    int finalDamage = GetRageDamage(chargeDamage);

                    PlayerStats stats = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
                    if (stats == null) stats = FindFirstObjectByType<PlayerStats>();

                    if (stats != null)
                    {
                        float rageKnockback = chargeKnockback * currentRageMultiplier;
                        stats.TakeDamage(finalDamage, GetCenterPosition(), rageKnockback);
                        Debug.Log($"<color=red>[Orc Berserker]</color> Lao Đâm trúng! Gây {finalDamage} DMG + Knockback {rageKnockback:F1}! (Rage x{currentRageMultiplier:F1})");
                    }

                    FloatingTextManager.SpawnWorldText($"💥 {finalDamage} DMG!", col.transform.position + Vector3.up * 0.4f, Color.red);
                    break;
                }
            }

            yield return null;
        }

        // ===== GIAI ĐOẠN 3: PHỤC HỒI SAU LAO =====
        rb.linearVelocity = Vector2.zero;
        HideHitboxVisual();

        if (!hasHitPlayer)
        {
            // Nếu lao hụt: Đứng choáng ngắn (0.5s) vì quán tính
            Debug.Log($"<color=yellow>[Orc Berserker]</color> Lao đâm hụt! Bị choáng quán tính 0.5s");
            FloatingTextManager.SpawnWorldText("💨 HỤT!", transform.position + Vector3.up * 0.4f, new Color(0.8f, 0.8f, 0.2f, 1.0f));
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }

        // Khôi phục màu rage
        if (sr != null) sr.color = rageColor;

        nextChargeTime = Time.time + chargeCooldown;
        isAttacking = false;
        ChangeState(BerserkerState.Chase);
    }

    // ============================
    // TIỆN ÍCH CHUNG
    // ============================
    private void AimAtPlayer()
    {
        if (playerTarget != null)
        {
            Vector3 myPos = GetCenterPosition();
            Vector3 playerPos = GetPlayerCenterPosition();
            Vector2 dir = (playerPos - myPos).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                lastFacingDirection = dir;
                sr.flipX = (lastFacingDirection.x < -0.01f);
            }
        }
    }

    private void ChangeState(BerserkerState newState)
    {
        currentState = newState;
        stateTimer = 0f;
    }

    private Vector3 GetRandomPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * 2.5f;
        return spawnPoint + new Vector3(randomCircle.x, randomCircle.y, 0f);
    }

    private Vector2 CalculateSeparationForce()
    {
        Vector2 separation = Vector2.zero;
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, 0.5f);

        int count = 0;
        foreach (Collider2D col in nearbyEnemies)
        {
            if (col.gameObject != gameObject && (col.GetComponent<EnemyAI>() != null || col.GetComponent<OrcWarriorAI>() != null || col.GetComponent<OrcShamanAI>() != null || col.GetComponent<OrcBerserkerAI>() != null))
            {
                Vector2 pushDir = (transform.position - col.transform.position);
                float dist = pushDir.magnitude;
                if (dist > 0.001f)
                {
                    separation += pushDir.normalized / dist;
                    count++;
                }
            }
        }

        if (count > 0) separation /= count;
        return separation * 0.5f;
    }

    private void FindPlayerTarget()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) p = GameObject.Find("DarkFantasyPlayer");
        if (p == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) p = pm.gameObject;
        }
        if (p != null) playerTarget = p.transform;
    }

    private void SetupHitboxVisualizer()
    {
        Transform child = transform.Find("OrcBerserkerHitboxVisualizer");
        GameObject lineObj = child != null ? child.gameObject : new GameObject("OrcBerserkerHitboxVisualizer");
        lineObj.transform.SetParent(transform, false);
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localRotation = Quaternion.identity;

        hitboxLineRenderer = lineObj.GetComponent<LineRenderer>();
        if (hitboxLineRenderer == null)
        {
            hitboxLineRenderer = lineObj.AddComponent<LineRenderer>();
        }

        hitboxLineRenderer.useWorldSpace = false;
        hitboxLineRenderer.startWidth = 0.04f;
        hitboxLineRenderer.endWidth = 0.04f;
        hitboxLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        hitboxLineRenderer.startColor = new Color(1f, 0.15f, 0.15f, 0.8f); // Đỏ rực cuồng nộ
        hitboxLineRenderer.endColor = new Color(1f, 0.15f, 0.15f, 0.8f);
        hitboxLineRenderer.sortingOrder = 100;
        hitboxLineRenderer.enabled = false;
    }

    private void ShowHitboxVisual(Vector3 center, float radius)
    {
        if (hitboxLineRenderer == null || !showHitbox) return;

        Vector3 localCenter = transform.InverseTransformPoint(center);
        int segments = 24;
        hitboxLineRenderer.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float rad = (i / (float)segments) * 2f * Mathf.PI;
            Vector3 point = localCenter + new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
            hitboxLineRenderer.SetPosition(i, point);
        }
        hitboxLineRenderer.enabled = true;
    }

    private void HideHitboxVisual()
    {
        if (hitboxLineRenderer != null)
        {
            hitboxLineRenderer.enabled = false;
        }
    }

    private void CacheAnimatorParameters()
    {
        animParamsCache = new HashSet<string>();
        if (anim != null)
        {
            foreach (AnimatorControllerParameter param in anim.parameters)
            {
                animParamsCache.Add(param.name);
            }
        }
    }

    private void SetAnimFloat(string paramName, float value)
    {
        if (animParamsCache != null && animParamsCache.Contains(paramName))
        {
            anim.SetFloat(paramName, value);
        }
    }

    private void SetAnimTrigger(string paramName)
    {
        if (animParamsCache != null && animParamsCache.Contains(paramName))
        {
            anim.SetTrigger(paramName);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showHitbox) return;

        Vector3 basePosition = GetCenterPosition();

        // Bán kính phát hiện (đỏ nhạt)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
        Gizmos.DrawWireSphere(basePosition, detectionRadius);

        // Bán kính bỏ đuổi (xám)
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.15f);
        Gizmos.DrawWireSphere(basePosition, loseSightRadius);

        Vector2 direction = Application.isPlaying ? lastFacingDirection : new Vector2(1f, 0f);
        Vector3 attackPoint = basePosition + (Vector3)(direction.normalized * attackOffset);

        // Hitbox chém rìu (vàng/đỏ)
        Gizmos.color = isAttacking ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(attackPoint, slashRadius);

        // Phạm vi lao đâm tối thiểu (cam nhạt)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawWireSphere(basePosition, chargeMinDistance);

        // Phạm vi lao đâm tối đa (cam đậm)
        Gizmos.color = new Color(1f, 0.35f, 0f, 0.1f);
        Gizmos.DrawWireSphere(basePosition, chargeMaxDistance);

        // Hitbox va chạm lao đâm (đỏ)
        if (currentState == BerserkerState.Charging)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(basePosition, chargeHitRadius);
        }

        // Bán kính tầm đánh cận chiến thực tế (đỏ nhạt)
        Gizmos.color = new Color(1f, 0f, 0f, 0.22f);
        Gizmos.DrawWireSphere(basePosition, attackRange);
    }

    public void ResetState()
    {
        currentState = BerserkerState.Idle;
        stateTimer = 0f;
        isAttacking = false;
        nextSlashTime = 0f;
        nextChargeTime = 0f;
        lastFacingDirection = new Vector2(1f, 0f);
        smoothMovement = Vector2.zero;
        velocityWorkspace = Vector2.zero;
        patrolTarget = GetRandomPatrolPoint();
        currentRageMultiplier = 1.0f;
    }
}
