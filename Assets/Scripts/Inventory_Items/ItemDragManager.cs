using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Quản lý cơ chế Cầm / Thả / Hoán Đổi Vật Phẩm chuẩn phong cách Minecraft.
/// Khi mở Bảng Túi Đồ, bấm Chuột Trái vào ô bất kỳ để nhặt item dính vào con trỏ chuột,
/// rồi bấm Chuột Trái vào ô muốn đặt để thả hoặc đổi chỗ 2 item với nhau!
/// </summary>
public class ItemDragManager : MonoBehaviour
{
    public static ItemDragManager Instance { get; private set; }

    [Header("=== TRẠNG THÁI ITEM ĐANG CẦM TRÊN CON TRỎ CHUỘT ===")]
    public string heldItemName = "";
    public Sprite heldItemIcon = null;
    public int heldItemCount = 0;
    public ItemData heldItemData = null;

    [Header("=== CURSOR UI ===")]
    public Canvas parentCanvas;
    public Image cursorIconImage; // Ảnh đại diện item bay theo con trỏ chuột

    public bool IsHoldingItem => heldItemIcon != null;

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

        CreateCursorOverlayUI();
    }

    void Start()
    {
        UpdateCursorUI();
    }

    void Update()
    {
        // 1. Di chuyển ảnh item đi theo vị trí con trỏ chuột khi đang cầm
        if (IsHoldingItem && cursorIconImage != null)
        {
            cursorIconImage.transform.position = Input.mousePosition;
        }

        // 2. Tự động phát hiện và xử lý click chuột trên ô chứa đồ khi Bảng Túi Đồ đang MỞ
        if (InventoryManager.Instance != null && InventoryManager.Instance.isOpen && Input.GetMouseButtonDown(0))
        {
            ItemSlot slotUnderMouse = GetSlotUnderMouse();
            if (slotUnderMouse != null)
            {
                OnSlotClicked(slotUnderMouse);
            }
        }
    }

    /// <summary>
    /// Phát hiện chính xác ô ItemSlot bên dưới vị trí con trỏ chuột (Bao gồm cả UI Canvas & 2D World Physics)
    /// </summary>
    public ItemSlot GetSlotUnderMouse()
    {
        // A. Thử tìm qua UI GraphicRaycaster (Dành cho ô UI Canvas)
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        if (EventSystem.current != null)
        {
            EventSystem.current.RaycastAll(eventData, results);
            foreach (RaycastResult result in results)
            {
                ItemSlot slot = result.gameObject.GetComponent<ItemSlot>() ?? result.gameObject.GetComponentInParent<ItemSlot>();
                if (slot != null) return slot;
            }
        }

        // B. Thử tìm qua Physics2D Overlap (Dành cho ô 2D World Sprite)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            Collider2D[] hits = Physics2D.OverlapPointAll((Vector2)worldPos);
            foreach (Collider2D hit in hits)
            {
                ItemSlot slot = hit.GetComponent<ItemSlot>() ?? hit.GetComponentInParent<ItemSlot>();
                if (slot != null) return slot;
            }
        }

        return null;
    }

    /// <summary>
    /// Tự động tạo Canvas Overlay Icon bám theo con trỏ chuột
    /// </summary>
    private void CreateCursorOverlayUI()
    {
        if (cursorIconImage != null) return;

        // Tìm hoặc tạo Canvas Overlay có SortingOrder cao nhất (999)
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            GameObject iconGO = new GameObject("MouseCursorItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGO.transform.SetParent(canvas.transform, false);

            RectTransform rect = iconGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(56f, 56f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            cursorIconImage = iconGO.GetComponent<Image>();
            cursorIconImage.raycastTarget = false; // Không cản trở click chuột vào ô bên dưới
            cursorIconImage.enabled = false;
        }
    }

    /// <summary>
    /// Cập nhật hiển thị con trỏ chuột
    /// </summary>
    private void UpdateCursorUI()
    {
        if (cursorIconImage == null) CreateCursorOverlayUI();

        if (cursorIconImage != null)
        {
            if (IsHoldingItem)
            {
                cursorIconImage.sprite = heldItemIcon;
                cursorIconImage.enabled = true;
                cursorIconImage.transform.position = Input.mousePosition;
                cursorIconImage.transform.SetAsLastSibling(); // Luôn hiển thị trên cùng
            }
            else
            {
                cursorIconImage.enabled = false;
            }
        }
    }

    /// <summary>
    /// Xử lý bấm chuột vào ô chứa đồ (Hotbar Slot hoặc Inventory Slot)
    /// </summary>
    public void OnSlotClicked(ItemSlot slot)
    {
        if (slot == null) return;

        // TRƯỜNG HỢP 1: CHUỘT ĐANG TRỐNG -> Nhặt item từ ô lên con trỏ chuột
        if (!IsHoldingItem)
        {
            if (slot.itemIcon != null || !string.IsNullOrEmpty(slot.itemName))
            {
                heldItemName = slot.itemName;
                heldItemIcon = slot.itemIcon;
                heldItemCount = slot.itemCount;
                heldItemData = slot.itemData;

                slot.ClearItem();

                Debug.Log($"<color=cyan><b>[Minecraft Drag] Đã nhặt '{heldItemName}' lên con trỏ chuột!</b></color>");
                UpdateCursorUI();
            }
        }
        // TRƯỜNG HỢP 2: CHUỘT ĐANG CẦM ITEM -> Đặt vào ô hoặc Hoán đổi 2 item
        else
        {
            // Ô mục tiêu đang TRỐNG -> Đặt item đang cầm vào ô này
            if (slot.itemIcon == null && string.IsNullOrEmpty(slot.itemName))
            {
                slot.SetItem(heldItemName, heldItemIcon, heldItemCount, heldItemData);
                ClearHeldItem();
                Debug.Log($"<color=green><b>[Minecraft Drag] Đã đặt '{slot.itemName}' vào ô {slot.slotIndex}!</b></color>");
            }
            // Ô mục tiêu CÓ ITEM -> HOÁN ĐỔI 2 VẬT PHẨM VỚI NHAU (SWAP)
            else
            {
                string tempName = slot.itemName;
                Sprite tempIcon = slot.itemIcon;
                int tempCount = slot.itemCount;
                ItemData tempData = slot.itemData;

                slot.SetItem(heldItemName, heldItemIcon, heldItemCount, heldItemData);

                heldItemName = tempName;
                heldItemIcon = tempIcon;
                heldItemCount = tempCount;
                heldItemData = tempData;

                Debug.Log($"<color=yellow><b>[Minecraft Drag] Đã hoán đổi '{slot.itemName}' và '{heldItemName}'!</b></color>");
                UpdateCursorUI();
            }
        }

        // Cập nhật lại vũ khí đang trang bị nếu thay đổi ô Hotbar đang chọn
        if (HotbarManager.Instance != null)
        {
            HotbarManager.Instance.SelectSlot(HotbarManager.Instance.currentSelectedIndex);
        }
    }

    /// <summary>
    /// Xóa item trên con trỏ chuột
    /// </summary>
    public void ClearHeldItem()
    {
        heldItemName = "";
        heldItemIcon = null;
        heldItemCount = 0;
        heldItemData = null;
        UpdateCursorUI();
    }
}
