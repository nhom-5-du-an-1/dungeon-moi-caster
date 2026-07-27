using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Bảng Túi Đồ (Inventory Window) trong Game.
/// Điều khiển việc Mở / Đóng túi đồ khi bấm vào Túi Đồ trên Thanh Đồ hoặc bấm phím B, I, Tab.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("=== INVENTORY UI ===")]
    public GameObject inventoryPanel; // Khung Bảng Túi Đồ
    public Transform gridContainer;   // Nơi chứa các ô o_chua_do
    public KeyCode toggleKey = KeyCode.B; // Phím tắt mở túi (B / I / Tab)

    [Header("=== STATE ===")]
    public bool isOpen = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (GetComponent<ItemDragManager>() == null && ItemDragManager.Instance == null)
        {
            gameObject.AddComponent<ItemDragManager>();
        }
    }

    void Start()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        isOpen = false;
    }

    void Update()
    {
        // Đã gỡ bỏ tính năng bật/tắt Bảng Túi Đồ theo yêu cầu người dùng
    }

    /// <summary>
    /// Chuyển đổi trạng thái Mở / Đóng Túi Đồ
    /// </summary>
    public void ToggleInventory()
    {
        CloseInventory();
    }

    [Header("=== INVENTORY SLOTS ===")]
    public List<ItemSlot> inventorySlots = new List<ItemSlot>();

    /// <summary>
    /// Đồng bộ và cài đặt khả năng click chuột kéo thả chuẩn 100% cho TẤT CẢ các ô chứa đồ trong Bảng Túi Đồ khi mở Túi Đồ
    /// </summary>
    public void SetupInventorySlots()
    {
        inventorySlots.Clear();

        // 1. Tắt raycastTarget của tất cả các tấm ảnh phông nền xung quanh (tránh cản trở click chuột vào các ô chứa đồ)
        if (inventoryPanel != null)
        {
            Image[] panelImages = inventoryPanel.GetComponentsInChildren<Image>(true);
            foreach (Image img in panelImages)
            {
                ItemSlot slot = img.GetComponent<ItemSlot>() ?? img.GetComponentInParent<ItemSlot>();
                if (slot == null && img.gameObject != inventoryPanel)
                {
                    img.raycastTarget = false; // Phông nền KHÔNG cản click
                }
            }
        }

        // 2. Tự động kiểm tra và gắn ItemSlot cho tất cả các ô con/cháu trong Bảng Túi Đồ
        Transform targetParent = gridContainer != null ? gridContainer : (inventoryPanel != null ? inventoryPanel.transform : null);
        if (targetParent != null)
        {
            for (int i = 0; i < targetParent.childCount; i++)
            {
                Transform child = targetParent.GetChild(i);
                if (child.childCount > 0 && child.GetComponent<ItemSlot>() == null)
                {
                    for (int j = 0; j < child.childCount; j++)
                    {
                        Transform grandChild = child.GetChild(j);
                        EnsureSlotComponent(grandChild, inventorySlots.Count + 10);
                    }
                }
                else
                {
                    EnsureSlotComponent(child, inventorySlots.Count + 10);
                }
            }
        }

        // 3. Đăng ký tất cả các ô ItemSlot trong Scene (bao gồm cả ô đang ẩn)
        ItemSlot[] allSlots = Object.FindObjectsByType<ItemSlot>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        foreach (ItemSlot slot in allSlots)
        {
            if (slot != null)
            {
                slot.EnsureIconObject();

                Image bgImg = slot.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.raycastTarget = true; // Ô chứa đồ BẮT BUỘC nhận click kéo thả
                }

                Collider2D col = slot.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.enabled = true;
                }

                if (!inventorySlots.Contains(slot))
                {
                    inventorySlots.Add(slot);
                }
            }
        }

        Debug.Log($"<color=green><b>[InventoryManager] Đã đồng bộ thành công {inventorySlots.Count} ô chứa đồ cho Bảng Túi Đồ!</b></color>");
    }

    private void EnsureSlotComponent(Transform slotTrans, int defaultIndex)
    {
        if (slotTrans == null) return;

        ItemSlot slot = slotTrans.GetComponent<ItemSlot>();
        if (slot == null)
        {
            slot = slotTrans.gameObject.AddComponent<ItemSlot>();
            slot.slotIndex = defaultIndex;
        }

        Image img = slotTrans.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
        }

        slot.EnsureIconObject();
        if (!inventorySlots.Contains(slot))
        {
            inventorySlots.Add(slot);
        }
    }

    /// <summary>
    /// Mở Bảng Túi Đồ
    /// </summary>
    public void OpenInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            isOpen = true;
            SetupInventorySlots();
            Debug.Log("<color=green><b>[InventoryManager] Đã mở Bảng Túi Đồ & Đồng bộ các ô chứa!</b></color>");
        }
        else
        {
            Debug.LogWarning("[InventoryManager] Chưa gán inventoryPanel trong Inspector!");
        }
    }

    /// <summary>
    /// Đóng Bảng Túi Đồ
    /// </summary>
    public void CloseInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isOpen = false;
            Debug.Log("<color=yellow><b>[InventoryManager] Đã đóng Bảng Túi Đồ.</b></color>");
        }
    }
}
