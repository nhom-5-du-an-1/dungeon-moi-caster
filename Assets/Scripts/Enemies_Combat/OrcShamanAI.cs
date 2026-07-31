using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QUÁI QUỶ 2 - ORC PHÁP SƯ (Orc Shaman)
/// Loại quái tấn công tầm xa bằng phép thuật nguyên tố.
/// - Phóng Đạn Phép (Magic Bolt): Bắn đạn phép tầm xa gây sát thương
/// - Hồi Máu Đồng Đội (Tribal Heal): Hồi máu cho quái đồng minh xung quanh
/// - Giữ khoảng cách an toàn, lùi lại khi Player áp sát quá gần
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class OrcShamanAI : MonoBehaviour
{
    public enum ShamanState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Retreat,       // Lùi lại khi Player áp sát
        HealAllies,    // Hồi máu đồng đội
        ReturnToSpawn
    }

    [Header("=== MỤC TIÊU & NHẬN DIỆN ===")]
    [Tooltip("Mục tiêu theo dõi (Nếu để trống, tự tìm Player có tag 'Player')")]
    public Transform playerTarget;
    [Tooltip("Bán kính phát hiện Player")]
    public float detectionRadius = 7.0f;
    [Tooltip("Bán kính tối đa trước khi bỏ đuổi")]
    public float loseSightRadius = 10.0f;
    [Tooltip("Tầm bắn phép thuật tối đa")]
    public float shootRange = 5.0f;
    [Tooltip("Khoảng cách tối thiểu muốn giữ với Player (lùi lại nếu gần hơn)")]
    public float safeDistance = 2.5f;

    [Header("=== DI CHUYỂN ===")]
    [Tooltip("Tốc độ di chuyển khi đuổi theo")]
    public float moveSpeed = 2.0f;
    [Tooltip("Tốc độ đi tuần")]
    public float patrolSpeed = 0.9f;
    [Tooltip("Tốc độ lùi lại khi Player áp sát")]
    public float retreatSpeed = 2.5f;
    [Range(0f, 0.5f)] public float movementSmoothing = 0.05f;

    [Header("=== PHÓNG ĐẠN PHÉP (MAGIC BOLT) ===")]
    [Tooltip("Loại nguyên tố của đạn phép")]
    public MonsterProjectile.ElementType elementType = MonsterProjectile.ElementType.Fire;
    [Tooltip("Prefab của viên đạn phép")]
    public GameObject projectilePrefab;
    [Tooltip("Tốc độ bay của viên đạn phép")]
    public float projectileSpeed = 5.5f;
    [Tooltip("Sát thương viên đạn phép")]
    public int projectileDamage = 3;
    [Tooltip("Thời gian hồi chiêu giữa các phát bắn")]
    public float shootCooldown = 2.0f;
    [Tooltip("Thời gian thực hiện phép bắn")]
    public float shootDuration = 0.7f;
    [Tooltip("Thời gian trễ trước khi đạn bay ra")]
    public float shootDelay = 0.3f;
    [Tooltip("Số lượng đạn bắn liên tiếp mỗi lần (Burst)")]
    public int burstCount = 2;
    [Tooltip("Khoảng cách giữa mỗi viên đạn trong burst (giây)")]
    public float burstInterval = 0.2f;

    [Header("=== HỒI MÁU ĐỒNG ĐỘI (TRIBAL HEAL) ===")]
    [Tooltip("Bán kính hồi máu đồng đội xung quanh")]
    public float healRadius = 3.5f;
    [Tooltip("Lượng máu hồi cho mỗi đồng đội")]
    public int healAmount = 4;
    [Tooltip("Thời gian hồi chiêu hồi máu")]
    public float healCooldown = 10.0f;
    [Tooltip("Tỷ lệ máu đồng đội để kích hoạt hồi máu (VD: 0.6 = hồi khi đồng đội dưới 60% HP)")]
    public float healThreshold = 0.6f;

    [Header("=== HITBOX & HIỂN THỊ ===")]
    [Tooltip("Độ lệch tâm hình ảnh")]
    public Vector2 spriteOffset = Vector2.zero;
    public bool showGizmos = true;

    // Thành phần Unity
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    // Trạng thái AI
    private ShamanState currentState = ShamanState.Idle;
    private Vector3 spawnPoint;
    private Vector3 patrolTarget;
    private float stateTimer = 0f;
    private bool isAttacking = false;
    private float nextShootTime = 0f;
    private float nextHealTime = 0f;
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

        // Quái Quỷ 2: 10 HP = 5 trái tim (Pháp sư mỏng máu hơn chiến binh)
        if (health.maxHealth <= 0 || health.maxHealth == 50)
        {
            health.maxHealth = 10;
            health.currentHealth = 10;
        }
        health.useHeartDisplay = true;
        health.heartCount = 5;
        health.alwaysShowHealthBar = true;
        health.barHeightOffset = 0.08f;
        health.barWidth = 0.28f;
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
        return playerTarget.position;
    }

    public bool IsPlayerInShootRange()
    {
        if (playerTarget == null) return false;
        float dist = Vector2.Distance(GetCenterPosition(), GetPlayerCenterPosition());
        return dist <= shootRange;
    }

    /// <summary>
    /// Kiểm tra xem có đồng đội nào cần hồi máu không
    /// </summary>
    private bool ShouldHealAllies()
    {
        if (Time.time < nextHealTime) return false;

        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, healRadius);
        foreach (Collider2D col in nearbyColliders)
        {
            if (col.gameObject == gameObject) continue;

            EnemyHealth allyHealth = col.GetComponent<EnemyHealth>();
            if (allyHealth != null && allyHealth.currentHealth > 0)
            {
                float hpRatio = (float)allyHealth.currentHealth / allyHealth.maxHealth;
                if (hpRatio <= healThreshold && hpRatio > 0f)
                {
                    return true;
                }
            }
        }
        return false;
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
            if (currentState == ShamanState.Chase || currentState == ShamanState.Attack || currentState == ShamanState.Retreat)
            {
                ChangeState(ShamanState.Patrol);
                isAttacking = false;
            }
        }

        if (isAttacking) return;

        Vector3 myPos = GetCenterPosition();
        Vector3 playerPos = GetPlayerCenterPosition();
        float distanceToPlayer = Vector2.Distance(myPos, playerPos);

        switch (currentState)
        {
            case ShamanState.Idle:
                stateTimer += Time.deltaTime;
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(ShamanState.Chase);
                }
                else if (stateTimer >= 2.5f)
                {
                    patrolTarget = GetRandomPatrolPoint();
                    ChangeState(ShamanState.Patrol);
                }
                break;

            case ShamanState.Patrol:
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(ShamanState.Chase);
                }
                else if (Vector2.Distance(myPos, patrolTarget) < 0.2f)
                {
                    ChangeState(ShamanState.Idle);
                }
                break;

            case ShamanState.Chase:
                // Ưu tiên 1: Hồi máu đồng đội nếu cần
                if (ShouldHealAllies())
                {
                    StartCoroutine(PerformHealRoutine());
                    break;
                }

                // Ưu tiên 2: Lùi lại nếu Player quá gần
                if (distanceToPlayer < safeDistance)
                {
                    ChangeState(ShamanState.Retreat);
                    break;
                }

                // Ưu tiên 3: Bắn đạn phép nếu trong tầm
                if (IsPlayerInShootRange() && Time.time >= nextShootTime)
                {
                    StartCoroutine(PerformShootRoutine());
                }
                else if (distanceToPlayer > loseSightRadius)
                {
                    ChangeState(ShamanState.ReturnToSpawn);
                }
                break;

            case ShamanState.Retreat:
                // Khi đã lùi đủ xa, quay lại Chase
                if (distanceToPlayer >= safeDistance + 0.5f)
                {
                    ChangeState(ShamanState.Chase);
                }
                else if (distanceToPlayer > loseSightRadius)
                {
                    ChangeState(ShamanState.ReturnToSpawn);
                }
                break;

            case ShamanState.ReturnToSpawn:
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(ShamanState.Chase);
                }
                else if (Vector2.Distance(myPos, spawnPoint) < 0.3f)
                {
                    ChangeState(ShamanState.Idle);
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
        if (smoothMovement.sqrMagnitude > 0.001f && currentState != ShamanState.Retreat)
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
            case ShamanState.Idle:
                targetInput = Vector2.zero;
                break;

            case ShamanState.Patrol:
                targetInput = (patrolTarget - myPos).normalized;
                currentSpeed = patrolSpeed;
                break;

            case ShamanState.Chase:
                if (IsPlayerInShootRange())
                {
                    // Đứng yên chờ bắn nếu đang trong tầm bắn
                    targetInput = Vector2.zero;
                }
                else
                {
                    targetInput = (playerPos - myPos).normalized;
                }
                break;

            case ShamanState.Retreat:
                // Di chuyển LÙI LẠI xa khỏi Player
                Vector2 retreatDir = (myPos - playerPos).normalized;
                targetInput = retreatDir;
                currentSpeed = retreatSpeed;
                break;

            case ShamanState.ReturnToSpawn:
                targetInput = (spawnPoint - myPos).normalized;
                currentSpeed = patrolSpeed;
                break;
        }

        targetInput += CalculateSeparationForce();

        SetAnimFloat("Speed", targetInput.sqrMagnitude);

        smoothMovement = Vector2.SmoothDamp(smoothMovement, targetInput, ref velocityWorkspace, movementSmoothing);
        rb.MovePosition(rb.position + smoothMovement * currentSpeed * Time.fixedDeltaTime);
    }

    // ============================================
    // CHIÊU 1: PHÓNG ĐẠN PHÉP (Magic Bolt Burst)
    // Bắn nhiều viên đạn phép liên tiếp
    // ============================================
    private IEnumerator PerformShootRoutine()
    {
        currentState = ShamanState.Attack;
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;
        smoothMovement = Vector2.zero;
        velocityWorkspace = Vector2.zero;

        AimAtPlayer();

        // Kích hoạt animation tấn công
        SetAnimTrigger("Attack1");

        // Đợi đến thời điểm phóng phép
        yield return new WaitForSeconds(shootDelay);

        // Bắn burst đạn phép
        for (int i = 0; i < burstCount; i++)
        {
            if (playerTarget == null) break;

            AimAtPlayer();

            if (projectilePrefab != null)
            {
                Vector3 spawnPos = GetCenterPosition() + (Vector3)(lastFacingDirection.normalized * 0.3f);
                GameObject bulletObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                MonsterProjectile projectile = bulletObj.GetComponent<MonsterProjectile>();
                if (projectile != null)
                {
                    // Thêm chút spread nhẹ cho viên đạn thứ 2 trở đi
                    Vector2 shootDir = lastFacingDirection;
                    if (i > 0)
                    {
                        float spreadAngle = Random.Range(-12f, 12f);
                        float rad = spreadAngle * Mathf.Deg2Rad;
                        float cos = Mathf.Cos(rad);
                        float sin = Mathf.Sin(rad);
                        shootDir = new Vector2(
                            lastFacingDirection.x * cos - lastFacingDirection.y * sin,
                            lastFacingDirection.x * sin + lastFacingDirection.y * cos
                        );
                    }
                    projectile.Setup(shootDir, elementType, projectileDamage, projectileSpeed);
                }
                Debug.Log($"<color=magenta>[Orc Shaman]</color> Bắn đạn phép {elementType} viên {i + 1}/{burstCount}!");
            }

            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }

        // Chờ nốt thời gian hoàn thành
        float remainingDuration = shootDuration - shootDelay - (burstInterval * (burstCount - 1));
        if (remainingDuration > 0f)
        {
            yield return new WaitForSeconds(remainingDuration);
        }

        nextShootTime = Time.time + shootCooldown;
        isAttacking = false;
        ChangeState(ShamanState.Chase);
    }

    // ============================================
    // CHIÊU 2: HỒI MÁU ĐỒNG ĐỘI (Tribal Heal)
    // Hồi máu cho tất cả quái đồng minh xung quanh
    // ============================================
    private IEnumerator PerformHealRoutine()
    {
        currentState = ShamanState.HealAllies;
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;
        smoothMovement = Vector2.zero;
        velocityWorkspace = Vector2.zero;

        SetAnimTrigger("Attack2");
        FloatingTextManager.SpawnWorldText("💚 HỒI MÁU BỘ TỘC!", transform.position + Vector3.up * 0.5f, new Color(0.2f, 1.0f, 0.4f, 1.0f));

        // Nhấp nháy xanh lá thể hiện thi pháp hồi máu
        for (int i = 0; i < 4; i++)
        {
            sr.color = new Color(0.3f, 1.0f, 0.4f, 1.0f);
            yield return new WaitForSeconds(0.15f);
            sr.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }

        // Hồi máu cho tất cả đồng đội trong bán kính
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, healRadius);
        int healedCount = 0;

        foreach (Collider2D col in nearbyColliders)
        {
            if (col.gameObject == gameObject) continue;

            EnemyHealth allyHealth = col.GetComponent<EnemyHealth>();
            if (allyHealth != null && allyHealth.currentHealth > 0 && allyHealth.currentHealth < allyHealth.maxHealth)
            {
                allyHealth.Heal(healAmount);
                healedCount++;
                Debug.Log($"<color=green>[Orc Shaman]</color> Hồi {healAmount} HP cho {col.gameObject.name}!");
                FloatingTextManager.SpawnWorldText($"+{healAmount} HP", col.transform.position + Vector3.up * 0.3f, new Color(0.2f, 1.0f, 0.3f, 1.0f));
            }
        }

        // Tự hồi máu cho bản thân nếu cần
        EnemyHealth selfHealth = GetComponent<EnemyHealth>();
        if (selfHealth != null && selfHealth.currentHealth > 0 && selfHealth.currentHealth < selfHealth.maxHealth)
        {
            int selfHeal = Mathf.RoundToInt(healAmount * 0.5f); // Tự hồi ít hơn (50%)
            selfHealth.Heal(selfHeal);
            FloatingTextManager.SpawnWorldText($"+{selfHeal} HP", transform.position + Vector3.up * 0.3f, new Color(0.4f, 1.0f, 0.5f, 1.0f));
        }

        if (healedCount > 0)
        {
            Debug.Log($"<color=green>[Orc Shaman]</color> Đã hồi máu cho {healedCount} đồng đội trong bán kính {healRadius}!");
        }

        yield return new WaitForSeconds(0.3f);

        nextHealTime = Time.time + healCooldown;
        isAttacking = false;
        ChangeState(ShamanState.Chase);
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

    private void ChangeState(ShamanState newState)
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
        if (!showGizmos) return;

        Vector3 basePosition = GetCenterPosition();

        // Bán kính phát hiện (tím nhạt)
        Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.2f);
        Gizmos.DrawWireSphere(basePosition, detectionRadius);

        // Bán kính bỏ đuổi (xám)
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.15f);
        Gizmos.DrawWireSphere(basePosition, loseSightRadius);

        // Tầm bắn (vàng)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(basePosition, shootRange);

        // Khoảng cách an toàn (đỏ nhạt - lùi lại nếu Player vào vùng này)
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.2f);
        Gizmos.DrawWireSphere(basePosition, safeDistance);

        // Bán kính hồi máu đồng đội (xanh lá)
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.2f);
        Gizmos.DrawWireSphere(basePosition, healRadius);
    }
}
