using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class BossOrcAI : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        EnragedRoar,
        ReturnToSpawn
    }

    [Header("=== MỤC TIÊU & NHẬN DIỆN ===")]
    public Transform playerTarget;
    public float detectionRadius = 8.0f;
    public float loseSightRadius = 12.0f;

    [Header("=== DI CHUYỂN ===")]
    public float moveSpeed = 2.0f;
    public float patrolSpeed = 1.0f;
    [Range(0f, 0.5f)] public float movementSmoothing = 0.05f;

    [Header("=== THÔNG SỐ CHIẾN ĐẤU ===")]
    public int baseDamage = 3; // Sát thương đòn đánh thường
    public float attackCooldown = 1.5f;

    [Header("=== CÁC CHIÊU THỨC CỦA BOSS ===")]
    [Tooltip("Khoảng cách kích hoạt đòn chém thường")]
    public float slashRange = 1.2f;
    [Tooltip("Khoảng cách kích hoạt vòng chém lốc xoáy (Cyclone Spin)")]
    public float spinRange = 1.8f;
    [Tooltip("Khoảng cách kích hoạt nện đất (Earthquake Slam)")]
    public float slamRange = 4.0f;

    [Header("=== HỒI CHIÊU ĐẶC BIỆT ===")]
    public float spinCooldown = 6.0f;
    public float slamCooldown = 8.0f;

    [Header("=== THỜI GIAN ĐÒN ĐÁNH ===")]
    public float slashDuration = 0.5f;
    public float spinDuration = 1.0f;
    public float slamDuration = 1.2f;

    [Header("=== LỰC LAO & DI CHUYỂN CHIÊU ===")]
    public float slamLungeForce = 6.0f; // Tốc độ lao tới khi nện đất
    public float slashLungeForce = 2.0f; // Nhích nhẹ khi chém

    [Header("=== PHẠM VI SÁT THƯƠNG ĐẶC BIỆT ===")]
    public float slashRadius = 0.8f;
    public float spinRadius = 1.4f;
    public float slamRadius = 1.8f;

    [Header("=== PHÂN ĐOẠN PHÒNG THỦ ===")]
    [Tooltip("Tỷ lệ máu để kích hoạt Nổi điên (Enraged State - mặc định 50%)")]
    public float enragedHealthThreshold = 0.5f;
    public float enragedSpeedMultiplier = 1.4f;
    public float enragedCooldownReduction = 0.5f; // Giảm 50% thời gian hồi chiêu

    // Lớp Layer chứa Player
    public LayerMask playerLayer;
    public bool showHitbox = true;

    // Thành phần Unity
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private LineRenderer hitboxLineRenderer;

    // Trạng thái Boss
    private BossState currentState = BossState.Idle;
    private Vector3 spawnPoint;
    private Vector3 patrolTarget;
    private float stateTimer = 0f;
    private bool isAttacking = false;
    private bool isEnraged = false;
    private float nextSlashTime = 0f;
    private float nextSpinTime = 0f;
    private float nextSlamTime = 0f;

    private Vector2 lastFacingDirection = new Vector2(1f, 0f);
    private Vector2 smoothMovement;
    private Vector2 velocityWorkspace = Vector2.zero;
    private HashSet<string> animParamsCache;
    private PlayerStats playerStatsTarget;
    private EnemyHealth healthComponent;

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
        EnsureBossHealthComponent();
        FindPlayerTarget();
    }

    private void EnsureBossHealthComponent()
    {
        healthComponent = GetComponent<EnemyHealth>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<EnemyHealth>();
        }

        // Cấu hình máu Boss khổng lồ: 100 HP = 50 Trái tim
        healthComponent.maxHealth = 100;
        healthComponent.currentHealth = 100;

        // Boss dùng thanh máu lớn thay vì hiển thị các trái tim nhỏ
        healthComponent.useHeartDisplay = false;
        healthComponent.alwaysShowHealthBar = true;
        healthComponent.barWidth = 0.6f; // Thanh máu dài hơn
        healthComponent.barHeight = 0.06f; // Thanh máu dày hơn
        healthComponent.barHeightOffset = 0.15f; // Đẩy thanh máu lên cao hơn đầu Boss Orc
        healthComponent.UpdateHealthBarUI(true);
    }

    public Vector3 GetCenterPosition()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            return transform.position + (Vector3)col.offset;
        }
        return transform.position;
    }

    public Vector3 GetPlayerCenterPosition()
    {
        if (playerTarget == null) return transform.position;

        PlayerMovement pm = playerTarget.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            return pm.GetCenterPosition();
        }
        return playerTarget.position;
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

        // Khi Player đã chết, quái lập tức bỏ chiến đấu, quay về tuần tra
        if (playerStatsTarget != null && playerStatsTarget.isDead)
        {
            if (currentState == BossState.Chase || currentState == BossState.Attack)
            {
                ChangeState(BossState.Patrol);
                isAttacking = false;
            }
        }

        // Kiểm tra Nổi điên khi máu xuống dưới 50%
        if (!isEnraged && healthComponent != null && (float)healthComponent.currentHealth / healthComponent.maxHealth <= enragedHealthThreshold)
        {
            StartCoroutine(TriggerEnragedPhaseRoutine());
        }

        if (isAttacking || currentState == BossState.EnragedRoar) return;

        Vector3 myPos = GetCenterPosition();
        Vector3 playerPos = GetPlayerCenterPosition();
        float distanceToPlayer = Vector2.Distance(myPos, playerPos);

        // Hướng nhìn về phía Player
        if (distanceToPlayer <= detectionRadius)
        {
            Vector2 dirToPlayer = (playerPos - myPos).normalized;
            if (dirToPlayer.sqrMagnitude > 0.001f)
            {
                lastFacingDirection = dirToPlayer;
                sr.flipX = (lastFacingDirection.x < -0.01f);
            }
        }
        else if (smoothMovement.sqrMagnitude > 0.001f)
        {
            lastFacingDirection = smoothMovement.normalized;
            sr.flipX = (lastFacingDirection.x < -0.01f);
        }

        switch (currentState)
        {
            case BossState.Idle:
                stateTimer += Time.deltaTime;
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(BossState.Chase);
                }
                else if (stateTimer >= 2.5f)
                {
                    patrolTarget = GetRandomPatrolPoint();
                    ChangeState(BossState.Patrol);
                }
                break;

            case BossState.Patrol:
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(BossState.Chase);
                }
                else if (Vector2.Distance(myPos, patrolTarget) < 0.3f)
                {
                    ChangeState(BossState.Idle);
                }
                break;

            case BossState.Chase:
                if (distanceToPlayer > loseSightRadius)
                {
                    ChangeState(BossState.ReturnToSpawn);
                    break;
                }

                // AI QUYẾT ĐỊNH CHIÊU THỨC TẤN CÔNG
                // 1. Nện đất (Earthquake Slam): Khi ở xa, lao tới nện xuống
                if (distanceToPlayer <= slamRange && distanceToPlayer > spinRange && Time.time >= nextSlamTime)
                {
                    StartCoroutine(PerformSlamRoutine());
                }
                // 2. Chém xoay lốc (Cyclone Spin): Khi Player ở cự ly gần trung bình
                else if (distanceToPlayer <= spinRange && distanceToPlayer > slashRange && Time.time >= nextSpinTime)
                {
                    StartCoroutine(PerformSpinRoutine());
                }
                // 3. Chém thường (Slash): Áp sát cự ly cực gần
                else if (distanceToPlayer <= slashRange && Time.time >= nextSlashTime)
                {
                    StartCoroutine(PerformSlashRoutine());
                }
                break;

            case BossState.ReturnToSpawn:
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(BossState.Chase);
                }
                else if (Vector2.Distance(myPos, spawnPoint) < 0.3f)
                {
                    ChangeState(BossState.Idle);
                }
                break;
        }
    }

    void LateUpdate()
    {
        if (sr != null)
        {
            sr.sortingOrder = 10000 - Mathf.RoundToInt(transform.position.y * 100);
        }
    }

    void FixedUpdate()
    {
        if (isAttacking || currentState == BossState.EnragedRoar)
        {
            SetAnimFloat("Speed", 0f);
            return;
        }

        Vector3 myPos = GetCenterPosition();
        Vector3 playerPos = GetPlayerCenterPosition();
        Vector2 targetInput = Vector2.zero;
        float currentSpeed = moveSpeed;

        if (isEnraged)
        {
            currentSpeed *= enragedSpeedMultiplier;
        }

        switch (currentState)
        {
            case BossState.Idle:
                targetInput = Vector2.zero;
                break;

            case BossState.Patrol:
                targetInput = (patrolTarget - myPos).normalized;
                currentSpeed = patrolSpeed;
                break;

            case BossState.Chase:
                // Nếu ở cự ly chém quá sát, Boss sẽ đứng lại chém chứ không đi xuyên qua Player
                if (Vector2.Distance(myPos, playerPos) <= slashRange - 0.2f)
                {
                    targetInput = Vector2.zero;
                }
                else
                {
                    targetInput = (playerPos - myPos).normalized;
                }
                break;

            case BossState.ReturnToSpawn:
                targetInput = (spawnPoint - myPos).normalized;
                currentSpeed = patrolSpeed;
                break;
        }

        // Tác dụng lực đẩy ngăn chồng đè quái vật khác
        targetInput += CalculateSeparationForce();

        SetAnimFloat("Speed", targetInput.sqrMagnitude);

        smoothMovement = Vector2.SmoothDamp(smoothMovement, targetInput, ref velocityWorkspace, movementSmoothing);
        rb.MovePosition(rb.position + smoothMovement * currentSpeed * Time.fixedDeltaTime);
    }

    // CHIÊU 1: CHÉM THƯỜNG (Slash)
    private IEnumerator PerformSlashRoutine()
    {
        currentState = BossState.Attack;
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;
        smoothMovement = Vector2.zero;

        AimAtPlayer();

        // Kích hoạt hoạt ảnh chém thường (Attack1)
        SetAnimTrigger("Attack");
        SetAnimTrigger("Attack1");

        // Đợi vung tay chém
        float delay = 0.2f;
        yield return new WaitForSeconds(delay);

        // Lao nhẹ lên chém dứt khoát
        if (slashLungeForce > 0f)
        {
            rb.linearVelocity = lastFacingDirection.normalized * slashLungeForce;
        }

        Vector3 basePosition = GetCenterPosition();
        Vector3 attackPoint = basePosition + (Vector3)(lastFacingDirection.normalized * 0.4f);

        ShowHitboxVisual(attackPoint, slashRadius, true);

        // Quét sát thương
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint, slashRadius, playerLayer);
        foreach (Collider2D col in hits)
        {
            if (col.CompareTag("Player") || col.GetComponent<PlayerMovement>() != null)
            {
                PlayerStats stats = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
                if (stats != null)
                {
                    int damage = isEnraged ? Mathf.RoundToInt(baseDamage * 1.5f) : baseDamage;
                    stats.TakeDamage(damage, GetCenterPosition(), 3.0f);
                    Debug.Log($"[Boss Orc] Chém thường gây {damage} dmg!");
                }
                break;
            }
        }

        yield return new WaitForSeconds(0.06f);
        rb.linearVelocity = Vector2.zero;

        float remaining = slashDuration - delay - 0.06f;
        if (remaining > 0f) yield return new WaitForSeconds(remaining);

        HideHitboxVisual();

        float cd = isEnraged ? attackCooldown * enragedCooldownReduction : attackCooldown;
        nextSlashTime = Time.time + cd;
        isAttacking = false;
        ChangeState(BossState.Chase);
    }

    // CHIÊU 2: XOAY LỐC XOÁY (Cyclone Spin)
    private IEnumerator PerformSpinRoutine()
    {
        currentState = BossState.Attack;
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;
        smoothMovement = Vector2.zero;

        SetAnimTrigger("Attack2"); // Kích hoạt hoạt ảnh xoay rìu vòng tròn
        FloatingTextManager.SpawnWorldText("🌪️ XOAY LỐC XOÁY!", transform.position + Vector3.up * 0.6f, new Color(1.0f, 0.4f, 0.0f, 1.0f));

        // Nhấp nháy quái xoay vòng
        float duration = spinDuration;
        float elapsed = 0f;
        float damageTickRate = 0.25f;
        float nextDamageTick = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Xoay nhẹ theo thời gian
            sr.flipX = !sr.flipX;

            ShowHitboxVisual(GetCenterPosition(), spinRadius, false);

            if (elapsed >= nextDamageTick)
            {
                nextDamageTick = elapsed + damageTickRate;

                // Gây sát thương 360 độ xung quanh
                Collider2D[] hits = Physics2D.OverlapCircleAll(GetCenterPosition(), spinRadius, playerLayer);
                foreach (Collider2D col in hits)
                {
                    if (col.CompareTag("Player") || col.GetComponent<PlayerMovement>() != null)
                    {
                        PlayerStats stats = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
                        if (stats != null)
                        {
                            int damage = isEnraged ? Mathf.RoundToInt(baseDamage * 1.2f) : baseDamage;
                            stats.TakeDamage(damage, GetCenterPosition(), 2.0f);
                            Debug.Log($"[Boss Orc] Chém xoay trúng gây {damage} dmg!");
                        }
                    }
                }
            }
            yield return null;
        }

        HideHitboxVisual();

        float cd = isEnraged ? spinCooldown * enragedCooldownReduction : spinCooldown;
        nextSpinTime = Time.time + cd;
        isAttacking = false;
        ChangeState(BossState.Chase);
    }

    // CHIÊU 3: JUMP SLAM (Earthquake Slam)
    private IEnumerator PerformSlamRoutine()
    {
        currentState = BossState.Attack;
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;
        smoothMovement = Vector2.zero;

        AimAtPlayer();

        SetAnimTrigger("Attack3"); // Kích hoạt nhảy nện đất
        FloatingTextManager.SpawnWorldText("🌋 ĐỊA CHẤN!", transform.position + Vector3.up * 0.6f, new Color(1.0f, 0.1f, 0.1f, 1.0f));

        // 1. Giai đoạn lấy đà nhảy (Roar/Charging)
        yield return new WaitForSeconds(0.3f);

        // 2. Phi thân lao tới điểm của Player
        Vector3 jumpTarget = GetPlayerCenterPosition();
        Vector2 jumpDir = (jumpTarget - GetCenterPosition()).normalized;
        rb.linearVelocity = jumpDir * slamLungeForce;

        // Lao đi trong 0.4s
        yield return new WaitForSeconds(0.4f);
        rb.linearVelocity = Vector2.zero;

        // 3. 💥 NỆN ĐẤT TẠO RUNG CHẤN VÀ GÂY SÁT THƯƠNG RỘNG (AOE)
        Vector3 impactPoint = GetCenterPosition();
        ShowHitboxVisual(impactPoint, slamRadius, false);

        // Tạo chữ dung nham
        FloatingTextManager.SpawnWorldText("💥 RUNG CHẤN!", impactPoint + Vector3.up * 0.4f, Color.red);

        Collider2D[] hits = Physics2D.OverlapCircleAll(impactPoint, slamRadius, playerLayer);
        foreach (Collider2D col in hits)
        {
            if (col.CompareTag("Player") || col.GetComponent<PlayerMovement>() != null)
            {
                PlayerStats stats = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
                if (stats != null)
                {
                    // Chiêu nện đất gây sát thương gấp đôi và knockback cực mạnh
                    int damage = isEnraged ? baseDamage * 3 : baseDamage * 2;
                    stats.TakeDamage(damage, impactPoint, 8.0f); // Bị đánh bật xa
                    Debug.Log($"[Boss Orc] Nện đất chí mạng gây {damage} dmg!");
                }
            }
        }

        // Đợi phục hồi sau nện đất
        yield return new WaitForSeconds(0.5f);
        HideHitboxVisual();

        float cd = isEnraged ? slamCooldown * enragedCooldownReduction : slamCooldown;
        nextSlamTime = Time.time + cd;
        isAttacking = false;
        ChangeState(BossState.Chase);
    }

    // NỔI ĐIÊN (Enraged Phase)
    private IEnumerator TriggerEnragedPhaseRoutine()
    {
        isEnraged = true;
        currentState = BossState.EnragedRoar;
        isAttacking = true; // Khóa các hành động khác

        rb.linearVelocity = Vector2.zero;
        smoothMovement = Vector2.zero;

        // Hiển thị chữ to trên đầu quái
        FloatingTextManager.SpawnWorldText("🔥 ORC BOSS NỔI ĐIÊN! 🔥", transform.position + Vector3.up * 0.7f, new Color(1.0f, 0.15f, 0.15f, 1.0f));

        // Nhấp nháy đỏ rực rỡ báo hiệu nộ khí
        for (int i = 0; i < 6; i++)
        {
            sr.color = new Color(1f, 0.2f, 0.2f);
            yield return new WaitForSeconds(0.12f);
            sr.color = Color.white;
            yield return new WaitForSeconds(0.12f);
        }

        // Khóa màu da đỏ sẫm biểu thị trạng thái Rage vĩnh viễn
        sr.color = new Color(1.0f, 0.65f, 0.65f, 1.0f);

        isAttacking = false;
        ChangeState(BossState.Chase);
    }

    private void AimAtPlayer()
    {
        if (playerTarget != null)
        {
            Vector3 currentBasePos = GetCenterPosition();
            Vector3 currentPlayerPos = GetPlayerCenterPosition();
            Vector2 currentDir = (currentPlayerPos - currentBasePos).normalized;
            if (currentDir.sqrMagnitude > 0.001f)
            {
                lastFacingDirection = currentDir;
                sr.flipX = (lastFacingDirection.x < -0.01f);
            }
        }
    }

    private void ChangeState(BossState newState)
    {
        currentState = newState;
        stateTimer = 0f;
    }

    private Vector3 GetRandomPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * 3.5f;
        return spawnPoint + new Vector3(randomCircle.x, randomCircle.y, 0f);
    }

    private Vector2 CalculateSeparationForce()
    {
        Vector2 separation = Vector2.zero;
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, 0.7f);

        int count = 0;
        foreach (Collider2D col in nearbyEnemies)
        {
            if (col.gameObject != gameObject && (col.GetComponent<EnemyAI>() != null || col.GetComponent<BossOrcAI>() != null))
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

        if (count > 0)
        {
            separation /= count;
        }

        return separation * 0.4f;
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

        if (p != null)
        {
            playerTarget = p.transform;
        }
    }

    private void SetupHitboxVisualizer()
    {
        Transform child = transform.Find("BossHitboxVisualizer");
        GameObject lineObj = child != null ? child.gameObject : new GameObject("BossHitboxVisualizer");
        lineObj.transform.SetParent(transform, false);
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localRotation = Quaternion.identity;

        hitboxLineRenderer = lineObj.GetComponent<LineRenderer>();
        if (hitboxLineRenderer == null)
        {
            hitboxLineRenderer = lineObj.AddComponent<LineRenderer>();
        }

        hitboxLineRenderer.useWorldSpace = false;
        hitboxLineRenderer.startWidth = 0.05f;
        hitboxLineRenderer.endWidth = 0.05f;
        hitboxLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        hitboxLineRenderer.startColor = new Color(1f, 0.3f, 0f, 0.8f); // Màu cam lửa
        hitboxLineRenderer.endColor = new Color(1f, 0.3f, 0f, 0.8f);
        hitboxLineRenderer.sortingOrder = 101;
        hitboxLineRenderer.enabled = false;
    }

    private void ShowHitboxVisual(Vector3 center, float radius, bool isOffsetForward)
    {
        if (hitboxLineRenderer == null || !showHitbox) return;

        Vector3 localCenter = transform.InverseTransformPoint(center);
        int segments = 32;
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

        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(basePosition, detectionRadius);

        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.15f);
        Gizmos.DrawWireSphere(basePosition, loseSightRadius);

        Vector2 direction = Application.isPlaying ? lastFacingDirection : new Vector2(1f, 0f);
        
        // Slash radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(basePosition + (Vector3)(direction.normalized * 0.4f), slashRadius);

        // Spin radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(basePosition, spinRadius);

        // Slam radius
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(basePosition + (Vector3)(direction.normalized * 0.8f), slamRadius);
    }
}
