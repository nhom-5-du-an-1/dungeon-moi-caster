using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        ReturnToSpawn
    }

    [Header("=== MỤC TIÊU & NHẬN DIỆN ===")]
    [Tooltip("Mục tiêu theo dõi (Nếu để trống, script sẽ tự động tìm Player có tag 'Player')")]
    public Transform playerTarget;
    [Tooltip("Bán kính phát hiện Player để bắt đầu đuổi theo (đơn vị)")]
    public float detectionRadius = 5.0f;
    [Tooltip("Bán kính tối đa để từ bỏ đuổi theo và quay về vị trí cũ (đơn vị)")]
    public float loseSightRadius = 8.0f;
    [Tooltip("Khoảng cách tầm đánh tối đa (dùng tham chiếu)")]
    public float attackRange = 0.35f;

    [Header("=== DI CHUYỂN ===")]
    [Tooltip("Tốc độ di chuyển của quái vật khi đuổi theo")]
    public float moveSpeed = 2.5f;
    [Tooltip("Tốc độ đi tuần khi rảnh rỗi")]
    public float patrolSpeed = 1.2f;
    [Tooltip("Hệ số làm mượt di chuyển (0 = dừng ngay, giá trị cao = trượt mượt)")]
    [Range(0f, 0.5f)] public float movementSmoothing = 0.05f;

    [Header("=== TẤN CÔNG (AGGRESSIVE MELEE) ===")]
    [Tooltip("Thời gian giãn cách giữa các lần chém (giây)")]
    public float attackCooldown = 1.0f;
    [Tooltip("Thời gian khóa di chuyển khi thực hiện đòn chém (giây)")]
    public float attackDuration = 0.45f;
    [Tooltip("Thời gian trễ từ khi vung tay đến khi đòn chém hit thực sự (giây)")]
    public float attackDelay = 0.12f;
    [Tooltip("Lực nhích nhẹ dứt khoát về phía trước đúng khoảnh khắc hit chém")]
    public float lungeForce = 1.0f;

    [Header("=== HITBOX TẤN CÔNG (CHUẨN TOP-DOWN 2D) ===")]
    [Tooltip("Sử dụng Hitbox dạng hình Tròn/Cung quạt (Chuẩn các game 2D Top-Down như Zelda, Soul Knight, Enter the Gungeon)")]
    public bool useCircleHitbox = true;
    [Tooltip("Bán kính hình tròn đòn chém (Hitbox hình tròn phủ đều 360 độ góc nhìn từ trên xuống)")]
    public float attackRadius = 0.32f;
    [Tooltip("Kích thước hình hộp chữ nhật (chỉ áp dụng nếu không dùng hình tròn)")]
    public Vector2 attackBoxSize = new Vector2(0.4f, 0.3f);
    [Tooltip("Khoảng cách lệch từ tâm quái đến tâm đòn chém")]
    public float attackOffset = 0.20f;
    [Tooltip("Độ lệch tâm của hình ảnh quái so với gốc tọa độ")]
    public Vector2 spriteOffset = Vector2.zero;
    [Tooltip("Lớp Layer chứa Player")]
    public LayerMask playerLayer;
    [Tooltip("Sát thương đòn đánh của quái (Chuẩn Minecraft: 2 HP = 1 Tim đỏ, 1 HP = Nửa tim)")]
    public int attackDamage = 2;
    [Tooltip("Hiển thị khung Hitbox màu đỏ trực quan khi quái thực hiện đòn chém")]
    public bool showHitbox = true;

    // Thành phần Unity
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private LineRenderer hitboxLineRenderer;

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (attackDamage <= 0)
        {
            attackDamage = 2;
        }

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
        
        // Chuẩn Minecraft: Quái 10 HP = 5 trái tim
        if (health.maxHealth <= 0 || health.maxHealth == 50)
        {
            health.maxHealth = 10;
        }
        health.useHeartDisplay = true;
        health.heartCount = 5;
        health.alwaysShowHealthBar = true;
        health.barHeightOffset = 0.06f;
        health.barWidth = 0.28f;
        health.barHeight = 0.04f;
    }

    public Vector3 GetCenterPosition()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            return sr.bounds.center;
        }
        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
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
    /// Kiểm tra chính xác xem Player có ĐANG nằm trong vùng phủ đòn chém thực tế hay chưa
    /// </summary>
    public bool IsPlayerInAttackZone()
    {
        if (playerTarget == null) return false;

        Vector3 myPos = GetCenterPosition();
        Vector3 playerPos = GetPlayerCenterPosition();
        Vector2 dirToPlayer = (playerPos - myPos);
        float dist = dirToPlayer.magnitude;

        if (dist < 0.001f) return true;

        Vector2 facing = dirToPlayer.normalized;
        Vector3 attackPoint = myPos + (Vector3)(facing * attackOffset);

        if (useCircleHitbox)
        {
            // Quét hình tròn xem có chạm Player không
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint, attackRadius, playerLayer);
            foreach (Collider2D col in hits)
            {
                if (col.transform == playerTarget || col.transform.IsChildOf(playerTarget) || col.CompareTag("Player") || col.GetComponentInParent<PlayerMovement>() != null)
                {
                    return true;
                }
            }

            // Hoặc khoảng cách mép vòng tròn đủ phủ chạm thân Player
            float effectiveReach = attackOffset + (attackRadius * 0.7f);
            return dist <= effectiveReach;
        }
        else
        {
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            Collider2D[] hits = Physics2D.OverlapBoxAll(attackPoint, attackBoxSize, angle, playerLayer);
            foreach (Collider2D col in hits)
            {
                if (col.transform == playerTarget || col.transform.IsChildOf(playerTarget) || col.CompareTag("Player") || col.GetComponentInParent<PlayerMovement>() != null)
                {
                    return true;
                }
            }
            float effectiveReach = attackOffset + (attackBoxSize.x * 0.4f);
            return dist <= effectiveReach;
        }
    }

    private PlayerStats playerStatsTarget;

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

        // Khi Player đã chết, quái lập tức bỏ theo đuổi, ngừng lắc và quay về đi tuần bình thường
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
                // CHỈ BẮT ĐẦU CHÉM KHI QUÁI ĐÃ ÁP SÁT ĐỦ ĐỂ HITBOX CHẠM VÀO THÂN PLAYER
                if (IsPlayerInAttackZone() && Time.time >= nextAttackTime)
                {
                    StartCoroutine(PerformAttackRoutine());
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

        // Luôn bám hướng nhìn về phía Player
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
        if (sr == null) sr = GetComponent<SpriteRenderer>();
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
                // Áp sát liên tục cho tới khi Hitbox thực sự trùm lên Player mới dừng vung chiêu
                if (IsPlayerInAttackZone())
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

        targetInput += CalculateSeparationForce();

        SetAnimFloat("Speed", targetInput.sqrMagnitude);

        smoothMovement = Vector2.SmoothDamp(smoothMovement, targetInput, ref velocityWorkspace, movementSmoothing);
        rb.MovePosition(rb.position + smoothMovement * currentSpeed * Time.fixedDeltaTime);
    }

    private IEnumerator PerformAttackRoutine()
    {
        currentState = EnemyState.Attack;
        isAttacking = true;

        rb.linearVelocity = Vector2.zero;
        smoothMovement = Vector2.zero;
        velocityWorkspace = Vector2.zero;

        AimAtPlayer();

        SetAnimTrigger("Attack");
        SetAnimTrigger("Attack1");

        // 1. Trong thời gian giơ tay (attackDelay), đứng im ngắm chính xác hướng Player
        float elapsedTime = 0f;
        while (elapsedTime < attackDelay)
        {
            elapsedTime += Time.deltaTime;
            AimAtPlayer();
            yield return null;
        }

        // 2. 🎯 ĐÚNG KHOẢNH KHẮC HIT CHÉM: Nhích nhẹ 1 cú dứt khoát đồng thời xuất Hitbox va chạm
        if (lungeForce > 0f)
        {
            rb.linearVelocity = lastFacingDirection.normalized * lungeForce;
        }

        Vector3 basePosition = GetCenterPosition();
        Vector3 attackPoint = basePosition + (Vector3)(lastFacingDirection.normalized * attackOffset);
        float angle = Mathf.Atan2(lastFacingDirection.y, lastFacingDirection.x) * Mathf.Rad2Deg;

        ShowHitboxVisual(attackPoint, attackBoxSize, angle);

        Collider2D[] hitColliders;
        if (useCircleHitbox)
        {
            hitColliders = Physics2D.OverlapCircleAll(attackPoint, attackRadius);
        }
        else
        {
            hitColliders = Physics2D.OverlapBoxAll(attackPoint, attackBoxSize, angle);
        }

        bool hasHitPlayer = false;
        foreach (Collider2D col in hitColliders)
        {
            if (col.transform == playerTarget || col.transform.IsChildOf(playerTarget) || col.CompareTag("Player") || col.GetComponent<PlayerMovement>() != null || col.GetComponentInParent<PlayerMovement>() != null)
            {
                hasHitPlayer = true;
                Debug.Log($"<color=orange>[Enemy Combat]</color> Quái {gameObject.name} chém trúng Player ({col.name}) gây ra {attackDamage} sát thương!");
                
                PlayerStats playerStats = col.GetComponent<PlayerStats>();
                if (playerStats == null) playerStats = col.GetComponentInParent<PlayerStats>();
                if (playerStats == null && playerTarget != null) playerStats = playerTarget.GetComponent<PlayerStats>();
                if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
                
                if (playerStats != null)
                {
                    playerStats.TakeDamage(attackDamage, GetCenterPosition(), 4.5f);
                }
                break;
            }
        }

        // Dự phòng: Nếu khoảng cách từ tâm đòn chém tới Player đủ gần, tự động tính trúng đòn
        if (!hasHitPlayer && playerTarget != null)
        {
            float distToPlayer = Vector3.Distance(attackPoint, playerTarget.position);
            if (distToPlayer <= attackRadius + 0.25f)
            {
                hasHitPlayer = true;
                Debug.Log($"<color=orange>[Enemy Combat Fallback]</color> Quái {gameObject.name} chém trúng Player (Khoảng cách: {distToPlayer:F2}) gây ra {attackDamage} sát thương!");
                PlayerStats playerStats = playerTarget.GetComponent<PlayerStats>();
                if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(attackDamage, GetCenterPosition(), 4.5f);
                }
            }
        }

        if (!hasHitPlayer)
        {
            Debug.Log($"<color=yellow>[Enemy Combat]</color> Quái {gameObject.name} chém hụt! (Player nằm ngoài Hitbox)");
        }

        if (lungeForce > 0f)
        {
            yield return new WaitForSeconds(0.06f);
            rb.linearVelocity = Vector2.zero;
        }

        float remainingDuration = attackDuration - attackDelay - 0.06f;
        if (remainingDuration > 0f)
        {
            yield return new WaitForSeconds(remainingDuration);
        }

        HideHitboxVisual();

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
        ChangeState(EnemyState.Chase);
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
            if (col.gameObject != gameObject && col.GetComponent<EnemyAI>() != null)
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
        if (p == null)
        {
            p = GameObject.Find("DarkFantasyPlayer");
        }
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
        Transform child = transform.Find("EnemyHitboxVisualizer");
        GameObject lineObj = child != null ? child.gameObject : new GameObject("EnemyHitboxVisualizer");
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
        hitboxLineRenderer.startColor = Color.red;
        hitboxLineRenderer.endColor = Color.red;
        hitboxLineRenderer.sortingOrder = 100;
        hitboxLineRenderer.enabled = false;
    }

    private void ShowHitboxVisual(Vector3 center, Vector2 size, float angle)
    {
        if (hitboxLineRenderer == null) return;

        if (!showHitbox)
        {
            hitboxLineRenderer.enabled = false;
            return;
        }

        Vector3 localCenter = transform.InverseTransformPoint(center);

        if (useCircleHitbox)
        {
            int segments = 24;
            hitboxLineRenderer.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float rad = (i / (float)segments) * 2f * Mathf.PI;
                Vector3 point = localCenter + new Vector3(Mathf.Cos(rad) * attackRadius, Mathf.Sin(rad) * attackRadius, 0f);
                hitboxLineRenderer.SetPosition(i, point);
            }
            hitboxLineRenderer.enabled = true;
        }
        else
        {
            hitboxLineRenderer.positionCount = 5;
            Vector2 halfSize = size * 0.5f;
            Quaternion rot = Quaternion.Euler(0, 0, angle);

            Vector3 p1 = localCenter + rot * new Vector3(-halfSize.x, -halfSize.y, 0);
            Vector3 p2 = localCenter + rot * new Vector3(halfSize.x, -halfSize.y, 0);
            Vector3 p3 = localCenter + rot * new Vector3(halfSize.x, halfSize.y, 0);
            Vector3 p4 = localCenter + rot * new Vector3(-halfSize.x, halfSize.y, 0);

            hitboxLineRenderer.SetPosition(0, p1);
            hitboxLineRenderer.SetPosition(1, p2);
            hitboxLineRenderer.SetPosition(2, p3);
            hitboxLineRenderer.SetPosition(3, p4);
            hitboxLineRenderer.SetPosition(4, p1);
            hitboxLineRenderer.enabled = true;
        }
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

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.2f);
        Gizmos.DrawWireSphere(basePosition, detectionRadius);

        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.15f);
        Gizmos.DrawWireSphere(basePosition, loseSightRadius);

        Vector2 direction = Application.isPlaying ? lastFacingDirection : new Vector2(1f, 0f);
        Vector3 attackPoint = basePosition + (Vector3)(direction.normalized * attackOffset);

        if (useCircleHitbox)
        {
            Gizmos.color = isAttacking ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(attackPoint, attackRadius);
        }
        else
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(attackPoint, Quaternion.Euler(0, 0, angle), Vector3.one);
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = rotationMatrix;
            Gizmos.color = isAttacking ? Color.red : Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(attackBoxSize.x, attackBoxSize.y, 0.1f));
            Gizmos.matrix = oldMatrix;
        }
    }
}
