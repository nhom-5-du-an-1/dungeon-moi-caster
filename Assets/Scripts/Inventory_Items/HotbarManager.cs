using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quáº£n lÃ½ Thanh Äá»“ (Hotbar / Quick Slot Bar) trong Game.
/// PhÃ­m 1 -> 5 tÆ°Æ¡ng á»©ng chá»n tá»« Ã” 1 -> Ã” 5.
/// Sá»­ dá»¥ng váº­t pháº©m báº±ng phÃ­m E, F hoáº·c Space.
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
    /// Tá»± Ä‘á»™ng Ä‘Æ°a khung HotbarPanel xuá»‘ng chÃ­nh giá»¯a mÃ©p dÆ°á»›i mÃ n hÃ¬nh & XÃ³a phÃ´ng má» tráº¯ng Panel
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
    /// Tá»± Ä‘á»™ng tÃ¬m kiáº¿m chuáº©n 5 Ã´ chá»©a Ä‘á»“ trong HotbarPanel
    /// </summary>
    public void FindAndSetupSlots()
    {
        if (slots.Count == 0)
        {
            // TÃ¬m cÃ¡c Ã´ con náº±m trong HotbarPanel
            GameObject panel = GameObject.Find("HotbarPanel");
            if (panel != null)
            {
                ItemSlot[] panelSlots = panel.GetComponentsInChildren<ItemSlot>(true);
                if (panelSlots != null && panelSlots.Length > 0)
                {
                    slots.AddRange(panelSlots);
                }
            }

            // Náº¿u khÃ´ng tÃ¬m tháº¥y thÃ¬ tÃ¬m toÃ n Scene
            if (slots.Count == 0)
            {
                ItemSlot[] foundSlots = FindObjectsByType<ItemSlot>();
                if (foundSlots != null && foundSlots.Length > 0)
                {
                    slots.AddRange(foundSlots);
                }
            }
        }

        // Sáº¯p xáº¿p theo thá»© tá»± hiá»ƒn thá»‹ tá»« trÃ¡i sang pháº£i trong Hierarchy
        slots.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        // ÄÃ¡nh sá»‘ thá»© tá»± slot tá»« 1 Ä‘áº¿n N
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                slots[i].slotIndex = i + 1;
            }
        }
    }

    /// <summary>
    /// Chá»n Ã´ active hiá»‡n táº¡i (index 0 = Slot 1, index 1 = Slot 2, ...)
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
            // Ã” Ä‘ang chá»n TRá»NG KHÃ”NG CÃ“ VÅ¨ KHÃ -> Cáº¥t rÃ¬u ngay láº­p tá»©c!
            if (WeaponController.Instance != null)
            {
                WeaponController.Instance.UnequipWeapon();
            }
        }

        Debug.Log($"<color=cyan>[HotbarManager] ÄÃ£ chá»n Ã” sá»‘ {currentSelectedIndex + 1}</color>");
    }

    /// <summary>
    /// Sá»­ dá»¥ng váº­t pháº©m á»Ÿ Ã´ Ä‘ang chá»n hiá»‡n táº¡i
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
    /// Chá»‰ cho phÃ©p chuyá»ƒn Ã´ báº±ng cÃ¡c phÃ­m sá»‘ tá»« 1 Ä‘áº¿n 5 (KhÃ³a hoÃ n toÃ n cuá»™n chuá»™t hoáº·c click linh tinh)
    /// </summary>
    private void HandleKeyboardInputs()
    {
        // PhÃ­m 1 -> chá»n Ã” 1 (Slot 1)
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SelectSlot(0);
        // PhÃ­m 2 -> chá»n Ã” 2 (Slot 2)
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SelectSlot(1);
        // PhÃ­m 3 -> chá»n Ã” 3 (Slot 3)
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SelectSlot(2);
        // PhÃ­m 4 -> chá»n Ã” 4 (Slot 4)
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SelectSlot(3);
        // PhÃ­m 5 -> chá»n Ã” 5 (Slot 5)
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) SelectSlot(4);

        // Báº¥m phÃ­m E, F hoáº·c Space Ä‘á»ƒ sá»­ dá»¥ng váº­t pháº©m tiÃªu hao trong Ã´ Ä‘ang chá»n
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space))
        {
            UseActiveItem();
        }
    }
}
