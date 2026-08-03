using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện Trái tim Minecraft của Player.
/// Lưu vị trí VĨNH VIỄN cả trong Editor Scene lẫn trong Game (Play Mode).
/// Hỗ trợ Kéo thả trực tiếp bằng Chuột và xem trước thời gian thực.
/// </summary>
[ExecuteAlways]
public class PlayerHeartUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    [Header("=== HEART SPRITES ===")]
    public Sprite fullHeartSprite;
    public Sprite halfHeartSprite;
    public Sprite emptyHeartSprite;

    [Header("=== SETTINGS ===")]
    [Tooltip("Số tim tối đa hiển thị (mặc định 10 tim = 20 HP)")]
    public int maxHearts = 10;
    
    [Tooltip("Kích thước mỗi tim trên Canvas (Pixel)")]
    public Vector2 heartSize = new Vector2(32f, 32f);

    [Tooltip("Khoảng cách giữa các tim")]
    public float heartSpacing = 4f;

    [Tooltip("Căn lề của hàng tim (Trái, Giữa, Phải)")]
    public TextAnchor layoutAlignment = TextAnchor.MiddleLeft;

    [Header("=== KÉO THẢ & LƯU VỊ TRÍ (POSITION SAVING) ===")]
    [Tooltip("Bật/tắt cho phép dùng chuột giữ và kéo vị trí thanh tim trong game")]
    public bool isDraggable = true;

    [Tooltip("Tự động lưu vị trí kéo thả khi chơi game")]
    public bool savePositionAcrossSessions = true;

    [SerializeField, HideInInspector]
    public Vector2 savedAnchoredPosition = new Vector2(20f, -20f);

    [Range(0.2f, 3.0f)]
    [Tooltip("Tỷ lệ thu phóng giao diện (Scale)")]
    public float uiScale = 1.0f;

    private const string PREF_KEY_POS_X = "PlayerHeartUI_PosX";
    private const string PREF_KEY_POS_Y = "PlayerHeartUI_PosY";
    private const string PREF_KEY_SAVED = "PlayerHeartUI_HasSavedPos";

    [Header("=== REFERENCES ===")]
    public PlayerStats playerStats;
    public Transform heartsContainer;

    private List<Image> heartImages = new List<Image>();
    private Coroutine shakeCoroutine;
    private int lastHealth = -1;

    private RectTransform myRectTransform;
    private Canvas rootCanvas;
    private float lastScale = 1.0f;

    private void Awake()
    {
        myRectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();

        if (heartsContainer == null)
        {
            heartsContainer = transform;
        }

        // Tải vị trí & kích thước đã lưu
        uiScale = PlayerPrefs.GetFloat("PlayerHeartUI_Scale", 1.0f);
        transform.localScale = new Vector3(uiScale, uiScale, 1.0f);
        lastScale = uiScale;
        
        ApplySavedPosition();

        // Đảm bảo có Image trong suốt trên chính nó để hứng sự kiện Click/Drag chuột
        Image myImage = GetComponent<Image>();
        if (myImage == null)
        {
            myImage = gameObject.AddComponent<Image>();
            myImage.color = new Color(0f, 0f, 0f, 0f); // trong suốt hoàn toàn
            myImage.raycastTarget = true;
        }
        else
        {
            myImage.color = new Color(0f, 0f, 0f, 0f);
            myImage.raycastTarget = true;
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            // Tự động đồng bộ vị trí khi bạn kéo thả trong Editor Scene View
            if (myRectTransform == null) myRectTransform = GetComponent<RectTransform>();
            if (myRectTransform != null && myRectTransform.anchoredPosition != savedAnchoredPosition)
            {
                savedAnchoredPosition = myRectTransform.anchoredPosition;
                PlayerPrefs.SetFloat(PREF_KEY_POS_X, savedAnchoredPosition.x);
                PlayerPrefs.SetFloat(PREF_KEY_POS_Y, savedAnchoredPosition.y);
                PlayerPrefs.SetInt(PREF_KEY_SAVED, 1);
                PlayerPrefs.Save();
            }

            // Tự động đồng bộ tỷ lệ Scale trong Editor (Hai chiều)
            if (transform.localScale.x != lastScale)
            {
                uiScale = transform.localScale.x;
                lastScale = uiScale;
                PlayerPrefs.SetFloat("PlayerHeartUI_Scale", uiScale);
                PlayerPrefs.Save();
            }
            else if (uiScale != lastScale)
            {
                transform.localScale = new Vector3(uiScale, uiScale, 1.0f);
                lastScale = uiScale;
                PlayerPrefs.SetFloat("PlayerHeartUI_Scale", uiScale);
                PlayerPrefs.Save();
            }
        }
        else
        {
            // Trong game, áp dụng scale nếu thay đổi
            if (transform.localScale.x != uiScale)
            {
                transform.localScale = new Vector3(uiScale, uiScale, 1.0f);
            }
        }
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            PlayerStats.OnAnyPlayerHealthChanged -= UpdateHeartsUI;
            PlayerStats.OnAnyPlayerHealthChanged += UpdateHeartsUI;
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            PlayerStats.OnAnyPlayerHealthChanged -= UpdateHeartsUI;
        }
    }

    private void Start()
    {
        LoadSpritesIfNull();

        if (Application.isPlaying)
        {
            PlayerStats.OnAnyPlayerHealthChanged -= UpdateHeartsUI;
            PlayerStats.OnAnyPlayerHealthChanged += UpdateHeartsUI;

            PlayerStats sceneStats = FindFirstObjectByType<PlayerStats>();
            if (sceneStats != null)
            {
                playerStats = sceneStats;
            }

            if (playerStats != null)
            {
                playerStats.OnHealthChanged -= UpdateHeartsUI;
                playerStats.OnHealthChanged += UpdateHeartsUI;
                SetupHeartGrid();
                UpdateHeartsUI(playerStats.currentHealth, playerStats.maxHealth);
            }
            else
            {
                SetupHeartGrid();
                UpdateHeartsUI(maxHearts * 2, maxHearts * 2);
            }
        }
        else
        {
            SetupHeartGrid();
            UpdateHeartsUI(maxHearts * 2, maxHearts * 2);
        }
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            PlayerStats.OnAnyPlayerHealthChanged -= UpdateHeartsUI;
            if (playerStats != null)
            {
                playerStats.OnHealthChanged -= UpdateHeartsUI;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        LoadSpritesIfNull();
    }
#endif

    public void ApplySavedPosition()
    {
        if (myRectTransform == null) myRectTransform = GetComponent<RectTransform>();
        if (myRectTransform == null) return;

        // Chỉ áp dụng vị trí từ PlayerPrefs nếu người dùng bật lưu vị trí kéo thả trong khi chơi game
        if (Application.isPlaying && savePositionAcrossSessions && PlayerPrefs.HasKey(PREF_KEY_SAVED))
        {
            float posX = PlayerPrefs.GetFloat(PREF_KEY_POS_X, myRectTransform.anchoredPosition.x);
            float posY = PlayerPrefs.GetFloat(PREF_KEY_POS_Y, myRectTransform.anchoredPosition.y);
            myRectTransform.anchoredPosition = new Vector2(posX, posY);
        }
    }

    public void SaveCurrentPosition()
    {
        if (myRectTransform == null) myRectTransform = GetComponent<RectTransform>();
        if (myRectTransform == null) return;

        savedAnchoredPosition = myRectTransform.anchoredPosition;
        PlayerPrefs.SetFloat(PREF_KEY_POS_X, savedAnchoredPosition.x);
        PlayerPrefs.SetFloat(PREF_KEY_POS_Y, savedAnchoredPosition.y);
        PlayerPrefs.SetInt(PREF_KEY_SAVED, 1);
        PlayerPrefs.SetFloat("PlayerHeartUI_Scale", uiScale);
        PlayerPrefs.Save();
    }

    public static void ClearSavedRuntimePosition()
    {
        PlayerPrefs.DeleteKey(PREF_KEY_POS_X);
        PlayerPrefs.DeleteKey(PREF_KEY_POS_Y);
        PlayerPrefs.DeleteKey(PREF_KEY_SAVED);
        PlayerPrefs.Save();
    }

    public void LoadSpritesIfNull()
    {
        if (fullHeartSprite == null)
        {
            fullHeartSprite = Resources.Load<Sprite>("heart_full");
            #if UNITY_EDITOR
            if (fullHeartSprite == null) fullHeartSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_full.png");
            #endif
        }
        if (halfHeartSprite == null)
        {
            halfHeartSprite = Resources.Load<Sprite>("heart_half");
            #if UNITY_EDITOR
            if (halfHeartSprite == null) halfHeartSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_half.png");
            #endif
        }
        if (emptyHeartSprite == null)
        {
            emptyHeartSprite = Resources.Load<Sprite>("heart_empty");
            #if UNITY_EDITOR
            if (emptyHeartSprite == null) emptyHeartSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_empty.png");
            #endif
        }
    }

    public void SetupHeartGrid()
    {
        if (heartsContainer == null)
        {
            heartsContainer = transform;
        }

        // Cập nhật hoặc Thêm HorizontalLayoutGroup
        HorizontalLayoutGroup layout = heartsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = heartsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        }
        layout.childAlignment = layoutAlignment;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = heartSpacing;

        // Dọn dẹp TOÀN BỘ tất cả các child cũ/thừa đang có dưới heartsContainer để loại bỏ sạch 100% tim đen trùng lặp từ Prefab
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in heartsContainer)
        {
            toDestroy.Add(child.gameObject);
        }

        for (int i = toDestroy.Count - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(toDestroy[i]);
            else DestroyImmediate(toDestroy[i]);
        }

        heartImages.Clear();

        // Tạo mới chuẩn xác maxHearts (10) ô tim
        for (int i = 0; i < maxHearts; i++)
        {
            GameObject heartObj = new GameObject($"Heart_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            heartObj.transform.SetParent(heartsContainer, false);

            RectTransform rect = heartObj.GetComponent<RectTransform>();
            if (rect != null) rect.sizeDelta = heartSize;

            Image img = heartObj.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = fullHeartSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
                heartImages.Add(img);
            }
        }
    }

    public void UpdateHeartsUI(int currentHealth, int maxHealth)
    {
        LoadSpritesIfNull();

        // Theo hệ thống Tim Minecraft: 1 Tim = 2 HP, 10 Tim = 20 HP.
        int scaledHealth;
        if (maxHealth == maxHearts * 2)
        {
            scaledHealth = Mathf.Clamp(currentHealth, 0, maxHearts * 2);
        }
        else
        {
            float healthRatio = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 1f;
            scaledHealth = Mathf.RoundToInt(healthRatio * (maxHearts * 2));
        }

        if (heartImages.Count != maxHearts || heartImages.Exists(img => img == null))
        {
            SetupHeartGrid();
        }

        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] == null) continue;

            int heartValue = (i + 1) * 2;

            if (scaledHealth >= heartValue)
            {
                heartImages[i].sprite = fullHeartSprite;
                heartImages[i].enabled = true;
            }
            else if (scaledHealth == heartValue - 1)
            {
                heartImages[i].sprite = halfHeartSprite;
                heartImages[i].enabled = true;
            }
            else
            {
                heartImages[i].sprite = emptyHeartSprite;
                heartImages[i].enabled = true;
            }

            RectTransform rect = heartImages[i].rectTransform;
            if (rect != null)
            {
                rect.sizeDelta = heartSize;
            }
        }

        Debug.Log($"<color=cyan>[PlayerHeartUI]</color> Cập nhật thanh tim UI chuẩn Minecraft! Máu hiện tại: {currentHealth}/{maxHealth} HP (Tương ứng: {scaledHealth/2f}/{maxHearts} tim)");

        if (Application.isPlaying && lastHealth >= 0 && currentHealth < lastHealth)
        {
            TriggerHitShake();
        }
        lastHealth = currentHealth;
    }

    public void TriggerHitShake()
    {
        if (!gameObject.activeInHierarchy) return;
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(DoShakeAnimation());
    }

    private IEnumerator DoShakeAnimation()
    {
        float elapsed = 0f;
        float duration = 0.25f;
        float magnitude = 5f;

        Vector3 startLocalPos = heartsContainer.localPosition;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            heartsContainer.localPosition = startLocalPos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        heartsContainer.localPosition = startLocalPos;
    }

    #region MOUSE DRAG IMPLEMENTATION
    public void OnPointerDown(PointerEventData eventData)
    {
        if (myRectTransform != null)
        {
            myRectTransform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        if (myRectTransform == null) myRectTransform = GetComponent<RectTransform>();
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();

        if (myRectTransform != null && rootCanvas != null)
        {
            myRectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
            savedAnchoredPosition = myRectTransform.anchoredPosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SaveCurrentPosition();
    }
    #endregion
}
