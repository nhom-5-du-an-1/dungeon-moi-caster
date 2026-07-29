using System.Collections;
using UnityEngine;

public class MonsterProjectile : MonoBehaviour
{
    public enum ElementType
    {
        Normal,
        Poison,
        Frost,
        Fire
    }

    [Header("=== THÔNG SỐ ĐẠN ===")]
    public float speed = 5.0f;
    public int damage = 2;
    public float lifeTime = 5.0f;
    public float knockbackForce = 3.0f;
    public ElementType elementType = ElementType.Normal;

    private Vector2 moveDirection;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool hasCollided = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
        ConfigureVisuals();
    }

    public void Setup(Vector2 direction, ElementType type, int dmg, float bulletSpeed)
    {
        moveDirection = direction.normalized;
        elementType = type;
        damage = dmg;
        speed = bulletSpeed;

        if (rb != null)
        {
            rb.linearVelocity = moveDirection * speed;
        }

        // Xoay đạn hướng theo hướng bay
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        ConfigureVisuals();
    }

    private void ConfigureVisuals()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        switch (elementType)
        {
            case ElementType.Poison:
                sr.color = new Color(0.2f, 0.9f, 0.2f, 1.0f); // Xanh lá độc
                break;
            case ElementType.Frost:
                sr.color = new Color(0.3f, 0.75f, 1.0f, 1.0f); // Xanh băng lam
                break;
            case ElementType.Fire:
                sr.color = new Color(1.0f, 0.35f, 0.1f, 1.0f); // Đỏ lửa cam
                break;
            default:
                sr.color = Color.white; // Đạn thường màu trắng
                break;
        }
    }

    void Update()
    {
        // Dự phòng nếu Rigidbody2D không tự di chuyển
        if (rb == null || rb.bodyType == RigidbodyType2D.Kinematic)
        {
            transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasCollided) return;

        // Tránh va chạm với quái vật khác hoặc vùng trigger khác
        if (collision.CompareTag("Enemy") || collision.GetComponent<EnemyHealth>() != null || collision.isTrigger)
        {
            return;
        }

        hasCollided = true;

        if (collision.CompareTag("Player") || collision.GetComponent<PlayerStats>() != null || collision.GetComponentInParent<PlayerStats>() != null)
        {
            PlayerStats playerStats = collision.GetComponent<PlayerStats>() ?? collision.GetComponentInParent<PlayerStats>() ?? PlayerStats.Instance;
            if (playerStats != null)
            {
                // Gây sát thương cơ bản và knockback lên Player
                playerStats.TakeDamage(damage, transform.position, knockbackForce);

                // Áp dụng hiệu ứng trạng thái nguyên tố thông qua coroutine của PlayerStats
                ApplyElementalStatusEffect(playerStats);
            }

            SpawnImpactEffect();
            Destroy(gameObject);
        }
        else
        {
            // Va chạm với tường hoặc chướng ngại vật khác
            Debug.Log($"<color=cyan>[Projectile]</color> Đạn va chạm vật cản ({collision.name}) và biến mất.");
            SpawnImpactEffect();
            Destroy(gameObject);
        }
    }

    private void ApplyElementalStatusEffect(PlayerStats player)
    {
        if (player == null || player.isDead) return;

        switch (elementType)
        {
            case ElementType.Poison:
                player.StartCoroutine(PoisonStatusRoutine(player, 1, 0.5f, 4));
                break;
            case ElementType.Frost:
                player.StartCoroutine(FrostStatusRoutine(player, 2.0f, 0.5f));
                break;
            case ElementType.Fire:
                player.StartCoroutine(BurnStatusRoutine(player, 2, 0.5f, 4));
                break;
        }
    }

    private IEnumerator PoisonStatusRoutine(PlayerStats player, int tickDamage, float interval, int ticks)
    {
        SpriteRenderer playerSr = player.GetComponent<SpriteRenderer>();
        Color originalColor = playerSr != null ? playerSr.color : Color.white;

        FloatingTextManager.SpawnWorldText("☣️ NHIỄM ĐỘC!", player.transform.position + Vector3.up * 0.5f, new Color(0.2f, 0.8f, 0.2f, 1.0f));

        for (int i = 0; i < ticks; i++)
        {
            if (player == null || player.isDead) break;

            if (playerSr != null) playerSr.color = new Color(0.3f, 0.8f, 0.3f, 1.0f); // Đổi màu xanh lá độc khi tick sát thương

            player.TakeDamage(tickDamage); // Sát thương chuẩn không knockback
            
            yield return new WaitForSeconds(0.12f);
            if (playerSr != null && !player.isDead) playerSr.color = originalColor; // Khôi phục màu nhanh

            yield return new WaitForSeconds(interval - 0.12f);
        }

        if (playerSr != null && player != null) playerSr.color = originalColor;
    }

    private IEnumerator FrostStatusRoutine(PlayerStats player, float duration, float speedMultiplier)
    {
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm == null) yield break;

        SpriteRenderer playerSr = player.GetComponent<SpriteRenderer>();
        Color originalColor = playerSr != null ? playerSr.color : Color.white;
        float originalSpeed = pm.moveSpeed;

        FloatingTextManager.SpawnWorldText("❄️ BỊ LÀM CHẬM!", player.transform.position + Vector3.up * 0.5f, new Color(0.3f, 0.7f, 1.0f, 1.0f));
        
        // Giảm tốc độ di chuyển
        pm.moveSpeed = originalSpeed * speedMultiplier;
        if (playerSr != null) playerSr.color = new Color(0.4f, 0.7f, 1.0f, 1.0f); // Ánh màu xanh băng lam

        yield return new WaitForSeconds(duration);

        // Khôi phục trạng thái ban đầu
        if (pm != null) pm.moveSpeed = originalSpeed;
        if (playerSr != null) playerSr.color = originalColor;
    }

    private IEnumerator BurnStatusRoutine(PlayerStats player, int tickDamage, float interval, int ticks)
    {
        SpriteRenderer playerSr = player.GetComponent<SpriteRenderer>();
        Color originalColor = playerSr != null ? playerSr.color : Color.white;

        FloatingTextManager.SpawnWorldText("🔥 THIÊU ĐỐT!", player.transform.position + Vector3.up * 0.5f, new Color(1.0f, 0.4f, 0.0f, 1.0f));

        for (int i = 0; i < ticks; i++)
        {
            if (player == null || player.isDead) break;

            if (playerSr != null) playerSr.color = new Color(1.0f, 0.5f, 0.1f, 1.0f); // Ánh đỏ cam rực lửa

            player.TakeDamage(tickDamage);
            
            yield return new WaitForSeconds(0.12f);
            if (playerSr != null && !player.isDead) playerSr.color = originalColor;

            yield return new WaitForSeconds(interval - 0.12f);
        }

        if (playerSr != null && player != null) playerSr.color = originalColor;
    }

    private void SpawnImpactEffect()
    {
        // Hiển thị text hoặc hiệu ứng nhỏ khi đạn vỡ
        // (Nếu có Particle System có thể kích hoạt ở đây)
    }
}
