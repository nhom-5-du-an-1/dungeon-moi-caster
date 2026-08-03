using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị thanh kinh nghiệm (XP) và cấp độ (Level) độc lập của Player.
/// Hỗ trợ kéo thả trực tiếp bằng Chuột, tự động lưu vị trí kéo thả.
/// Tự động tách khỏi PlayerHeartsUI để làm hệ thống UI độc lập dưới Canvas.
/// Sử dụng hình ảnh số pixel art tự tạo từ 0-9 để đảm bảo số cấp độ cực kỳ sắc nét.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class PlayerXpUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    [Header("=== XP BAR SPRITES ===")]
    public Sprite emptyBarSprite;
    public Sprite fillBarSprite;

    [Header("=== PIXEL ART DIGITS (0-9) ===")]
    public Sprite[] digitSprites = new Sprite[10];
    public RectTransform levelNumberContainer;

    [Header("=== REFERENCES ===")]
    public PlayerStats playerStats;
    public Image emptyBarImage;
    public Image fillBarImage;

    [Header("=== SETTINGS ===")]
    [Tooltip("Kích thước thanh XP (Pixel)")]
    public Vector2 barSize = new Vector2(200f, 10f);

    [Range(0.2f, 3.0f)]
    [Tooltip("Tỷ lệ thu phóng giao diện (Scale)")]
    public float uiScale = 1.0f;

    [Header("=== KÉO THẢ & LƯU VỊ TRÍ (POSITION SAVING) ===")]
    [Tooltip("Bật/tắt cho phép dùng chuột giữ và kéo vị trí thanh XP")]
    public bool isDraggable = true;

    [Tooltip("Tự động lưu vị trí kéo thả khi chơi game")]
    public bool savePositionAcrossSessions = true;

    [SerializeField, HideInInspector]
    public Vector2 savedAnchoredPosition = new Vector2(20f, -70f);

    private const string PREF_KEY_POS_X = "PlayerXpUI_PosX";
    private const string PREF_KEY_POS_Y = "PlayerXpUI_PosY";
    private const string PREF_KEY_SAVED = "PlayerXpUI_HasSavedPos";

    private RectTransform myRectTransform;
    private Canvas rootCanvas;
    private float lastScale = 1.0f;

    void Awake()
    {
        myRectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();

        // Tải vị trí & kích thước đã lưu
        uiScale = PlayerPrefs.GetFloat("PlayerXpUI_Scale", 1.0f);
        transform.localScale = new Vector3(uiScale, uiScale, 1.0f);
        lastScale = uiScale;

        ApplySavedPosition();

        // 1. Tách khỏi PlayerHeartsUI để làm đối tượng độc lập dưới Canvas
        DetachFromLayoutGroup();

        // 2. Tạo UI
        CreateUIElements();
    }

    void OnEnable()
    {
        FindPlayerStats();

        if (playerStats != null)
        {
            playerStats.OnXpChanged -= UpdateXpUI;
            playerStats.OnXpChanged += UpdateXpUI;
            
            UpdateXpUI(playerStats.currentXp, playerStats.xpToNextLevel, playerStats.level);
        }
    }

    void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnXpChanged -= UpdateXpUI;
        }
    }

    void Start()
    {
        DetachFromLayoutGroup();
        CreateUIElements();
        FindPlayerStats();

        if (playerStats != null)
        {
            UpdateXpUI(playerStats.currentXp, playerStats.xpToNextLevel, playerStats.level);
        }
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            DetachFromLayoutGroup();

            // Tự động đồng bộ vị trí khi bạn kéo thả trong Editor Scene View
            if (myRectTransform == null) myRectTransform = GetComponent<RectTransform>();
            if (myRectTransform != null && myRectTransform.anchoredPosition != savedAnchoredPosition)
            {
                savedAnchoredPosition = myRectTransform.anchoredPosition;
                
                // Đồng bộ luôn vào PlayerPrefs để khi nhấn Play game không bị quay về vị trí cũ
                PlayerPrefs.SetFloat(PREF_KEY_POS_X, savedAnchoredPosition.x);
                PlayerPrefs.SetFloat(PREF_KEY_POS_Y, savedAnchoredPosition.y);
                PlayerPrefs.SetInt(PREF_KEY_SAVED, 1);
                PlayerPrefs.Save();
            }
            // Tự động đồng bộ kích thước khi bạn thay đổi chiều rộng/cao (Resize) trong Editor
            if (myRectTransform != null && myRectTransform.sizeDelta != barSize)
            {
                barSize = myRectTransform.sizeDelta;
            }

            // Tự động đồng bộ tỷ lệ Scale trong Editor (Hai chiều)
            if (transform.localScale.x != lastScale)
            {
                uiScale = transform.localScale.x;
                lastScale = uiScale;
                PlayerPrefs.SetFloat("PlayerXpUI_Scale", uiScale);
                PlayerPrefs.Save();
            }
            else if (uiScale != lastScale)
            {
                transform.localScale = new Vector3(uiScale, uiScale, 1.0f);
                lastScale = uiScale;
                PlayerPrefs.SetFloat("PlayerXpUI_Scale", uiScale);
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

    private void FindPlayerStats()
    {
        if (playerStats == null)
        {
            playerStats = PlayerStats.Instance;
            if (playerStats == null)
            {
                playerStats = FindFirstObjectByType<PlayerStats>();
            }
        }
    }

    /// <summary>
    /// Tách GameObject này ra khỏi PlayerHeartsUI để không bị ảnh hưởng bởi HorizontalLayoutGroup hay dọn dẹp tim của nó.
    /// </summary>
    public void DetachFromLayoutGroup()
    {
        if (transform.parent != null && transform.parent.name == "PlayerHeartsUI")
        {
            Transform newParent = transform.parent.parent; // Chuyển lên cùng cấp với PlayerHeartsUI (dưới PlayerHUD_Canvas)
            if (newParent != null)
            {
                transform.SetParent(newParent, false);
                Debug.Log("<color=green>[PlayerXpUI]</color> Đã tự động tách khỏi PlayerHeartsUI để trở thành hệ thống UI độc lập dưới Canvas.");
                
                // Căn lại vị trí mặc định dưới hàng tim một chút
                if (!PlayerPrefs.HasKey(PREF_KEY_SAVED))
                {
                    savedAnchoredPosition = new Vector2(20f, -70f);
                    ApplySavedPosition();
                }
            }
        }

        // Loại bỏ LayoutElement vì đã nằm ngoài LayoutGroup rồi
        LayoutElement layout = GetComponent<LayoutElement>();
        if (layout != null)
        {
            if (Application.isPlaying) Destroy(layout);
            else DestroyImmediate(layout);
        }
    }

    public void ApplySavedPosition()
    {
        if (myRectTransform == null) myRectTransform = GetComponent<RectTransform>();
        if (myRectTransform == null) return;

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
        PlayerPrefs.SetFloat("PlayerXpUI_Scale", uiScale);
        PlayerPrefs.Save();
    }

    public static void ClearSavedRuntimePosition()
    {
        PlayerPrefs.DeleteKey(PREF_KEY_POS_X);
        PlayerPrefs.DeleteKey(PREF_KEY_POS_Y);
        PlayerPrefs.DeleteKey(PREF_KEY_SAVED);
        PlayerPrefs.Save();
    }

    public void AlignPosition()
    {
        if (myRectTransform == null) myRectTransform = GetComponent<RectTransform>();
        if (myRectTransform == null) return;
        
        myRectTransform.sizeDelta = barSize;

        // Nếu chưa từng lưu kéo thả, gán vị trí mặc định
        if (!Application.isPlaying && !PlayerPrefs.HasKey(PREF_KEY_SAVED))
        {
            myRectTransform.anchoredPosition = savedAnchoredPosition;
        }
    }

    public void CreateUIElements()
    {
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

        LoadSpritesIfNull();
        AlignPosition();

        // Background
        if (emptyBarImage == null)
        {
            Transform bgTrans = transform.Find("Background");
            GameObject bgObj;
            if (bgTrans == null)
            {
                bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bgObj.transform.SetParent(transform, false);
            }
            else
            {
                bgObj = bgTrans.gameObject;
            }
            
            emptyBarImage = bgObj.GetComponent<Image>();
            emptyBarImage.type = Image.Type.Simple;
            emptyBarImage.preserveAspect = false;

            RectTransform rect = bgObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }
        if (emptyBarImage != null && emptyBarImage.sprite != emptyBarSprite)
        {
            emptyBarImage.sprite = emptyBarSprite;
        }

        // Fill
        if (fillBarImage == null)
        {
            Transform fillTrans = transform.Find("Fill");
            GameObject fillObj;
            if (fillTrans == null)
            {
                fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fillObj.transform.SetParent(transform, false);
            }
            else
            {
                fillObj = fillTrans.gameObject;
            }

            fillBarImage = fillObj.GetComponent<Image>();
            fillBarImage.type = Image.Type.Filled;
            fillBarImage.fillMethod = Image.FillMethod.Horizontal;
            fillBarImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillBarImage.preserveAspect = false;

            RectTransform rect = fillObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }
        if (fillBarImage != null && fillBarImage.sprite != fillBarSprite)
        {
            fillBarImage.sprite = fillBarSprite;
        }

        // LevelNumberContainer (Thay thế cho Text chữ bị mờ)
        if (levelNumberContainer == null)
        {
            Transform containerTrans = transform.Find("LevelNumberContainer");
            GameObject containerObj;
            if (containerTrans == null)
            {
                containerObj = new GameObject("LevelNumberContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                containerObj.transform.SetParent(transform, false);
            }
            else
            {
                containerObj = containerTrans.gameObject;
            }

            levelNumberContainer = containerObj.GetComponent<RectTransform>();
            levelNumberContainer.anchorMin = new Vector2(0.5f, 0.5f);
            levelNumberContainer.anchorMax = new Vector2(0.5f, 0.5f);
            levelNumberContainer.pivot = new Vector2(0.5f, 0.5f);
            levelNumberContainer.anchoredPosition = new Vector2(0f, 1f); // Hơi nhích lên trên một chút để cân đối
            levelNumberContainer.sizeDelta = new Vector2(100f, 16f);

            HorizontalLayoutGroup layout = containerObj.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 1f; // Khoảng cách giữa các chữ số
        }

        // Ẩn/xóa text cũ nếu có để tránh đè lấp
        Transform oldTextTrans = transform.Find("LevelText");
        if (oldTextTrans != null)
        {
            if (Application.isPlaying) Destroy(oldTextTrans.gameObject);
            else DestroyImmediate(oldTextTrans.gameObject);
        }
    }

    private void LoadSpritesIfNull()
    {
        if (emptyBarSprite == null)
        {
            emptyBarSprite = Resources.Load<Sprite>("xp_bar_empty");
#if UNITY_EDITOR
            if (emptyBarSprite == null) emptyBarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/xp_bar_empty.png");
#endif
        }

        if (fillBarSprite == null)
        {
            fillBarSprite = Resources.Load<Sprite>("xp_bar_full");
#if UNITY_EDITOR
            if (fillBarSprite == null) fillBarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/xp_bar_full.png");
#endif
        }

        LoadDigitSpritesIfNull();
    }

    private void LoadDigitSpritesIfNull()
    {
        bool needsLoad = false;
        if (digitSprites == null || digitSprites.Length < 10)
        {
            digitSprites = new Sprite[10];
            needsLoad = true;
        }
        else
        {
            for (int i = 0; i < 10; i++)
            {
                if (digitSprites[i] == null)
                {
                    needsLoad = true;
                    break;
                }
            }
        }

        if (needsLoad)
        {
            for (int i = 0; i < 10; i++)
            {
                if (digitSprites[i] == null)
                {
                    digitSprites[i] = Resources.Load<Sprite>($"num_{i}");
#if UNITY_EDITOR
                    if (digitSprites[i] == null)
                    {
                        string assetPath = $"Assets/tainguyen/item/num_{i}.png";
                        UnityEditor.AssetDatabase.ImportAsset(assetPath);
                        digitSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    }
#endif
                }
            }
        }
    }

    public void UpdateXpUI(int currentXp, int xpToNextLevel, int currentLevel)
    {
        CreateUIElements();

        if (fillBarImage != null)
        {
            float fillAmount = xpToNextLevel > 0 ? (float)currentXp / xpToNextLevel : 0f;
            fillBarImage.fillAmount = Mathf.Clamp01(fillAmount);
        }

        UpdateLevelDigits(currentLevel);

        Debug.Log($"<color=green>[PlayerXpUI]</color> Cập nhật thanh XP UI độc lập! Tiến trình: {currentXp}/{xpToNextLevel} XP (Cấp {currentLevel})");
    }

    private void UpdateLevelDigits(int currentLevel)
    {
        if (levelNumberContainer == null) return;

        string levelStr = Mathf.Max(0, currentLevel).ToString();

        // 1. Đảm bảo load đủ sprite số
        LoadDigitSpritesIfNull();

        // 2. Tắt tất cả các đối tượng số cũ
        int childCount = levelNumberContainer.childCount;
        for (int i = 0; i < childCount; i++)
        {
            levelNumberContainer.GetChild(i).gameObject.SetActive(false);
        }

        // 3. Tạo hoặc tái sử dụng các Image con hiển thị từng chữ số
        for (int i = 0; i < levelStr.Length; i++)
        {
            int digitVal = levelStr[i] - '0';
            if (digitVal < 0 || digitVal > 9) continue;

            Sprite digitSprite = null;
            if (digitSprites != null && digitVal < digitSprites.Length)
            {
                digitSprite = digitSprites[digitVal];
            }

            GameObject digitObj;
            if (i < childCount)
            {
                digitObj = levelNumberContainer.GetChild(i).gameObject;
                digitObj.SetActive(true);
            }
            else
            {
                digitObj = new GameObject($"Digit_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                digitObj.transform.SetParent(levelNumberContainer, false);
            }

            RectTransform rect = digitObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(10f, 10f); // Hiển thị chuẩn nét sắc sảo (gốc pixel art 8x8)

            Image img = digitObj.GetComponent<Image>();
            img.sprite = digitSprite;
            img.enabled = (digitSprite != null); // Tắt hiển thị nếu ảnh bị null để không bị ô trắng
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false; // Tránh chặn sự kiện drag chuột
        }
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
