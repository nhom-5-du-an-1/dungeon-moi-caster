using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Thanh Đồ (Hotbar / Quick Slot Bar) trong Game.
/// Phím 1 -> 5 tương ứng chọn từ Ô 1 -> Ô 5.
/// Sử dụng vật phẩm bằng phím E, F hoặc Space.
/// </summary>
public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance { get; private set; }

    [Header("=== HOTBAR SLOTS ===")]
    public List<ItemSlot> slots = new List<ItemSlot>();
    public int currentSelectedIndex = 0;

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
    }

    void Start()
    {
        FindAndSetupSlots();
        PositionHotbarAtScreenBottom();
        SelectSlot(0);
    }

    /// <summary>
    /// Tự động đưa khung HotbarPanel xuống chính giữa mép dưới màn hình & Xóa phông mờ trắng Panel
    /// </summary>
    public void PositionHotbarAtScreenBottom()
    {
        if (slots.Count > 0 && slots[0] != null)
        {
            Transform parentTrans = slots[0].transform.parent;
            if (parentTrans != null)
            {
                Image panelImg = parentTrans.GetComponent<Image>();
                if (panelImg != null && panelImg.color.a > 0f)
                {
                    panelImg.enabled = false;
                }

                RectTransform panelRect = parentTrans.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    panelRect.anchorMin = new Vector2(0.5f, 0f);
                    panelRect.anchorMax = new Vector2(0.5f, 0f);
                    panelRect.pivot = new Vector2(0.5f, 0f);
                    panelRect.sizeDelta = new Vector2(450f, 90f);
                    panelRect.anchoredPosition = new Vector2(0f, 35f);
                }
            }
        }
    }

    void Update()
    {
        HandleKeyboardInputs();
    }

    /// <summary>
    /// Tự động tìm kiếm chuẩn 5 ô chứa đồ trong HotbarPanel
    /// </summary>
    public void FindAndSetupSlots()
    {
        if (slots.Count == 0)
        {
            // Tìm các ô con nằm trong HotbarPanel
            GameObject panel = GameObject.Find("HotbarPanel");
            if (panel != null)
            {
                ItemSlot[] panelSlots = panel.GetComponentsInChildren<ItemSlot>(true);
                if (panelSlots != null && panelSlots.Length > 0)
                {
                    slots.AddRange(panelSlots);
                }
            }

            // Nếu không tìm thấy thì tìm toàn Scene
            if (slots.Count == 0)
            {
                ItemSlot[] foundSlots = FindObjectsByType<ItemSlot>(FindObjectsSortMode.InstanceID);
                if (foundSlots != null && foundSlots.Length > 0)
                {
                    slots.AddRange(foundSlots);
                }
            }
        }

        // Sắp xếp theo thứ tự hiển thị từ trái sang phải trong Hierarchy
        slots.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        // Đánh số thứ tự slot từ 1 đến N
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                slots[i].slotIndex = i + 1;
            }
        }
    }

    /// <summary>
    /// Chọn ô active hiện tại (index 0 = Slot 1, index 1 = Slot 2, ...)
    /// </summary>
    public void SelectSlot(int index)
    {
        if (slots.Count == 0) return;

        currentSelectedIndex = Mathf.Clamp(index, 0, slots.Count - 1);

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                slots[i].SetSelected(i == currentSelectedIndex);
            }
        }

        ItemSlot selectedSlot = slots[currentSelectedIndex];
        if (selectedSlot != null && selectedSlot.itemIcon != null && !string.IsNullOrEmpty(selectedSlot.itemName))
        {
            if (WeaponController.Instance != null)
            {
                WeaponController.Instance.EquipAxe(selectedSlot.itemIcon, selectedSlot.itemName);
            }
        }
        else
        {
            // Ô đang chọn TRỐNG KHÔNG CÓ VŨ KHÍ -> Cất rìu ngay lập tức!
            if (WeaponController.Instance != null)
            {
                WeaponController.Instance.UnequipWeapon();
            }
        }

        Debug.Log($"<color=cyan>[HotbarManager] Đã chọn Ô số {currentSelectedIndex + 1}</color>");
    }

    /// <summary>
    /// Sử dụng vật phẩm ở ô đang chọn hiện tại
    /// </summary>
    public void UseActiveItem()
    {
        if (slots.Count == 0) return;

        if (currentSelectedIndex >= 0 && currentSelectedIndex < slots.Count)
        {
            ItemSlot activeSlot = slots[currentSelectedIndex];
            if (activeSlot != null)
            {
                activeSlot.UseItem();
            }
        }
    }

    /// <summary>
    /// Chỉ cho phép chuyển ô bằng các phím số từ 1 đến 5 (Khóa hoàn toàn cuộn chuột hoặc click linh tinh)
    /// </summary>
    private void HandleKeyboardInputs()
    {
        // Phím 1 -> chọn Ô 1 (Slot 1)
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SelectSlot(0);
        // Phím 2 -> chọn Ô 2 (Slot 2)
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SelectSlot(1);
        // Phím 3 -> chọn Ô 3 (Slot 3)
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SelectSlot(2);
        // Phím 4 -> chọn Ô 4 (Slot 4)
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SelectSlot(3);
        // Phím 5 -> chọn Ô 5 (Slot 5)
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) SelectSlot(4);

        // Bấm phím E, F hoặc Space để sử dụng vật phẩm tiêu hao trong ô đang chọn
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space))
        {
            UseActiveItem();
        }
    }
}
