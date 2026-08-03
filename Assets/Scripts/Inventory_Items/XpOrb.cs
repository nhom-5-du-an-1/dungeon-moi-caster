using System.Collections;
using UnityEngine;

/// <summary>
/// Thực thể hạt kinh nghiệm (XP Orb) rơi ra khi quái vật bị tiêu diệt.
/// Tự động bồng bềnh và hút mạnh mẽ về phía Player khi ở cự ly gần.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class XpOrb : MonoBehaviour
{
    [Header("=== THÔNG SỐ KINH NGHIỆM ===")]
    [Tooltip("Lượng XP mà Player nhận được khi nhặt hạt này")]
    public int xpValue = 5;

    [Header("=== HIỆU ỨNG HÚT (MAGNET EFFECT) ===")]
    [Tooltip("Khoảng cách tối đa bắt đầu hút Player (mét/unit)")]
    public float magnetRange = 3.5f;

    [Tooltip("Tốc độ bay ban đầu khi bắt đầu hút")]
    public float initialSpeed = 2f;

    [Tooltip("Gia tốc tăng tốc khi bay gần Player")]
    public float acceleration = 12f;

    [Header("=== HIỆU ỨNG BỒNG BỀNH (BOBBING) ===")]
    public bool enableBobbing = true;
    public float bobSpeed = 4f;
    public float bobHeight = 0.08f;

    private SpriteRenderer sr;
    private CircleCollider2D col;
    private Transform playerTransform;
    private Vector3 startPos;
    private float currentSpeed;
    private bool isBeingAttracted = false;
    private float spawnTime;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        
        // Đặt kích thước collider hợp lý để nhặt
        col.radius = 0.15f; 
    }

    void Start()
    {
        startPos = transform.position;
        spawnTime = Time.time;
        currentSpeed = initialSpeed;

        // Tìm Player
        FindPlayer();

        // Tự động tìm gán sprite nếu chưa có
        if (sr.sprite == null)
        {
            sr.sprite = Resources.Load<Sprite>("xp_orb");
#if UNITY_EDITOR
            if (sr.sprite == null)
            {
                sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/xp_orb.png");
            }
#endif
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null)
            {
                // Nếu vẫn không thấy Player, chạy hiệu ứng bồng bềnh tại chỗ
                ApplyBobbing();
                return;
            }
        }

        // Đo khoảng cách đến Player
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        // Kiểm tra xem Player có đang sống không trước khi hút
        PlayerStats playerStats = PlayerStats.Instance;
        bool isPlayerAlive = playerStats != null && !playerStats.IsDead;

        if (isPlayerAlive && (distance <= magnetRange || isBeingAttracted))
        {
            isBeingAttracted = true;

            // Tăng tốc bay dần về phía Player
            currentSpeed += acceleration * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, currentSpeed * Time.deltaTime);

            // Nếu khoảng cách cực nhỏ, tự động nhặt
            if (distance < 0.15f)
            {
                Pickup();
            }
        }
        else
        {
            isBeingAttracted = false;
            // Nếu không bị hút, bồng bềnh nhẹ nhàng trên mặt đất
            ApplyBobbing();
        }
    }

    private void FindPlayer()
    {
        if (PlayerStats.Instance != null)
        {
            playerTransform = PlayerStats.Instance.transform;
        }
        else
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
    }

    private void ApplyBobbing()
    {
        if (enableBobbing)
        {
            // Bồng bềnh theo hàm Sin, lệch pha chút dựa trên X để các hạt rơi gần nhau không bồng bềnh giống hệt nhau
            float phaseOffset = transform.position.x * 2.0f;
            float newY = startPos.y + Mathf.Sin((Time.time - spawnTime) * bobSpeed + phaseOffset) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null || other.GetComponent<PlayerStats>() != null)
        {
            Pickup();
        }
    }

    private void Pickup()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddXp(xpValue);
        }

        // Phát tiếng bip nhẹ hoặc hiệu ứng (nếu có AudioSource trong game, có thể gán ở đây)
        // Ví dụ: AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Destroy(gameObject);
    }
}
