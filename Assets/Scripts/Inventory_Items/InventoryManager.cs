using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quáº£n lÃ½ Báº£ng TÃºi Äá»“ (Inventory Window) trong Game.
/// Äiá»u khiá»ƒn viá»‡c Má»Ÿ / ÄÃ³ng tÃºi Ä‘á»“ khi báº¥m vÃ o TÃºi Äá»“ trÃªn Thanh Äá»“ hoáº·c báº¥m phÃ­m B, I, Tab.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("=== INVENTORY UI ===")]
    public GameObject inventoryPanel; // Khung Báº£ng TÃºi Äá»“
    public Transform gridContainer;   // NÆ¡i chá»©a cÃ¡c Ã´ o_chua_do
    public KeyCode toggleKey = KeyCode.B; // PhÃ­m táº¯t má»Ÿ tÃºi (B / I / Tab)

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
        // ÄÃ£ gá»¡ bá» tÃ­nh nÄƒng báº­t/táº¯t Báº£ng TÃºi Äá»“ theo yÃªu cáº§u ngÆ°á»i dÃ¹ng
    }

    /// <summary>
    /// Chuyá»ƒn Ä‘á»•i tráº¡ng thÃ¡i Má»Ÿ / ÄÃ³ng TÃºi Äá»“
    /// </summary>
    public void ToggleInventory()
    {
        CloseInventory();
    }

    [Header("=== INVENTORY SLOTS ===")]
    public List<ItemSlot> inventorySlots = new List<ItemSlot>();

    /// <summary>
    /// Äá»“ng bá»™ vÃ  cÃ i Ä‘áº·t kháº£ nÄƒng click chuá»™t kÃ©o tháº£ chuáº©n 100% cho Táº¤T Cáº¢ cÃ¡c Ã´ chá»©a Ä‘á»“ trong Báº£ng TÃºi Äá»“ khi má»Ÿ TÃºi Äá»“
    /// </summary>
    public void SetupInventorySlots()
    {
        inventorySlots.Clear();

        // 1. Táº¯t raycastTarget cá»§a táº¥t cáº£ cÃ¡c táº¥m áº£nh phÃ´ng ná»n xung quanh (trÃ¡nh cáº£n trá»Ÿ click chuá»™t vÃ o cÃ¡c Ã´ chá»©a Ä‘á»“)
        if (inventoryPanel != null)
        {
            Image[] panelImages = inventoryPanel.GetComponentsInChildren<Image>(true);
            foreach (Image img in panelImages)
            {
                ItemSlot slot = img.GetComponent<ItemSlot>() ?? img.GetComponentInParent<ItemSlot>();
                if (slot == null && img.gameObject != inventoryPanel)
                {
                    img.raycastTarget = false; // PhÃ´ng ná»n KHÃ”NG cáº£n click
                }
            }
        }

        // 2. Tá»± Ä‘á»™ng kiá»ƒm tra vÃ  gáº¯n ItemSlot cho táº¥t cáº£ cÃ¡c Ã´ con/chÃ¡u trong Báº£ng TÃºi Äá»“
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

        // 3. ÄÄƒng kÃ½ táº¥t cáº£ cÃ¡c Ã´ ItemSlot trong Scene (bao gá»“m cáº£ Ã´ Ä‘ang áº©n)
        ItemSlot[] allSlots = Object.FindObjectsByType<ItemSlot>(FindObjectsInactive.Include);
        foreach (ItemSlot slot in allSlots)
        {
            if (slot != null)
            {
                slot.EnsureIconObject();

                Image bgImg = slot.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.raycastTarget = true; // Ã” chá»©a Ä‘á»“ Báº®T BUá»˜C nháº­n click kÃ©o tháº£
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

        Debug.Log($"<color=green><b>[InventoryManager] ÄÃ£ Ä‘á»“ng bá»™ thÃ nh cÃ´ng {inventorySlots.Count} Ã´ chá»©a Ä‘á»“ cho Báº£ng TÃºi Äá»“!</b></color>");
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
    /// Má»Ÿ Báº£ng TÃºi Äá»“
    /// </summary>
    public void OpenInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            isOpen = true;
            SetupInventorySlots();
            Debug.Log("<color=green><b>[InventoryManager] ÄÃ£ má»Ÿ Báº£ng TÃºi Äá»“ & Äá»“ng bá»™ cÃ¡c Ã´ chá»©a!</b></color>");
        }
        else
        {
            Debug.LogWarning("[InventoryManager] ChÆ°a gÃ¡n inventoryPanel trong Inspector!");
        }
    }

    /// <summary>
    /// ÄÃ³ng Báº£ng TÃºi Äá»“
    /// </summary>
    public void CloseInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isOpen = false;
            Debug.Log("<color=yellow><b>[InventoryManager] ÄÃ£ Ä‘Ã³ng Báº£ng TÃºi Äá»“.</b></color>");
        }
    }
}
