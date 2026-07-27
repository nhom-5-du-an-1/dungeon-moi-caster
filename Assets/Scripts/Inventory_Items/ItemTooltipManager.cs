using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Khung Hiển Thị Chỉ Số (Tooltip Manager) cho Vật Phẩm trong Game.
/// Khi rê con trỏ chuột vào ô chứa đồ (Hotbar / Túi đồ), bảng Tooltip sẽ hiện lên
/// hiển thị chi tiết: Tên, Sát thương, Tốc độ đánh, Mô tả, Kỹ năng & Nội tại!
/// </summary>
public class ItemTooltipManager : MonoBehaviour
{
    public static ItemTooltipManager Instance { get; private set; }

    [Header("=== TOOLTIP UI CONTAINER ===")]
    private Canvas tooltipCanvas;
    private GameObject tooltipPanel;
    private RectTransform panelRect;
    private Image bgImage;
    private Outline bgOutline;

    [Header("=== TEXT & CARD FIELDS ===")]
    private Image cardImageComponent;
    private GameObject dividerGO;
    private Text titleText;
    private Text typeAndStatsText;
    private Text descriptionText;
    private Text skillText;

    private bool isShowing = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        CreateTooltipUI();
    }

    void Update()
    {
        if (isShowing && tooltipPanel != null)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// Tự động khởi tạo Canvas Overlay và Bảng UI Tooltip chuẩn phong cách Dark Fantasy
    /// </summary>
    private void CreateTooltipUI()
    {
        if (tooltipPanel != null) return;

        // 1. Tạo Canvas Overlay hiển thị trên cùng (Sorting Order 10000)
        GameObject canvasGO = new GameObject("[ItemTooltipCanvas]");
        DontDestroyOnLoad(canvasGO);
        tooltipCanvas = canvasGO.AddComponent<Canvas>();
        tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tooltipCanvas.sortingOrder = 10000;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 2. Khung Bảng Tooltip (Panel)
        tooltipPanel = new GameObject("TooltipPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        tooltipPanel.transform.SetParent(canvasGO.transform, false);

        panelRect = tooltipPanel.GetComponent<RectTransform>();
        panelRect.pivot = new Vector2(0f, 1f); // Anchor Top-Left theo con trỏ chuột
        panelRect.sizeDelta = new Vector2(280f, 0f);

        bgImage = tooltipPanel.GetComponent<Image>();
        bgImage.color = new Color(0.08f, 0.09f, 0.14f, 0.94f); // Phông tối thủy tinh mờ
        bgImage.raycastTarget = false;

        bgOutline = tooltipPanel.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0.9f, 0.7f, 0.2f, 0.8f); // Viền vàng ánh kim
        bgOutline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup layout = tooltipPanel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = tooltipPanel.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // --- SECTION 0: BẢNG ẢNH MINECRAFT CARD ---
        GameObject cardGO = new GameObject("CardImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardGO.transform.SetParent(tooltipPanel.transform, false);
        cardImageComponent = cardGO.GetComponent<Image>();
        cardImageComponent.raycastTarget = false;
        cardImageComponent.preserveAspect = true;
        cardGO.SetActive(false);

        // --- SECTION 1: TÊN VẬT PHẨM ---
        titleText = CreateTextObject("TitleText", tooltipPanel.transform, defaultFont, 16, FontStyle.Bold, Color.gold);

        // --- SECTION 2: THÔNG SỐ VŨ KHÍ ---
        typeAndStatsText = CreateTextObject("TypeAndStatsText", tooltipPanel.transform, defaultFont, 13, FontStyle.Normal, new Color(0.85f, 0.9f, 1.0f));

        // --- VẠCH NGĂN NHAU (DIVIDER LINE) ---
        dividerGO = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dividerGO.transform.SetParent(tooltipPanel.transform, false);
        dividerGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 2f);
        dividerGO.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f, 0.5f);

        // --- SECTION 3: MÔ TẢ VẬT PHẨM ---
        descriptionText = CreateTextObject("DescriptionText", tooltipPanel.transform, defaultFont, 12, FontStyle.Italic, new Color(0.8f, 0.8f, 0.85f));

        // --- SECTION 4: NỘI TẠI & KỸ NĂNG ---
        skillText = CreateTextObject("SkillText", tooltipPanel.transform, defaultFont, 12, FontStyle.Bold, new Color(0.4f, 0.9f, 1.0f));

        tooltipPanel.SetActive(false);
    }

    private Text CreateTextObject(string name, Transform parent, Font font, int size, FontStyle style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);

        Text txt = go.GetComponent<Text>();
        txt.font = font;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = color;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        return txt;
    }

    /// <summary>
    /// Bật Tooltip hiển thị chỉ số của Vật phẩm từ ItemSlot
    /// </summary>
    public static void ShowTooltipForItem(ItemSlot slot)
    {
        if (slot == null || (string.IsNullOrEmpty(slot.itemName) && slot.itemIcon == null))
        {
            HideTooltip();
            return;
        }

        if (Instance == null)
        {
            GameObject managerGO = new GameObject("[ItemTooltipManager]");
            Instance = managerGO.AddComponent<ItemTooltipManager>();
        }

        Instance.DisplayTooltip(slot.itemName, slot.itemData, slot.itemIcon);
    }

    /// <summary>
    /// Hiển thị chi tiết nội dung Tooltip
    /// </summary>
    public void DisplayTooltip(string name, ItemData data, Sprite icon = null)
    {
        if (tooltipPanel == null) CreateTooltipUI();

        // ƯU TIÊN HIỂN THỊ BẢNG THÔNG TIN ẢNH MINECRAFT NẾU CÓ
        if (data != null && data.infoCardSprite != null)
        {
            if (bgImage != null) bgImage.color = Color.clear;
            if (bgOutline != null) bgOutline.enabled = false;

            if (titleText != null) titleText.gameObject.SetActive(false);
            if (typeAndStatsText != null) typeAndStatsText.gameObject.SetActive(false);
            if (dividerGO != null) dividerGO.SetActive(false);
            if (descriptionText != null) descriptionText.gameObject.SetActive(false);
            if (skillText != null) skillText.gameObject.SetActive(false);

            cardImageComponent.sprite = data.infoCardSprite;
            cardImageComponent.gameObject.SetActive(true);

            // Cấu hình Kích thước TO RÕ (Width = 460px) cho dễ đọc chữ
            RectTransform cardRect = cardImageComponent.rectTransform;
            float originalW = data.infoCardSprite.rect.width > 0 ? data.infoCardSprite.rect.width : 1024f;
            float originalH = data.infoCardSprite.rect.height > 0 ? data.infoCardSprite.rect.height : 512f;
            float targetW = 460f;
            float targetH = (originalH / originalW) * targetW;

            cardRect.sizeDelta = new Vector2(targetW, targetH);
            panelRect.sizeDelta = new Vector2(targetW, targetH);

            tooltipPanel.SetActive(true);
            isShowing = true;
            UpdatePosition();
            return;
        }

        // FALLBACK BẢNG TEXT TRUYỀN THỐNG
        if (cardImageComponent != null) cardImageComponent.gameObject.SetActive(false);
        if (titleText != null) titleText.gameObject.SetActive(true);
        if (typeAndStatsText != null) typeAndStatsText.gameObject.SetActive(true);
        if (dividerGO != null) dividerGO.SetActive(true);
        if (descriptionText != null) descriptionText.gameObject.SetActive(true);
        if (skillText != null) skillText.gameObject.SetActive(true);

        if (bgImage != null) bgImage.color = new Color(0.08f, 0.09f, 0.14f, 0.94f);
        if (bgOutline != null) bgOutline.enabled = true;

        string lowerName = (name ?? "").ToLower();

        // 1. Tên & Màu sắc theo Hệ
        Color headerColor = Color.white;
        Color borderCol = new Color(0.8f, 0.8f, 0.8f, 0.8f);

        if (lowerName.Contains("lửa") || lowerName.Contains("lua") || lowerName.Contains("fire"))
        {
            headerColor = new Color(1.0f, 0.5f, 0.1f);
            borderCol = new Color(1.0f, 0.4f, 0.0f, 0.9f);
        }
        else if (lowerName.Contains("băng") || lowerName.Contains("bang") || lowerName.Contains("frost") || lowerName.Contains("ice"))
        {
            headerColor = new Color(0.3f, 0.85f, 1.0f);
            borderCol = new Color(0.2f, 0.7f, 1.0f, 0.9f);
        }
        else
        {
            headerColor = new Color(1.0f, 0.85f, 0.3f);
            borderCol = new Color(0.9f, 0.75f, 0.2f, 0.9f);
        }

        titleText.text = string.IsNullOrEmpty(name) ? (data != null ? data.itemName : "Vật Phẩm") : name;
        titleText.color = headerColor;
        bgOutline.effectColor = borderCol;

        // 2. Chỉ số Sát thương & Tốc độ
        string damageRange = (lowerName.Contains("băng") || lowerName.Contains("bang") || lowerName.Contains("frost") || lowerName.Contains("ice")) ? "10 - 20" : "11 - 22";
        float speed = data != null ? data.attackSpeed : (lowerName.Contains("băng") || lowerName.Contains("bang") ? 0.30f : 0.35f);
        float range = data != null ? data.attackRange : 1.3f;

        typeAndStatsText.text = $"<b>Loại:</b> Vũ Khí Cận Chiến\n" +
                                $"⚔️ <b>Sát thương:</b> <color=#FFD700>{damageRange}</color> HP\n" +
                                $"⚡ <b>Tốc độ đánh:</b> {speed:F2}s\n" +
                                $"📏 <b>Tầm vung:</b> {range:F1}m";

        // 3. Mô tả
        if (data != null && !string.IsNullOrEmpty(data.description))
        {
            descriptionText.text = $"<i>\"{data.description}\"</i>";
        }
        else if (lowerName.Contains("băng") || lowerName.Contains("bang"))
        {
            descriptionText.text = "<i>\"Rìu Băng Vĩnh Cửu đúc từ băng tuyết ngàn năm. Mỗi cú chém đóng băng quái vật và tạo hiệu ứng Vỡ Băng chí mạng!\"</i>";
        }
        else if (lowerName.Contains("lửa") || lowerName.Contains("lua"))
        {
            descriptionText.text = "<i>\"Rìu Lửa Thần Thoại thiêu rụi bóng tối, thiêu đốt quái vật liên tục sau mỗi cú vung chém!\"</i>";
        }
        else
        {
            descriptionText.text = "<i>\"Trang bị mạnh mẽ giúp tăng cường khả năng chiến đấu.\"</i>";
        }

        // 4. Kỹ Năng & Nội Tại
        if (lowerName.Contains("băng") || lowerName.Contains("bang"))
        {
            skillText.text = "<color=#00E5FF><b>❄️ NỘI TẠI - BĂNG GIÁ & VỠ BĂNG:</b></color>\n" +
                             "• Chém trúng đóng băng tê liệt quái 2.0s!\n" +
                             "• Chém tiếp khi quái bị Đóng Băng $\\rightarrow$ <color=#FFD700>💥 VỠ BĂNG (+40 Damage)</color>\n\n" +
                             "<color=#00E5FF><b>❄️ KỸ NĂNG (Q / Chuột Phải):</b></color>\n" +
                             "• <i>Sóng Băng Tuyệt Đối</i>: Đóng băng toàn bộ quái 3.5s + 80 Sát Thương!";
        }
        else if (lowerName.Contains("lửa") || lowerName.Contains("lua"))
        {
            skillText.text = "<color=#FF6600><b>🔥 NỘI TẠI - HỎA THIÊU:</b></color>\n" +
                             "• Chém trúng thiêu đốt quái trừ 25 HP / 2.5s!\n\n" +
                             "<color=#FF6600><b>🔥 KỸ NĂNG (Q / Chuột Phải):</b></color>\n" +
                             "• <i>Bão Lửa Thiêu Rụi</i>: Quét bão lửa xung quanh gây 65 Sát Thương!";
        }
        else
        {
            skillText.text = data != null && !string.IsNullOrEmpty(data.skillName) ?
                $"<b>✨ Kỹ năng:</b> {data.skillName}\n{data.skillDescription}" : "";
        }

        tooltipPanel.SetActive(true);
        isShowing = true;
        UpdatePosition();
    }

    /// <summary>
    /// Cập nhật vị trí Tooltip bám sát ngay phía trên ô đồ được di chuột vào (Canvas Local Coordinates)
    /// </summary>
    private void UpdatePosition()
    {
        if (tooltipCanvas == null || panelRect == null) return;

        // Chuyển tọa độ chuột (Screen space) sang Canvas local space
        RectTransform canvasRect = tooltipCanvas.transform as RectTransform;
        Vector2 mousePos = Input.mousePosition;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, null, out localPoint);

        // Đặt Pivot ở chính giữa đáy bảng để nảy ngay trên con trỏ chuột
        panelRect.pivot = new Vector2(0.5f, 0f);

        float cardW = panelRect.rect.width > 0 ? panelRect.rect.width : 460f;
        float cardH = panelRect.rect.height > 0 ? panelRect.rect.height : 200f;

        Vector2 targetPos = localPoint + new Vector2(0f, 35f);

        // Giới hạn để bảng không bị tràn ra 4 viền màn hình
        float minX = -canvasRect.rect.width * 0.5f + cardW * 0.5f + 10f;
        float maxX = canvasRect.rect.width * 0.5f - cardW * 0.5f - 10f;
        float maxY = canvasRect.rect.height * 0.5f - cardH - 10f;

        if (targetPos.x < minX) targetPos.x = minX;
        if (targetPos.x > maxX) targetPos.x = maxX;

        // Nếu chạm sát mép trên màn hình ➔ Lật bảng xuống dưới con trỏ chuột
        if (targetPos.y > maxY)
        {
            panelRect.pivot = new Vector2(0.5f, 1f);
            targetPos.y = localPoint.y - 35f;
        }

        panelRect.anchoredPosition = targetPos;
    }

    /// <summary>
    /// Ẩn Tooltip
    /// </summary>
    public static void HideTooltip()
    {
        if (Instance != null && Instance.tooltipPanel != null)
        {
            Instance.tooltipPanel.SetActive(false);
            Instance.isShowing = false;
        }
    }
}
