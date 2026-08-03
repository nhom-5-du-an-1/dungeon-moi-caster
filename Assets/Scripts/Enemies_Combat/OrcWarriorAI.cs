using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QUÁI QUỶ 1 - ORC CHIẾN BINH (Orc Warrior)
/// Loại quái cận chiến thuần chất, trang bị rìu + khiên.
/// - Chém thường (Slash): Đòn cận chiến cơ bản
/// - Húc Khiên (Shield Bash): Gây choáng + knockback mạnh cho Player
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class OrcWarriorAI : MonoBehaviour
{
    public enum OrcState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        ReturnToSpawn
    }

    [Header("=== MỤC TIÊU & NHẬN DIỆN ===")]
    [Tooltip("Mục tiêu theo dõi (Nếu để trống, tự tìm Player có tag 'Player')")]
    public Transform playerTarget;
    [Tooltip("Bán kính phát hiện Player")]
    public float detectionRadius = 5.5f;
    [Tooltip("Bán kính tối đa trước khi bỏ đuổi")]
    public float loseSightRadius = 9.0f;
    [Tooltip("Khoảng cách tầm đánh")]
    public float attackRange = 0.45f;

    [Header("=== DI CHUYỂN ===")]
    [Tooltip("Tốc độ di chuyển khi đuổi theo")]
    public float moveSpeed = 2.8f;
    [Tooltip("Tốc độ đi tuần")]
    public float patrolSpeed = 1.2f;
    [Range(0f, 0.5f)] public float movementSmoothing = 0.05f;

    [Header("=== CHÉM THƯỜNG (SLASH) ===")]
    [Tooltip("Sát thương đòn chém thường")]
    public int slashDamage = 3;
    [Tooltip("Thời gian hồi chiêu chém")]
    public float slashCooldown = 1.2f;
    [Tooltip("Thời gian thực hiện đòn chém")]
    public float slashDuration = 0.45f;
    [Tooltip("Thời gian trễ trước khi gây sát thương")]
    public float slashDelay = 0.28f;
    [Tooltip("Lực lao tới khi chém")]
    public float slashLungeForce = 1.5f;
    [Tooltip("Bán kính hitbox chém")]
    public float slashRadius = 0.35f;

    [Header("=== HÚC KHIÊN (SHIELD BASH) ===")]
    [Tooltip("Sát thương húc khiên")]
    public int bashDamage = 2;
    [Tooltip("Thời gian hồi chiêu húc khiên")]
    public float bashCooldown = 5.0f;
    [Tooltip("Thời gian thực hiện húc khiên")]
    public float bashDuration = 0.6f;
    [Tooltip("Lực knockback khi húc khiên (gây choáng)")]
    public float bashKnockback = 7.0f;
    [Tooltip("Lực lao tới khi húc")]
    public float bashLungeForce = 4.0f;
    [Tooltip("Bán kính hitbox húc khiên")]
    public float bashRadius = 0.45f;
    [Tooltip("Thời gian Player bị choáng sau húc khiên (giây)")]
    public float bashStunDuration = 0.8f;

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

    // Trạng thái AI
    private OrcState currentState = OrcState.Idle;
    private Vector3 spawnPoint;
    private Vector3 patrolTarget;
    private float stateTimer = 0f;
    private bool isAttacking = false;
    private float nextSlashTime = 0f;
    private float nextBashTime = 0f;
    private Vector2 lastFacingDirection = new Vector2(1f, 0f);
    private Vector2 smoothMovement;
    private Vector2 velocityWorkspace = Vector2.zero;
    private HashSet<string> animParamsCache;
    private PlayerStats playerStatsTarget;

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
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health == null)
        {
            health = gameObject.AddComponent<EnemyHealth>();
        }

        // Quái Quỷ 1: 14 HP = 7 trái tim (Chiến binh bền bỉ hơn quái thường)
        if (health.maxHealth <= 0 || health.maxHealth == 50)
        {
            health.maxHealth = 14;
            health.currentHealth = 14;
        }
        health.useHeartDisplay = true;
        health.heartCount = 7;
        health.alwaysShowHealthBar = true;
        health.barHeightOffset = 0.08f;
        health.barWidth = 0.32f;
        health.barHeight = 0.04f;
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

    /// <summary>
    /// Kiểm tra Player có nằm trong tầm đánh thực tế không
    /// </summary>
    public bool IsPlayerInAttackZone()
    {
        if (playerTarget == null) return false;

        Vector3 myPos = GetCenterPosition();
        Vector3 playerPos = GetPlayerCenterPosition();
        float dist = Vector2.Distance(myPos, playerPos);

        return dist <= attackRange;
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
            if (currentState == OrcState.Chase || currentState == OrcState.Attack)
            {
                ChangeState(OrcState.Patrol);
                isAttacking = false;
            }
        }

        if (isAttacking) return;

        Vector3 myPos = GetCenterPosition();
        Vector3 playerPos = GetPlayerCenterPosition();
        float distanceToPlayer = Vector2.Distance(myPos, playerPos);

        switch (currentState)
        {
            case OrcState.Idle:
                stateTimer += Time.deltaTime;
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(OrcState.Chase);
                }
                else if (stateTimer >= 2.5f)
                {
                    patrolTarget = GetRandomPatrolPoint();
                    ChangeState(OrcState.Patrol);
                }
                break;

            case OrcState.Patrol:
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(OrcState.Chase);
                }
                else if (Vector2.Distance(myPos, patrolTarget) < 0.2f)
                {
                    ChangeState(OrcState.Idle);
                }
                break;

            case OrcState.Chase:
                if (IsPlayerInAttackZone())
                {
                    // Ưu tiên Húc Khiên khi sẵn sàng (chiêu mạnh hơn)
                    if (Time.time >= nextBashTime)
                    {
                        StartCoroutine(PerformShieldBashRoutine());
                    }
                    else if (Time.time >= nextSlashTime)
                    {
                        StartCoroutine(PerformSlashRoutine());
                    }
                }
                else if (distanceToPlayer > loseSightRadius)
                {
                    ChangeState(OrcState.ReturnToSpawn);
                }
                break;

            case OrcState.ReturnToSpawn:
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(OrcState.Chase);
                }
                else if (Vector2.Distance(myPos, spawnPoint) < 0.3f)
                {
                    ChangeState(OrcState.Idle);
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
        float currentSpeed = moveSpeed;

        switch (currentState)
        {
            case OrcState.Idle:
                targetInput = Vector2.zero;
                break;

            case OrcState.Patrol:
                targetInput = (patrolTarget - myPos).normalized;
                currentSpeed = patrolSpeed;
                break;

            case OrcState.Chase:
                if (IsPlayerInAttackZone())
                {
                    targetInput = Vector2.zero;
                }
                else
                {
                    targetInput = (playerPos - myPos).normalized;
                }
                break;

            case OrcState.ReturnToSpawn:
                targetInput = (spawnPoint - myPos).normalized;
                currentSpeed = patrolSpeed;
                break;
        }

        targetInput += CalculateSeparationForce();

        SetAnimFloat("Speed", targetInput.sqrMagnitude);

        smoothMovement = Vector2.SmoothDamp(smoothMovement, targetInput, ref velocityWorkspace, movementSmoothing);
        rb.MovePosition(rb.position + smoothMovement * currentSpeed * Time.fixedDeltaTime);
    }

    // ============================
    // CHIÊU 1: CHÉM THƯỜNG (Slash)
    // ============================
    private IEnumerator PerformSlashRoutine()
    {
        currentState = OrcState.Attack;
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

        // Lao nhẹ về phía trước khi chém
        if (slashLungeForce > 0f)
        {
            rb.linearVelocity = lastFacingDirection.normalized * slashLungeForce;
        }

        Vector3 basePosition = GetCenterPosition();
        Vector3 attackPoint = basePosition + (Vector3)(lastFacingDirection.normalized * attackOffset);

        ShowHitboxVisual(attackPoint, slashRadius);

        // Quét sát thương chém
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
                    stats.TakeDamage(slashDamage, GetCenterPosition(), 4.0f);
                    Debug.Log($"<color=orange>[Orc Warrior]</color> Chém thường trúng Player gây {slashDamage} sát thương!");
                }
                break;
            }
        }

        // Dự phòng: Kiểm tra khoảng cách gần
        if (!hasHit && playerTarget != null)
        {
            float distToPlayer = Vector3.Distance(attackPoint, GetPlayerCenterPosition());
            if (distToPlayer <= slashRadius + 0.25f)
            {
                PlayerStats stats = playerTarget.GetComponent<PlayerStats>() ?? FindFirstObjectByType<PlayerStats>();
                if (stats != null)
                {
                    stats.TakeDamage(slashDamage, GetCenterPosition(), 4.0f);
                    Debug.Log($"<color=orange>[Orc Warrior Fallback]</color> Chém trúng Player (Khoảng cách: {distToPlayer:F2})");
                }
            }
        }

        if (slashLungeForce > 0f)
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

        nextSlashTime = Time.time + slashCooldown;
        isAttacking = false;
        ChangeState(OrcState.Chase);
    }

    // =======================================
    // CHIÊU 2: HÚC KHIÊN (Shield Bash)
    // Gây choáng + knockback mạnh cho Player
    // =======================================
    private IEnumerator PerformShieldBashRoutine()
    {
        currentState = OrcState.Attack;
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;
        smoothMovement = Vector2.zero;
        velocityWorkspace = Vector2.zero;

        AimAtPlayer();

        SetAnimTrigger("Attack2");
        FloatingTextManager.SpawnWorldText("🛡️ HÚC KHIÊN!", transform.position + Vector3.up * 0.5f, new Color(0.9f, 0.7f, 0.2f, 1.0f));

        // Nhấp nháy cam cảnh báo trước khi lao tới
        sr.color = new Color(1.0f, 0.75f, 0.3f, 1.0f);
        yield return new WaitForSeconds(0.25f);
        sr.color = Color.white;

        // Lao mạnh về phía Player
        AimAtPlayer();
        rb.linearVelocity = lastFacingDirection.normalized * bashLungeForce;

        yield return new WaitForSeconds(0.15f);

        // Hit check húc khiên
        Vector3 basePosition = GetCenterPosition();
        Vector3 attackPoint = basePosition + (Vector3)(lastFacingDirection.normalized * attackOffset);

        ShowHitboxVisual(attackPoint, bashRadius);

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPoint, bashRadius, playerLayer);
        foreach (Collider2D col in hitColliders)
        {
            if (col.CompareTag("Player") || col.GetComponent<PlayerMovement>() != null || col.GetComponentInParent<PlayerMovement>() != null)
            {
                PlayerStats stats = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
                if (stats == null) stats = FindFirstObjectByType<PlayerStats>();

                if (stats != null)
                {
                    // Gây sát thương + knockback cực mạnh
                    stats.TakeDamage(bashDamage, GetCenterPosition(), bashKnockback);
                    Debug.Log($"<color=orange>[Orc Warrior]</color> Húc Khiên trúng Player gây {bashDamage} DMG + Choáng {bashStunDuration}s!");

                    // Hiệu ứng choáng bằng làm chậm Player
                    PlayerMovement pm = col.GetComponent<PlayerMovement>() ?? col.GetComponentInParent<PlayerMovement>();
                    if (pm != null)
                    {
                        StartCoroutine(StunPlayerRoutine(pm, stats));
                    }
                }
                break;
            }
        }

        rb.linearVelocity = Vector2.zero;

        float remaining = bashDuration - 0.4f;
        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
        }

        HideHitboxVisual();

        nextBashTime = Time.time + bashCooldown;
        isAttacking = false;
        ChangeState(OrcState.Chase);
    }

    /// <summary>
    /// Gây hiệu ứng choáng (Stun) cho Player sau khi bị húc khiên
    /// Giảm tốc độ xuống 30% trong thời gian bashStunDuration
    /// </summary>
    private IEnumerator StunPlayerRoutine(PlayerMovement pm, PlayerStats stats)
    {
        if (pm == null || stats == null || stats.isDead) yield break;

        float originalSpeed = pm.moveSpeed;
        pm.moveSpeed = originalSpeed * 0.3f; // Giảm 70% tốc độ

        SpriteRenderer playerSr = pm.GetComponent<SpriteRenderer>();
        Color originalColor = playerSr != null ? playerSr.color : Color.white;

        FloatingTextManager.SpawnWorldText("💫 CHOÁNG!", stats.transform.position + Vector3.up * 0.4f, new Color(1.0f, 1.0f, 0.3f, 1.0f));

        // Nhấp nháy vàng thể hiện choáng
        float timer = 0f;
        while (timer < bashStunDuration)
        {
            timer += Time.deltaTime;
            if (playerSr != null && !stats.isDead)
            {
                playerSr.color = Color.Lerp(new Color(1.0f, 1.0f, 0.4f), originalColor, Mathf.PingPong(timer * 6f, 1f));
            }
            yield return null;
        }

        // Khôi phục trạng thái
        if (pm != null) pm.moveSpeed = originalSpeed;
        if (playerSr != null && stats != null && !stats.isDead) playerSr.color = originalColor;
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

    private void ChangeState(OrcState newState)
    {
        currentState = newState;
        stateTimer = 0f;
    }

    private Vector3 GetRandomPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * 2.0f;
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
        Transform child = transform.Find("OrcWarriorHitboxVisualizer");
        GameObject lineObj = child != null ? child.gameObject : new GameObject("OrcWarriorHitboxVisualizer");
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
        hitboxLineRenderer.startColor = new Color(0.9f, 0.7f, 0.2f, 0.8f); // Màu vàng chiến binh
        hitboxLineRenderer.endColor = new Color(0.9f, 0.7f, 0.2f, 0.8f);
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

        // Bán kính phát hiện (xanh dương nhạt)
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.2f);
        Gizmos.DrawWireSphere(basePosition, detectionRadius);

        // Bán kính bỏ đuổi (xám)
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.15f);
        Gizmos.DrawWireSphere(basePosition, loseSightRadius);

        Vector2 direction = Application.isPlaying ? lastFacingDirection : new Vector2(1f, 0f);
        Vector3 attackPoint = basePosition + (Vector3)(direction.normalized * attackOffset);

        // Hitbox chém (vàng)
        Gizmos.color = isAttacking ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(attackPoint, slashRadius);

        // Hitbox húc khiên (cam)
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.4f);
        Gizmos.DrawWireSphere(attackPoint, bashRadius);

        // Bán kính tầm đánh cận chiến thực tế (đỏ nhạt)
        Gizmos.color = new Color(1f, 0f, 0f, 0.22f);
        Gizmos.DrawWireSphere(basePosition, attackRange);
    }

    public void ResetState()
    {
        currentState = OrcState.Idle;
        stateTimer = 0f;
        isAttacking = false;
        nextSlashTime = 0f;
        nextBashTime = 0f;
        lastFacingDirection = new Vector2(1f, 0f);
        smoothMovement = Vector2.zero;
        velocityWorkspace = Vector2.zero;
        patrolTarget = GetRandomPatrolPoint();
    }
}
