using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlantAI : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        ReturnToSpawn
    }

    [Header("=== ELEMENTAL SETTINGS ===")]
    public MonsterProjectile.ElementType elementType = MonsterProjectile.ElementType.Normal;

    [Header("=== MỤC TIÊU & NHẬN DIỆN ===")]
    [Tooltip("Mục tiêu theo dõi (Nếu để trống, script sẽ tự động tìm Player có tag 'Player')")]
    public Transform playerTarget;
    [Tooltip("Bán kính phát hiện Player để bắt đầu đuổi theo (đơn vị)")]
    public float detectionRadius = 5.0f;
    [Tooltip("Bán kính tối đa để từ bỏ đuổi theo và quay về vị trí cũ (đơn vị)")]
    public float loseSightRadius = 8.0f;
    [Tooltip("Khoảng cách tầm bắn tối đa (Tầm đánh của Plant)")]
    public float shootRange = 4.0f;

    [Header("=== DI CHUYỂN ===")]
    [Tooltip("Tốc độ di chuyển của quái vật khi đuổi theo")]
    public float moveSpeed = 1.5f;
    [Tooltip("Tốc độ đi tuần khi rảnh rỗi")]
    public float patrolSpeed = 0.8f;
    [Tooltip("Hệ số làm mượt di chuyển (0 = dừng ngay, giá trị cao = trượt mượt)")]
    [Range(0f, 0.5f)] public float movementSmoothing = 0.05f;

    [Header("=== TẤN CÔNG RANGED ===")]
    [Tooltip("Prefab của viên đạn bắn ra")]
    public GameObject projectilePrefab;
    [Tooltip("Tốc độ bay của viên đạn")]
    public float projectileSpeed = 5.0f;
    [Tooltip("Sát thương viên đạn gây ra")]
    public int projectileDamage = 2;
    [Tooltip("Thời gian giãn cách giữa các lần bắn (giây)")]
    public float attackCooldown = 2.0f;
    [Tooltip("Thời gian khóa di chuyển khi thực hiện đòn bắn (giây)")]
    public float attackDuration = 0.6f;
    [Tooltip("Thời gian trễ từ khi bắt đầu phun đến khi viên đạn thực sự bay ra (giây)")]
    public float attackDelay = 0.25f;

    [Header("=== THIẾT LẬP HITBOX HÌNH ẢNH ===")]
    [Tooltip("Độ lệch tâm của hình ảnh quái so với gốc tọa độ")]
    public Vector2 spriteOffset = Vector2.zero;
    [Tooltip("Hiển thị tầm bắn trực quan màu vàng")]
    public bool showGizmos = true;

    // Thành phần Unity
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    // Trạng thái AI
    private EnemyState currentState = EnemyState.Idle;
    private Vector3 spawnPoint;
    private Vector3 patrolTarget;
    private float stateTimer = 0f;
    private bool isAttacking = false;
    private float nextAttackTime = 0f;
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

        // Đặt mặc định máu 10 HP (5 tim) cho quái thường
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

        // Khi Player đã chết, quái bỏ theo đuổi, đi tuần bình thường
        if (playerStatsTarget != null && playerStatsTarget.isDead)
        {
            if (currentState == EnemyState.Chase || currentState == EnemyState.Attack)
            {
                ChangeState(EnemyState.Patrol);
                isAttacking = false;
            }
        }

        if (isAttacking) return;

        Vector3 myPos = GetCenterPosition();
        Vector3 playerPos = GetPlayerCenterPosition();
        float distanceToPlayer = Vector2.Distance(myPos, playerPos);

        switch (currentState)
        {
            case EnemyState.Idle:
                stateTimer += Time.deltaTime;
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(EnemyState.Chase);
                }
                else if (stateTimer >= 2.5f)
                {
                    patrolTarget = GetRandomPatrolPoint();
                    ChangeState(EnemyState.Patrol);
                }
                break;

            case EnemyState.Patrol:
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(EnemyState.Chase);
                }
                else if (Vector2.Distance(myPos, patrolTarget) < 0.2f)
                {
                    ChangeState(EnemyState.Idle);
                }
                break;

            case EnemyState.Chase:
                if (IsPlayerInShootRange() && Time.time >= nextAttackTime)
                {
                    StartCoroutine(PerformShootRoutine());
                }
                else if (distanceToPlayer > loseSightRadius)
                {
                    ChangeState(EnemyState.ReturnToSpawn);
                }
                break;

            case EnemyState.ReturnToSpawn:
                if (distanceToPlayer <= detectionRadius)
                {
                    ChangeState(EnemyState.Chase);
                }
                else if (Vector2.Distance(myPos, spawnPoint) < 0.3f)
                {
                    ChangeState(EnemyState.Idle);
                }
                break;
        }

        // Luôn bám hướng nhìn về phía Player khi đang rượt đuổi hoặc bắn
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
            case EnemyState.Idle:
                targetInput = Vector2.zero;
                break;

            case EnemyState.Patrol:
                targetInput = (patrolTarget - myPos).normalized;
                currentSpeed = patrolSpeed;
                break;

            case EnemyState.Chase:
                if (IsPlayerInShootRange())
                {
                    targetInput = Vector2.zero;
                }
                else
                {
                    targetInput = (playerPos - myPos).normalized;
                }
                break;

            case EnemyState.ReturnToSpawn:
                targetInput = (spawnPoint - myPos).normalized;
                currentSpeed = patrolSpeed;
                break;
        }

        // Thêm lực đẩy giãn cách giữa các quái để tránh đè lên nhau
        targetInput += CalculateSeparationForce();

        SetAnimFloat("Speed", targetInput.sqrMagnitude);

        smoothMovement = Vector2.SmoothDamp(smoothMovement, targetInput, ref velocityWorkspace, movementSmoothing);
        rb.MovePosition(rb.position + smoothMovement * currentSpeed * Time.fixedDeltaTime);
    }

    private IEnumerator PerformShootRoutine()
    {
        currentState = EnemyState.Attack;
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;
        smoothMovement = Vector2.zero;
        velocityWorkspace = Vector2.zero;

        AimAtPlayer();

        // Kích hoạt animation tấn công
        SetAnimTrigger("Attack");

        // Đợi đến thời điểm bắn đạn thực tế trong hoạt ảnh
        yield return new WaitForSeconds(attackDelay);

        // Bắn đạn
        if (playerTarget != null && projectilePrefab != null)
        {
            Vector3 spawnPos = GetCenterPosition() + (Vector3)(lastFacingDirection.normalized * 0.3f);
            GameObject bulletObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            MonsterProjectile projectile = bulletObj.GetComponent<MonsterProjectile>();
            if (projectile != null)
            {
                projectile.Setup(lastFacingDirection, elementType, projectileDamage, projectileSpeed);
            }
            Debug.Log($"<color=green>[Elemental Plant]</color> Quái {gameObject.name} bắn đạn nguyên tố {elementType}!");
        }

        // Chờ hoàn thành nốt thời gian hoạt ảnh
        float remainingDuration = attackDuration - attackDelay;
        if (remainingDuration > 0f)
        {
            yield return new WaitForSeconds(remainingDuration);
        }

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
        ChangeState(EnemyState.Chase);
    }

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

    private void ChangeState(EnemyState newState)
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
            if (col.gameObject != gameObject && (col.GetComponent<EnemyAI>() != null || col.GetComponent<PlantAI>() != null))
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

        if (p != null)
        {
            playerTarget = p.transform;
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
        if (!showGizmos) return;

        Vector3 basePosition = GetCenterPosition();

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.2f);
        Gizmos.DrawWireSphere(basePosition, detectionRadius);

        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.15f);
        Gizmos.DrawWireSphere(basePosition, loseSightRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(basePosition, shootRange);
    }
}
