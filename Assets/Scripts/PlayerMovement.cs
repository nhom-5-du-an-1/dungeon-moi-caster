using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMovement : MonoBehaviour
{
    [Header("=== DI CHUYỂN ===")]
    [Tooltip("Tốc độ di chuyển của nhân vật")]
    public float moveSpeed = 5f;
    [Tooltip("Hệ số giảm tốc giúp dừng chuyển động mượt mà hơn (0 = dừng ngay lập tức, giá trị cao = trượt nhẹ)")]
    [Range(0f, 0.5f)] public float movementSmoothing = 0.05f;

    [Header("=== TẤN CÔNG (CHÉM) ===")]
    [Tooltip("Thời gian giãn cách giữa các đòn chém (giây)")]
    public float attackCooldown = 0.35f;
    [Tooltip("Thời gian khóa di chuyển của nhân vật khi chém (giây)")]
    public float attackDuration = 0.3f;
    [Tooltip("Thời gian trễ từ khi nhấn chém đến khi hitbox thực sự quét (đồng bộ với frame chém của animation)")]
    public float attackDelay = 0.1f;
    [Tooltip("Lực lướt nhẹ lên phía trước khi thực hiện đòn chém để tạo cảm giác có lực hơn")]
    public float lungeForce = 2f;
    [Tooltip("Thời gian chờ tối đa để nối chiêu combo tiếp theo (giây)")]
    public float comboResetWindow = 1.0f;

    [Header("=== HITBOX TẤN CÔNG (CHUẨN TOP-DOWN 2D) ===")]
    [Tooltip("Sử dụng Hitbox dạng hình Tròn/Cung quạt (Chuẩn các game 2D Top-Down như Zelda, Soul Knight, Enter the Gungeon)")]
    public bool useCircleHitbox = true;
    [Tooltip("Bán kính hình tròn đòn chém (Phủ đều 360 độ góc nhìn từ trên xuống)")]
    public float attackRadius = 0.32f;
    [Tooltip("Kích thước hình hộp chữ nhật của đòn chém (chỉ dùng nếu không bật hình tròn)")]
    public Vector2 attackBoxSize = new Vector2(0.4f, 0.3f);
    [Tooltip("Khoảng cách lệch từ tâm nhân vật đến tâm hitbox chém")]
    public float attackOffset = 0.20f;
    [Tooltip("Độ lệch tâm của hình ảnh nhân vật so với gốc tọa độ (tự động cập nhật từ wizard)")]
    public Vector2 spriteOffset = Vector2.zero;
    [Tooltip("Lớp Layer chứa các quái vật")]
    public LayerMask enemyLayer;
    [Tooltip("Sát thương đòn đánh")]
    public int attackDamage = 10;
    [Tooltip("Hiển thị khung Hitbox màu đỏ trực quan khi thực hiện đòn chém")]
    public bool showHitbox = true;

    // Các thành phần Unity
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private LineRenderer hitboxLineRenderer;

    // Caching các tham số Animator
    private HashSet<string> animParamsCache;

    // Biến trạng thái di chuyển
    private Vector2 movementInput;
    private Vector2 smoothMovement;
    private Vector2 velocityWorkspace = Vector2.zero;
    private Vector2 lastMoveDirection = new Vector2(1f, 0f);

    // Biến trạng thái tấn công & combo
    private bool isAttacking = false;
    private float nextAttackTime = 0f;
    private float lastAttackTime = 0f;
    private int comboIndex = 0;
    private PlayerStats playerStats;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        playerStats = GetComponent<PlayerStats>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CacheAnimatorParameters();
        SetupHitboxVisualizer();
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

    void Update()
    {
        if (playerStats != null && playerStats.IsDead)
        {
            movementInput = Vector2.zero;
            return;
        }

        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        if (isAttacking)
        {
            movementInput = Vector2.zero;
        }
        else
        {
            movementInput = new Vector2(inputX, inputY).normalized;
        }

        if (movementInput.sqrMagnitude > 0.001f)
        {
            lastMoveDirection = movementInput;
            sr.flipX = (lastMoveDirection.x < -0.01f);
        }

        SetAnimFloat("Speed", movementInput.sqrMagnitude);

        if (Time.time - lastAttackTime > comboResetWindow)
        {
            comboIndex = 0;
        }

        if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Z) || Input.GetMouseButtonDown(0))
        {
            if (Time.time >= nextAttackTime && !isAttacking)
            {
                StartCoroutine(PerformAttackRoutine());
            }
        }
    }

    void FixedUpdate()
    {
        if (playerStats != null && playerStats.IsDead)
        {
            return;
        }

        if (isAttacking)
        {
            return;
        }

        smoothMovement = Vector2.SmoothDamp(smoothMovement, movementInput, ref velocityWorkspace, movementSmoothing);
        rb.MovePosition(rb.position + smoothMovement * moveSpeed * Time.fixedDeltaTime);
    }

    private IEnumerator PerformAttackRoutine()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        rb.linearVelocity = Vector2.zero;
        smoothMovement = Vector2.zero;
        velocityWorkspace = Vector2.zero;

        if (movementInput.sqrMagnitude > 0.001f)
        {
            lastMoveDirection = movementInput;
            sr.flipX = (lastMoveDirection.x < -0.01f);
        }

        string attackTrigger = "Attack" + (comboIndex + 1);
        SetAnimTrigger(attackTrigger);

        lastAttackTime = Time.time;

        yield return new WaitForSeconds(attackDelay);

        Vector2 facing = lastMoveDirection.sqrMagnitude > 0.001f ? lastMoveDirection.normalized : (sr != null && sr.flipX ? Vector2.left : Vector2.right);
        if (sr != null)
        {
            if (sr.flipX && facing.x > -0.01f) facing.x = -Mathf.Abs(facing.x > 0.01f ? facing.x : 1f);
            else if (!sr.flipX && facing.x < 0.01f) facing.x = Mathf.Abs(facing.x < -0.01f ? facing.x : 1f);
        }
        facing.Normalize();

        if (lungeForce > 0f)
        {
            rb.linearVelocity = facing * lungeForce;
        }

        Vector3 basePosition = GetCenterPosition();
        Vector3 attackPoint = basePosition + (Vector3)(facing * attackOffset);
        float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;

        ShowHitboxVisual(attackPoint, attackBoxSize, angle);

        Collider2D[] hitEnemies;
        if (useCircleHitbox)
        {
            hitEnemies = Physics2D.OverlapCircleAll(attackPoint, attackRadius, enemyLayer);
        }
        else
        {
            hitEnemies = Physics2D.OverlapBoxAll(attackPoint, attackBoxSize, angle, enemyLayer);
        }

        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log($"<color=red>[Player Combat]</color> Combo {comboIndex + 1} chém trúng: {enemy.name} gây ra {attackDamage} sát thương!");
            
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth == null) enemyHealth = enemy.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
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

        comboIndex = (comboIndex + 1) % 2;

        isAttacking = false;
    }

    private void SetupHitboxVisualizer()
    {
        Transform child = transform.Find("PlayerHitboxVisualizer");
        GameObject lineObj = child != null ? child.gameObject : new GameObject("PlayerHitboxVisualizer");
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
        if (!showHitbox || hitboxLineRenderer == null) return;

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

        Vector2 direction = Application.isPlaying ? lastMoveDirection : new Vector2(1f, 0f);
        Vector3 basePosition = GetCenterPosition();
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
