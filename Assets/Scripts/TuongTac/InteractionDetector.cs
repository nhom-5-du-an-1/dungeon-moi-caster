using UnityEngine;

public class InteractionDetector : MonoBehaviour
{
    private IInteractable interactableInRange = null; // Vật phẩm/NPC ở gần
    public GameObject interactionIcon; // Biểu tượng gợi ý tương tác (Ví dụ: Chữ "E")
    public KeyCode interactKey = KeyCode.E; // Nút bấm tương tác

    private void Start()
    {
        if (interactionIcon != null)
            interactionIcon.SetActive(false);
    }

    private void Update()
    {
        // Kiểm tra nếu người chơi ấn nút và đang có mục tiêu để tương tác
        if (Input.GetKeyDown(interactKey) && interactableInRange != null)
        {
            // Kiểm tra lại một lần nữa xem mục tiêu hiện tại còn tương tác được không
            if (interactableInRange.CanInteract())
            {
                interactableInRange.Interact();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IInteractable interactable) &&
            interactable.CanInteract())
        {
            interactableInRange = interactable;

            if (interactionIcon != null)
                interactionIcon.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IInteractable interactable) &&
            interactable == interactableInRange)
        {
            interactableInRange = null;

            if (interactionIcon != null)
                interactionIcon.SetActive(false);
        }
    }
}
