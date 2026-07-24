using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("=== UI COMPONENTS ===")]
    [Tooltip("The main Panel GameObject containing the inventory UI")]
    public GameObject inventoryPanel;

    [Tooltip("Whether to freeze player movement and attacks when the inventory is open")]
    public bool freezePlayerOnOpen = true;

    [Header("=== STATE ===")]
    [SerializeField] private bool isOpen = false;

    private PlayerMovement playerMovement;

    void Start()
    {
        // Automatically find the player in the scene
        FindPlayer();

        // Ensure the inventory panel is in the correct initial state (closed)
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isOpen);
        }
        else
        {
            Debug.LogWarning("[InventoryManager] Inventory Panel is not assigned! Searching for a child named 'InventoryPanel'...");
            Transform childPanel = transform.Find("InventoryPanel");
            if (childPanel != null)
            {
                inventoryPanel = childPanel.gameObject;
                inventoryPanel.SetActive(isOpen);
            }
        }
    }

    void Update()
    {
        // Toggle inventory when E is pressed
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleInventory();
        }
    }

    /// <summary>
    /// Find the player object in the scene
    /// </summary>
    private void FindPlayer()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
    }

    /// <summary>
    /// Toggle the active state of the inventory panel
    /// </summary>
    public void ToggleInventory()
    {
        SetInventoryActive(!isOpen);
    }

    /// <summary>
    /// Open the inventory panel
    /// </summary>
    public void OpenInventory()
    {
        SetInventoryActive(true);
    }

    /// <summary>
    /// Close the inventory panel (can be called by the 'X' button)
    /// </summary>
    public void CloseInventory()
    {
        SetInventoryActive(false);
    }

    /// <summary>
    /// Set the active state and update player freeze state
    /// </summary>
    public void SetInventoryActive(bool active)
    {
        isOpen = active;

        if (inventoryPanel != null)
        {
            UIWindowAnimator windowAnim = inventoryPanel.GetComponent<UIWindowAnimator>();
            if (active)
            {
                inventoryPanel.SetActive(true);
            }
            else
            {
                if (windowAnim != null && inventoryPanel.activeSelf)
                {
                    windowAnim.CloseWindow(() => {
                        inventoryPanel.SetActive(false);
                    });
                }
                else
                {
                    inventoryPanel.SetActive(false);
                }
            }
        }

        // Handle freezing player movement and action
        if (freezePlayerOnOpen)
        {
            if (playerMovement == null) FindPlayer();

            if (playerMovement != null)
            {
                playerMovement.enabled = !isOpen;
                
                // Reset velocity when opening inventory so they don't slide
                if (isOpen)
                {
                    Rigidbody2D rb = playerMovement.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero;
                    }
                    
                    // Reset Animator speed parameter to idle state
                    Animator anim = playerMovement.GetComponent<Animator>();
                    if (anim != null)
                    {
                        anim.SetFloat("Speed", 0f);
                    }
                }
            }
        }
    }

    public bool IsInventoryOpen()
    {
        return isOpen;
    }
}
