using UnityEngine;

/// <summary>
/// Đại diện cho Vật Phẩm rơi trên bản đồ (World Pickup Item).
/// Nhân vật đi qua chạm vào sẽ tự động nhặt vào Thanh Đồ / Túi Đồ.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ItemWorldPickup : MonoBehaviour
{
    [Header("=== DỮ LIỆU VẬT PHẨM ===")]
    public ItemData itemData;
    public int amount = 1;

    [Header("=== HIỆU ỨNG NỔI BỒNG BỀNH (BOB BICKING) ===")]
    public bool enableBobbing = true;
    public float bobSpeed = 3f;
    public float bobHeight = 0.08f;

    private Vector3 startPos;
    private SpriteRenderer sr;

    void Start()
    {
        startPos = transform.position;
        sr = GetComponent<SpriteRenderer>();

        // Đảm bảo Collider là Trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        UpdateVisual();
    }

    void Update()
    {
        if (enableBobbing)
        {
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPos.x, newY, startPos.z);
        }
    }

    public void UpdateVisual()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (itemData != null && itemData.icon != null && sr != null)
        {
            sr.sprite = itemData.icon;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            PickupItem();
        }
    }

    public void PickupItem()
    {
        if (itemData == null) return;

        // Thêm item vào Hotbar hoặc Inventory
        HotbarManager hotbar = HotbarManager.Instance;
        if (hotbar != null)
        {
            bool added = false;
            foreach (ItemSlot slot in hotbar.slots)
            {
                if (slot != null && string.IsNullOrEmpty(slot.itemName))
                {
                    slot.SetItem(itemData, amount);
                    added = true;
                    Debug.Log($"<color=green><b>[Nhặt Item] Đã nhặt '{itemData.itemName}' vào ô {slot.slotIndex}!</b></color>");
                    break;
                }
            }

            if (!added)
            {
                Debug.Log("<color=yellow>[Nhặt Item] Thanh đồ đã đầy!</color>");
                return;
            }
        }

        // Tự biến mất khi đã nhặt thành công
        Destroy(gameObject);
    }
}
