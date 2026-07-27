using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.0f;
    private Rigidbody2D rb;
    private Vector2 moveInput;

    [Header("Camera Follow")]
    public bool enableCameraFollow = true;
    public float cameraSmoothSpeed = 0.125f;
    public Vector3 cameraOffset = new Vector3(0f, 0f, -10f);

    private Camera mainCamera;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
    }

    void Update()
    {
        // Read simple horizontal and vertical axis input
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // Normalize vector to maintain consistent movement speed diagonally
        if (moveInput.sqrMagnitude > 1)
        {
            moveInput.Normalize();
        }

        // Depth sorting for player during movement
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = -Mathf.RoundToInt(transform.position.y * 100);
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
    }

    void LateUpdate()
    {
        if (enableCameraFollow && mainCamera != null)
        {
            Vector3 targetPosition = transform.position + cameraOffset;
            Vector3 smoothedPosition = Vector3.Lerp(mainCamera.transform.position, targetPosition, cameraSmoothSpeed);
            mainCamera.transform.position = smoothedPosition;
        }
    }
}
