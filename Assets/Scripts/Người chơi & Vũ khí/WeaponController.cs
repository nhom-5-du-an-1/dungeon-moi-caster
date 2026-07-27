using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý Rìu Lửa vung chém - Cho phép TỰ ĐIỀU CHỈNH VỊ TRÍ THỦ CÔNG 100% trong Unity Inspector
/// </summary>
public class WeaponController : MonoBehaviour
{
    public static WeaponController Instance { get; private set; }

    [Header("=== EQUIPPED WEAPON (GÁN THỦ CÔNG IN INSPECTOR) ===")]
    public GameObject weaponHolder;      // Kéo đối tượng WeaponHolder con của DarkFantasyPlayer vào đây
    public SpriteRenderer weaponSprite; // SpriteRenderer của Rìu
    public Sprite defaultAxeSprite;     // Ảnh riu_lua.png
    public Sprite defaultIceAxeSprite;  // Ảnh riu_bang.png

    [ContextMenu("🔥 Xem Trước Rìu Lửa (Preview Fire Axe)")]
    public void PreviewFireAxeInEditor()
    {
        AutoFindComponents();
        currentWeaponName = "Rìu Lửa";
        if (weaponSprite != null)
        {
            #if UNITY_EDITOR
            if (defaultAxeSprite == null) defaultAxeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/riu_lua.png");
            #endif
            weaponSprite.sprite = defaultAxeSprite;
            weaponSprite.enabled = true;
        }
        if (weaponHolder != null)
        {
            weaponHolder.transform.localScale = fireAxeScale;
            SetIdleHoldPose(playerSr != null && playerSr.flipX);
            weaponHolder.SetActive(true);
        }
    }

    [ContextMenu("❄️ Xem Trước Rìu Băng (Preview Ice Axe)")]
    public void PreviewIceAxeInEditor()
    {
        AutoFindComponents();
        currentWeaponName = "Rìu Băng";
        if (weaponSprite != null)
        {
            #if UNITY_EDITOR
            if (defaultIceAxeSprite == null) defaultIceAxeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/riu_bang.png");
            #endif
            weaponSprite.sprite = defaultIceAxeSprite ?? defaultAxeSprite;
            weaponSprite.enabled = true;
        }
        if (weaponHolder != null)
        {
            weaponHolder.transform.localScale = iceAxeScale;
            SetIdleHoldPose(playerSr != null && playerSr.flipX);
            weaponHolder.SetActive(true);
        }
    }

    [Header("=== TỰ ĐIỀU CHỈNH VỊ TRÍ NGHỈ (TAY PHẢI & TAY TRÁI) ===")]
    [Tooltip("Vị trí cán rìu khi quay sang PHẢI (Chỉnh thủ công từ Inspector)")]
    public Vector3 rightHandOffset = new Vector3(0.25f, -0.23f, 0f);
    
    [Tooltip("Vị trí cán rìu khi quay sang TRÁI (Chỉnh thủ công từ Inspector)")]
    public Vector3 leftHandOffset = new Vector3(-0.25f, -0.23f, 0f);

    [Tooltip("Góc nghiêng cầm rìu nghỉ ban đầu (độ)")]
    public float idleHoldAngle = -25f;

    [Header("=== KÍCH THƯỚC (SCALE) VŨ KHÍ IN INSPECTOR ===")]
    [Tooltip("Tỷ lệ độ to/nhỏ của Rìu Lửa khi vung chém (Chỉnh thủ công từ Inspector)")]
    public Vector3 fireAxeScale = new Vector3(1.0f, 1.0f, 1.0f);

    [Tooltip("Tỷ lệ độ to/nhỏ của Rìu Băng khi vung chém (Chỉnh thủ công từ Inspector)")]
    public Vector3 iceAxeScale = new Vector3(1.0f, 1.0f, 1.0f);

    [Tooltip("Tỷ lệ nhân độ to khi chém (Để 1, 1, 1 để giữ NGUYÊN kích thước như khi cầm)")]
    public Vector3 chopFrameScaleMultiplier = new Vector3(1.0f, 1.0f, 1.0f);

    [Header("=== TỰ ĐIỀU CHỈNH GÓC VUNG CHẶT (TERRARIA CHOP) ===")]
    [Tooltip("Góc giơ rìu lên cao hướng về phía trước (độ)")]
    public float startSwingAngle = 45f;

    [Tooltip("Góc bổ rìu phập xuống sát đất (độ)")]
    public float endSwingAngle = -60f;

    [Tooltip("Thời gian 1 cú vung bổ (giây)")]
    public float swingDuration = 0.22f;

    [Header("=== SÁT THƯƠNG CHUẨN MINECRAFT (2 HP = 1 TIM) ===")]
    public int minFireDamage = 2;
    public int maxFireDamage = 4;

    public int minIceDamage = 2;
    public int maxIceDamage = 3;

    [Header("=== THUỘC TÍNH CHIẾN ĐẤU ===")]
    public int axeDamage = 3;            // Sát thương trung bình (1.5 tim)
    public float axeReach = 1.3f;        // Tầm chém

    public int GetRandomAxeDamage()
    {
        if (IsIceWeapon(currentWeaponName))
        {
            return Random.Range(minIceDamage, maxIceDamage + 1);
        }
        else
        {
            return Random.Range(minFireDamage, maxFireDamage + 1);
        }
    }

    [Header("=== STATE ===")]
    public bool isWeaponEquipped = false;
    public bool isSwinging = false;

    private SpriteRenderer playerSr;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this && Instance.gameObject != gameObject)
        {
            Destroy(this);
            return;
        }

        SanitizeOffsets();
    }

    void Start()
    {
        playerSr = GetComponent<SpriteRenderer>();

        if (defaultAxeSprite == null)
        {
            #if UNITY_EDITOR
            defaultAxeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/riu_lua.png") ??
                               UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/rìu lửa.png");
            #endif
        }

        SanitizeOffsets();
        UnequipWeapon();
    }

    public void SanitizeOffsets()
    {
        AutoFindComponents();
        // Không tự động can thiệp hay thay đổi bất kỳ chỉ số nào!
        // Để người dùng tự do kéo thả trong Scene View và gõ tùy chỉnh 100% trong Inspector.
    }

    private void AutoFindComponents()
    {
        if (playerSr == null) playerSr = GetComponent<SpriteRenderer>();

        if (weaponHolder == null)
        {
            Transform holderTrans = transform.Find("WeaponHolder");
            if (holderTrans != null) weaponHolder = holderTrans.gameObject;
        }

        if (weaponHolder != null && weaponSprite == null)
        {
            weaponSprite = weaponHolder.GetComponent<SpriteRenderer>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SanitizeOffsets();
    }
#endif

    private string currentWeaponName = "";
    private float skillCooldownTimer = 0f;

    void Update()
    {
        if (skillCooldownTimer > 0f)
        {
            skillCooldownTimer -= Time.deltaTime;
        }

        // Kích hoạt Kỹ năng Đặc biệt của Vũ khí (Chiêu Q hoặc Chuột Phải)
        if (isWeaponEquipped && (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1)))
        {
            TryCastWeaponSkill();
        }
    }

    private bool IsIceWeapon(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        string lower = name.ToLower();
        return lower.Contains("băng") || lower.Contains("bang") || lower.Contains("frost") || lower.Contains("ice");
    }

    /// <summary>
    /// Thi triển Kỹ Năng Tuyệt Kỹ của Vũ Khí đang cầm
    /// </summary>
    public void TryCastWeaponSkill()
    {
        if (skillCooldownTimer > 0f)
        {
            Debug.Log($"<color=yellow>[Kỹ Năng]</color> Kỹ năng đang hồi chiêu! Còn {skillCooldownTimer:F1}s");
            FloatingTextManager.SpawnWorldText($"⏳ Chờ {skillCooldownTimer:F1}s", transform.position + Vector3.up * 0.5f, Color.yellow);
            return;
        }

        if (IsIceWeapon(currentWeaponName))
        {
            CastFrostNovaSkill();
        }
        else
        {
            CastFirestormSkill();
        }
    }

    private void CastFirestormSkill()
    {
        skillCooldownTimer = 3.0f;
        Debug.Log("<color=orange><b>🔥 KÍCH HOẠT KỸ NĂNG: BÃO LỬA BÙNG NỔ! 🔥</b></color>");

        FloatingTextManager.SpawnWorldText("🔥 BÃO LỬA! 🔥", transform.position + Vector3.up * 0.7f, new Color(1.0f, 0.4f, 0.0f, 1.0f));

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2.2f);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform)) continue;

            EnemyHealth enemyHP = hit.GetComponent<EnemyHealth>() ?? hit.GetComponentInParent<EnemyHealth>();
            if (enemyHP != null)
            {
                enemyHP.TakeDamage(6, transform.position, 6.0f);
            }
        }
    }

    private void CastFrostNovaSkill()
    {
        skillCooldownTimer = 4.0f;
        Debug.Log("<color=cyan><b>❄️ KÍCH HOẠT KỸ NĂNG: SÓNG BĂNG TUYỆT ĐỐI! ❄️</b></color>");

        FloatingTextManager.SpawnWorldText("❄️ SÓNG BĂNG TUYỆT ĐỐI! ❄️", transform.position + Vector3.up * 0.7f, new Color(0.3f, 0.85f, 1.0f, 1.0f));

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3.0f);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform)) continue;

            EnemyHealth enemyHP = hit.GetComponent<EnemyHealth>() ?? hit.GetComponentInParent<EnemyHealth>();
            if (enemyHP != null)
            {
                enemyHP.TakeDamage(6, transform.position, 4.0f);
                enemyHP.Freeze(3.5f); // Đóng băng toàn bộ quái vật trong 3.5s!
            }
        }
    }

    /// <summary>
    /// Trang bị Vũ khí lên tay
    /// </summary>
    public void EquipAxe(Sprite axeIcon = null, string weaponName = "Rìu Lửa")
    {
        AutoFindComponents();

        isWeaponEquipped = true;
        currentWeaponName = string.IsNullOrEmpty(weaponName) ? (axeIcon != null ? axeIcon.name : "Rìu Lửa") : weaponName;
        Sprite spriteToUse = (axeIcon != null) ? axeIcon : defaultAxeSprite;

        // Cập nhật Sát Thương & Tầm đánh riêng cho từng loại Rìu
        if (IsIceWeapon(currentWeaponName))
        {
            axeDamage = 50;
            axeReach = 1.4f;
        }
        else
        {
            axeDamage = 35;
            axeReach = 1.3f;
        }

        if (weaponSprite != null)
        {
            weaponSprite.sprite = spriteToUse;
            weaponSprite.enabled = true;
            if (playerSr != null)
            {
                weaponSprite.sortingLayerID = playerSr.sortingLayerID;
                weaponSprite.sortingLayerName = playerSr.sortingLayerName;
                weaponSprite.sortingOrder = playerSr.sortingOrder + 5;
            }
        }

        if (weaponHolder != null)
        {
            Vector3 currentScale = IsIceWeapon(currentWeaponName) ? iceAxeScale : fireAxeScale;
            weaponHolder.transform.localScale = currentScale;

            weaponHolder.SetActive(false); // Mặc định ẩn rìu khi cầm trên tay, chỉ xuất hiện khi chém
            bool isFacingLeft = (playerSr != null && playerSr.flipX);
            SetIdleHoldPose(isFacingLeft);
        }

        Debug.Log($"<color=orange><b>[WeaponController] Đã trang bị '{currentWeaponName}'! (Damage: {axeDamage})</b></color>");
    }

    /// <summary>
    /// Cất Rìu vào túi
    /// </summary>
    public void UnequipWeapon()
    {
        isWeaponEquipped = false;
        if (weaponSprite != null)
        {
            weaponSprite.enabled = false;
        }
        if (weaponHolder != null)
        {
            weaponHolder.SetActive(false);
        }
    }

    /// <summary>
    /// Áp dụng vị trí thủ công từ Inspector
    /// </summary>
    public void SetIdleHoldPose(bool isFacingLeft)
    {
        if (weaponHolder == null) return;

        // Sử dụng vị trí chỉnh thủ công trong Inspector
        Vector3 targetOffset = isFacingLeft ? leftHandOffset : rightHandOffset;
        weaponHolder.transform.localPosition = targetOffset;
        
        Vector3 currentScale = IsIceWeapon(currentWeaponName) ? iceAxeScale : fireAxeScale;
        weaponHolder.transform.localScale = currentScale;

        if (weaponSprite != null) weaponSprite.flipX = isFacingLeft;

        float angle = isFacingLeft ? -idleHoldAngle : idleHoldAngle;
        weaponHolder.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// Vung bổ Rìu Lửa từ trên xuống chuẩn Terraria
    /// </summary>
    public void PerformAxeSwing(Vector2 lookDirection)
    {
        if (!isWeaponEquipped || isSwinging) return;
        StartCoroutine(SwingRoutine(lookDirection));
    }

    private IEnumerator SwingRoutine(Vector2 lookDirection)
    {
        isSwinging = true;

        SanitizeOffsets();

        // Xuất hiện rìu khi bắt đầu chém
        if (weaponHolder != null) weaponHolder.SetActive(true);

        bool isFacingLeft = (playerSr != null && playerSr.flipX) || lookDirection.x < -0.01f;
        if (weaponSprite != null) weaponSprite.flipX = isFacingLeft;

        Vector3 handOffset = isFacingLeft ? leftHandOffset : rightHandOffset;
        Vector3 currentScale = IsIceWeapon(currentWeaponName) ? iceAxeScale : fireAxeScale;
        if (weaponHolder != null)
        {
            weaponHolder.transform.localScale = currentScale;
            weaponHolder.transform.localPosition = handOffset;
        }

        float startAngle = isFacingLeft ? -startSwingAngle : startSwingAngle;
        float endAngle = isFacingLeft ? -endSwingAngle : endSwingAngle;

        float timer = 0f;
        HashSet<GameObject> hitEnemiesThisSwing = new HashSet<GameObject>();

        while (timer < swingDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / swingDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            float currentAngle = Mathf.Lerp(startAngle, endAngle, smoothProgress);

            if (weaponHolder != null)
            {
                weaponHolder.transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
            }

            // Kiểm tra va chạm THỰC TẾ giữa hình ảnh Rìu Lửa và quái vật trên từng khung hình vung
            CheckSpriteAxeCollision(hitEnemiesThisSwing);

            yield return null;
        }

        SetIdleHoldPose(isFacingLeft);

        // Biến mất ngay lập tức sau khi chém xong
        if (weaponHolder != null) weaponHolder.SetActive(false);

        isSwinging = false;
    }

    /// <summary>
    /// Kiểm tra va chạm CHÍNH XÁC 100%: Rìu phải CHẠM TRỰC TIẾP vào Hitbox (Collider2D) của quái vật mới gây sát thương!
    /// </summary>
    private void CheckSpriteAxeCollision(HashSet<GameObject> hitEnemies)
    {
        if (weaponSprite == null || weaponSprite.sprite == null || !weaponSprite.enabled) return;

        Vector3 pPos = transform.position;
        Vector2 facingDir = (playerSr != null && playerSr.flipX) ? Vector2.left : Vector2.right;

        // Tính toán kích thước thật (Local Unrotated Size) của Sprite rìu trong World Space (Thu gọn 65% để loại bỏ viền trong suốt xung quanh ảnh PNG)
        Sprite currentSpr = weaponSprite.sprite;
        float ppu = currentSpr.pixelsPerUnit;
        Vector3 scale = weaponHolder != null ? weaponHolder.transform.lossyScale : Vector3.one;
        Vector2 tightSize = new Vector2(
            (currentSpr.rect.width / ppu) * Mathf.Abs(scale.x) * 0.65f,
            (currentSpr.rect.height / ppu) * Mathf.Abs(scale.y) * 0.65f
        );

        Bounds axeBounds = weaponSprite.bounds;
        Vector2 boxCenter = axeBounds.center;
        float boxAngle = weaponHolder != null ? weaponHolder.transform.eulerAngles.z : 0f;

        // Quét các Collider2D nằm trong đúng hình chữ nhật xoay thu gọn của chiếc rìu
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, tightSize, boxAngle);

        float maxAllowedReach = IsIceWeapon(currentWeaponName) ? 1.5f : 1.3f;

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.gameObject == gameObject || hit.transform.IsChildOf(transform)) continue;

            // Đảm bảo mỗi con quái chỉ bị trúng 1 lần trong 1 cú vung
            GameObject hitGO = hit.gameObject;
            if (hitEnemies.Contains(hitGO)) continue;

            // Kiểm tra điểm gần nhất của Collider quái vật có CHẠM TRỰC TIẾP vào vùng tâm của hình ảnh chiếc rìu không
            Vector2 closestPointToAxe = hit.ClosestPoint(boxCenter);
            float distToAxeCenter = Vector2.Distance(boxCenter, closestPointToAxe);
            float axeTouchThreshold = Mathf.Max(tightSize.x, tightSize.y) * 0.55f;

            if (distToAxeCenter > axeTouchThreshold) continue;

            // Đảm bảo quái vật không ở cách quá xa nhân vật (Kiểm tra khoảng cách tối đa)
            Vector2 closestPointToPlayer = hit.ClosestPoint(pPos);
            float distFromPlayer = Vector2.Distance(pPos, closestPointToPlayer);
            if (distFromPlayer > maxAllowedReach) continue;

            // Bỏ qua quái vật đứng hẳn sau lưng (Góc > 85 độ so với hướng quay mặt)
            Vector2 dirToEnemy = ((Vector2)hit.transform.position - (Vector2)pPos).normalized;
            float angle = Vector2.Angle(facingDir, dirToEnemy);
            if (angle > 85f) continue;

            EnemyHealth enemyHP = hit.GetComponent<EnemyHealth>() ?? hit.GetComponentInParent<EnemyHealth>();
            if (enemyHP != null)
            {
                hitEnemies.Add(hitGO);

                int dealtDamage = GetRandomAxeDamage();
                enemyHP.TakeDamage(dealtDamage, transform.position, 4.5f);

                // TỰ ĐỘNG KÍCH HOẠT NỘI TẠI KHI CHÉM TRÚNG QUÁI VẬT:
                if (IsIceWeapon(currentWeaponName))
                {
                    if (enemyHP.isFrozen)
                    {
                        // NỘI TẠI RÌU BĂNG - VỠ BĂNG (FROST SHATTER): Chém tiếp khi quái đang Đóng Băng -> Gây thêm +4 Sát Thương Chí Mạng (2 Tim)!
                        enemyHP.TakeDamage(4, transform.position, 5.0f);
                        FloatingTextManager.SpawnWorldText("💥 VỠ BĂNG +4!", hit.transform.position + Vector3.up * 0.5f, new Color(0.3f, 0.9f, 1.0f, 1.0f));
                        Debug.Log($"<color=cyan><b>[Nội Tại Rìu Băng] VỠ BĂNG Chí Mạng quái '{hit.name}' gây thêm +4 HP!</b></color>");
                    }
                    else
                    {
                        // NỘI TẠI RÌU BĂNG - ĐÓNG BẰNG TÊ LIỆT: Đóng băng quái vật đứng yên 2.0s
                        enemyHP.Freeze(2.0f);
                        Debug.Log($"<color=cyan><b>[Nội Tại Rìu Băng] Đóng Băng Tê Liệt quái '{hit.name}' trong 2.0s!</b></color>");
                    }
                }
                else
                {
                    // NỘI TẠI RÌU LỬA: Đốt cháy trừ máu liên tục (-2 HP / 2.5s)
                    enemyHP.ApplyBurn(2, 2.5f);
                    Debug.Log($"<color=orange><b>[Nội Tại Rìu Lửa] Kích hoạt Hỏa Thiêu đốt cháy quái '{hit.name}'!</b></color>");
                }
            }
        }
    }
}
