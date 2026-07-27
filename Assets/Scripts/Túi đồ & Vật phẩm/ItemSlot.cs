using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Quản lý từng ô vuông trên Thanh Đồ (Hotbar Slot)
/// Hỗ trợ cả UI Image (Canvas) và SpriteRenderer (2D World/UI)
/// </summary>
public class ItemSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("=== SLOT CONFIG ===")]
    public int slotIndex = 1;
    public string itemName = "";
    public Sprite itemIcon;
    public int itemCount = 1;
    public ItemData itemData;
    public bool isSelected = false;

    [Header("=== UI COMPONENTS ===")]
    public Image iconImage;           // Ảnh hiển thị item (UI Canvas)
    public SpriteRenderer iconSprite; // Ảnh hiển thị item (2D SpriteRenderer)
    public GameObject selectionBorder;// Viền viền sáng khi ô đang được chọn

    private Vector3 originalScale;
    private Image slotBackgroundImage;
    private SpriteRenderer slotBackgroundSprite;

    void Awake()
    {
        originalScale = transform.localScale;
        EnsureIconObject();
    }

    /// <summary>
    /// Đảm bảo ô luôn có thành phần hiển thị ảnh con chính giữa (Hỗ trợ 100% mọi cấu trúc UI/2D)
    /// </summary>
    public void EnsureIconObject()
    {
        if (slotBackgroundImage == null) slotBackgroundImage = GetComponent<Image>();
        if (slotBackgroundSprite == null) slotBackgroundSprite = GetComponent<SpriteRenderer>();

        Transform iconChild = transform.Find("Icon") ?? transform.Find("ItemIcon");
        if (iconChild != null)
        {
            if (iconImage == null) iconImage = iconChild.GetComponent<Image>();
            if (iconSprite == null) iconSprite = iconChild.GetComponent<SpriteRenderer>();
        }

        if (iconImage == null && (slotBackgroundImage != null || GetComponent<RectTransform>() != null))
        {
            GameObject newIconGO = new GameObject("ItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            newIconGO.transform.SetParent(transform, false);

            RectTransform iconRect = newIconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(50f, 50f);
            iconRect.anchoredPosition = Vector2.zero;

            iconImage = newIconGO.GetComponent<Image>();
            iconImage.raycastTarget = false;
        }

        if (iconSprite == null && (slotBackgroundSprite != null || GetComponent<Collider2D>() != null || slotBackgroundImage == null))
        {
            GameObject newIconGO = new GameObject("ItemIcon", typeof(SpriteRenderer));
            newIconGO.transform.SetParent(transform, false);
            newIconGO.transform.localPosition = Vector3.zero;
            newIconGO.transform.localRotation = Quaternion.identity;
            newIconGO.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

            iconSprite = newIconGO.GetComponent<SpriteRenderer>();
            SpriteRenderer parentSr = slotBackgroundSprite ?? GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            if (parentSr != null)
            {
                iconSprite.sortingLayerID = parentSr.sortingLayerID;
                iconSprite.sortingLayerName = parentSr.sortingLayerName;
                iconSprite.sortingOrder = parentSr.sortingOrder + 50;
            }
        }

        // Tự động gắn và căn chính giữa tâm BoxCollider2D chuẩn xác 100% cho ô 2D World
        SpriteRenderer slotSr = slotBackgroundSprite ?? GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol == null && GetComponent<RectTransform>() == null)
        {
            boxCol = gameObject.AddComponent<BoxCollider2D>();
        }

        if (boxCol != null && slotSr != null && slotSr.sprite != null)
        {
            boxCol.size = slotSr.sprite.bounds.size;
            boxCol.offset = slotSr.bounds.center - transform.position;
        }
    }

    void Start()
    {
        UpdateSlotUI();
    }

    /// <summary>
    /// Gán item vào ô chứa đồ này từ ItemData
    /// </summary>
    public void SetItem(ItemData data, int count = 1)
    {
        itemData = data;
        itemName = data != null ? data.itemName : "";
        itemIcon = data != null ? data.icon : null;
        itemCount = count;
        UpdateSlotUI();
    }

    /// <summary>
    /// Gán item vào ô chứa đồ này
    /// </summary>
    public void SetItem(string name, Sprite icon, int count = 1, ItemData data = null)
    {
        itemName = name;
        itemIcon = icon;
        itemCount = count;
        itemData = data;
        UpdateSlotUI();
    }

    /// <summary>
    /// Xóa item khỏi ô
    /// </summary>
    public void ClearItem()
    {
        itemName = "";
        itemIcon = null;
        itemCount = 0;
        itemData = null;
        UpdateSlotUI();
    }

    /// <summary>
    /// Tự động tính toán độ lệch Pivot của Sprite (Bù đắp cho Pivot cán rìu (0.15, 0.15) khi cầm chém)
    /// Đảm bảo biểu tượng chiếc Rìu luôn nằm CHÍNH GIỮA 100% bên trong khung ô chứa!
    /// </summary>
    private Vector3 GetSpriteCenterOffset(Sprite sprite)
    {
        if (sprite == null || sprite.rect.width <= 0 || sprite.rect.height <= 0) return Vector3.zero;

        float normPivotX = sprite.pivot.x / sprite.rect.width;
        float normPivotY = sprite.pivot.y / sprite.rect.height;

        float offsetX = (0.5f - normPivotX) * (sprite.rect.width / sprite.pixelsPerUnit);
        float offsetY = (0.5f - normPivotY) * (sprite.rect.height / sprite.pixelsPerUnit);

        return new Vector3(offsetX, offsetY, 0f);
    }

    /// <summary>
    /// Cập nhật hiển thị UI của ô chứa đồ (Căn chính giữa tâm ô 100%)
    /// </summary>
    public void UpdateSlotUI()
    {
        EnsureIconObject();

        bool isUI = GetComponent<RectTransform>() != null || slotBackgroundImage != null;

        if (isUI && iconImage != null && iconImage.gameObject != gameObject)
        {
            if (iconSprite != null) iconSprite.enabled = false;

            if (itemIcon != null)
            {
                iconImage.sprite = itemIcon;
                iconImage.enabled = true;
                iconImage.color = Color.white;
                iconImage.rectTransform.anchoredPosition = Vector2.zero;
                iconImage.rectTransform.localRotation = Quaternion.identity;
                iconImage.rectTransform.localScale = Vector3.one;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        else if (iconSprite != null && iconSprite.gameObject != gameObject)
        {
            if (iconImage != null) iconImage.enabled = false;

            if (itemIcon != null)
            {
                iconSprite.sprite = itemIcon;
                iconSprite.enabled = true;
                iconSprite.transform.localPosition = Vector3.zero;
                iconSprite.transform.localRotation = Quaternion.identity;
                iconSprite.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            }
            else
            {
                iconSprite.enabled = false;
            }
        }

        if (selectionBorder != null)
        {
            selectionBorder.SetActive(isSelected);
        }
    }

    /// <summary>
    /// Đánh dấu ô đang được chọn hay không
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionBorder != null)
        {
            selectionBorder.SetActive(selected);
        }

        if (slotBackgroundImage != null)
        {
            slotBackgroundImage.color = selected ? new Color(1f, 1f, 1f, 1f) : new Color(0.8f, 0.8f, 0.8f, 0.75f);
        }
        if (slotBackgroundSprite != null)
        {
            slotBackgroundSprite.color = selected ? new Color(1f, 1f, 1f, 1f) : new Color(0.8f, 0.8f, 0.8f, 0.75f);
        }

        // Phóng to nhẹ 1.15x ô đang chọn để nhận biết trực quan
        transform.localScale = selected ? originalScale * 1.15f : originalScale;
    }

    /// <summary>
    /// Thực thi sử dụng Vật Phẩm trong ô này
    /// </summary>
    public void UseItem()
    {
        if (string.IsNullOrEmpty(itemName) && itemIcon == null)
        {
            return;
        }

        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player != null)
        {
            player.Heal(300);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Chỉ chọn ô khi mở túi đồ (Tránh việc click chuột tấn công làm đổi ô!)
        if (InventoryManager.Instance != null && InventoryManager.Instance.isOpen)
        {
            HandleSlotClick();
        }
    }

    private void OnMouseDown()
    {
        // Chỉ chọn ô khi mở túi đồ (Tránh việc click chuột tấn công làm đổi ô!)
        if (InventoryManager.Instance != null && InventoryManager.Instance.isOpen)
        {
            HandleSlotClick();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = isSelected ? originalScale * 1.18f : originalScale * 1.08f;
        ItemTooltipManager.ShowTooltipForItem(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = isSelected ? originalScale * 1.12f : originalScale;
        ItemTooltipManager.HideTooltip();
    }

    private void HandleSlotClick()
    {
        HotbarManager hotbar = HotbarManager.Instance;
        if (hotbar != null)
        {
            hotbar.SelectSlot(slotIndex - 1);
        }
    }

    private void OnMouseEnter()
    {
        transform.localScale = isSelected ? originalScale * 1.18f : originalScale * 1.08f;
        ItemTooltipManager.ShowTooltipForItem(this);
    }

    private void OnMouseExit()
    {
        transform.localScale = isSelected ? originalScale * 1.12f : originalScale;
        ItemTooltipManager.HideTooltip();
    }
}
