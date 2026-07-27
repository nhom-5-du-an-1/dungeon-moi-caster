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
    [Tooltip("Hệ số giảm tốc giúp dừng chuyển động mượt mà hơn")]
    [Range(0f, 0.5f)] public float movementSmoothing = 0.05f;
    public Vector2 spriteOffset = Vector2.zero;

    [Header("=== TẤN CÔNG BẰNG VŨ KHÍ ===")]
    [Tooltip("Thời gian giãn cách giữa các cú chém vũ khí (giây)")]
    public float attackCooldown = 0.35f;
    [Tooltip("Thời gian tạm khóa di chuyển khi đang chém (giây)")]
    public float attackDuration = 0.25f;

    [Header("=== THÔNG SỐ TƯƠNG THÍCH EDITOR WIZARD ===")]
    public LayerMask enemyLayer;
    public float attackOffset = 0.20f;
    public Vector2 attackBoxSize = new Vector2(0.4f, 0.3f);
    public float attackDelay = 0.1f;
    public float lungeForce = 2f;
    public float comboResetWindow = 1.0f;
    public int attackDamage = 10;

    // Các thành phần Unity
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private HashSet<string> animParamsCache;

    // Biến trạng thái di chuyển & tấn công
    private Vector2 movementInput;
    private Vector2 smoothMovement;
    private Vector2 velocityWorkspace = Vector2.zero;
    private Vector2 lastMoveDirection = new Vector2(1f, 0f);
    private bool isAttacking = false;
    private float nextAttackTime = 0f;
    private PlayerStats playerStats;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        playerStats = GetComponent<PlayerStats>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (GetComponent<WeaponController>() == null)
        {
            gameObject.AddComponent<WeaponController>();
        }

        CacheAnimatorParameters();
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
        return transform.position;
    }

    void Update()
    {
        if (playerStats != null && playerStats.IsDead)
        {
            movementInput = Vector2.zero;
            return;
        }

        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        movementInput.Normalize();

        if (isAttacking)
        {
            movementInput = Vector2.zero;
        }

        if (movementInput.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = movementInput;
            sr.flipX = (lastMoveDirection.x < -0.01f);
        }

        SetAnimFloat("Speed", movementInput.sqrMagnitude);

        // Xử lý nút bấm Tấn Công (J / Z / Click Chuột Trái)
        if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Z) || Input.GetMouseButtonDown(0))
        {
            if (Time.time >= nextAttackTime && !isAttacking)
            {
                // Khi Túi Đồ đang mở -> KHÔNG chém đòn (Dành chuột để kéo thả item!)
                if (InventoryManager.Instance != null && InventoryManager.Instance.isOpen)
                {
                    return;
                }

                WeaponController weapon = WeaponController.Instance ?? GetComponent<WeaponController>();
                if (weapon != null && weapon.isWeaponEquipped)
                {
                    weapon.PerformAxeSwing(lastMoveDirection);
                    StartCoroutine(WeaponAttackLockRoutine());
                }
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

    /// <summary>
    /// Khóa di chuyển nhẹ trong lúc vũ khí đang thực hiện cú chém bổ
    /// </summary>
    private IEnumerator WeaponAttackLockRoutine()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        rb.linearVelocity = Vector2.zero;
        smoothMovement = Vector2.zero;
        velocityWorkspace = Vector2.zero;

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
    }

    private void CacheAnimatorParameters()
    {
        animParamsCache = new HashSet<string>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter param in anim.parameters)
            {
                animParamsCache.Add(param.name);
            }
        }
    }

    public void SetAnimFloat(string name, float value)
    {
        if (anim != null && animParamsCache != null && animParamsCache.Contains(name))
        {
            anim.SetFloat(name, value);
        }
    }

    public void SetAnimTrigger(string name)
    {
        if (anim != null && animParamsCache != null && animParamsCache.Contains(name))
        {
            anim.SetTrigger(name);
        }
    }
}
