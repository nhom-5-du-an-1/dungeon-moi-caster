using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class SetupPlayerStatsUI : EditorWindow
{
    [MenuItem("Tools/Setup Player Stats UI")]
    public static void SetupPlayerStats()
    {
        Debug.Log("Bắt đầu khởi tạo hệ thống UI Máu & Mana chuẩn 2D RPG 32x32 sắc nét...");

        // 1. Find the Player GameObject
        GameObject playerObj = GameObject.Find("DarkFantasyPlayer");
        if (playerObj == null) playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj == null)
        {
            Debug.LogError("Không tìm thấy GameObject Player trong Scene! Vui lòng kiểm tra lại nhân vật.");
            return;
        }

        // 2. Attach PlayerStats component if not present
        PlayerStats playerStats = playerObj.GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            playerStats = playerObj.AddComponent<PlayerStats>();
            Undo.RegisterCreatedObjectUndo(playerStats, "Add PlayerStats");
            Debug.Log("Đã thêm component PlayerStats vào Player.");
        }

        // 3. Find or create Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        GameObject canvasObj;
        if (canvas == null)
        {
            canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            Debug.Log("Đã tạo mới Canvas UI.");
        }
        else
        {
            canvasObj = canvas.gameObject;
        }

        // Load custom RPG HUD Frame texture asset
        Sprite customRpgFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/rpg_hud_frame.png");

        // Generate 32x32 Sprites so Unity Image.Type.Filled works 100% properly and looks beautiful!
        Sprite frameSprite = CreateBoxTexture(new Color(0.11f, 0.11f, 0.14f, 0.96f), new Color(0.65f, 0.48f, 0.22f, 1f));
        Sprite slotBgSprite = CreateBoxTexture(new Color(0.08f, 0.08f, 0.10f, 1f), new Color(0.18f, 0.18f, 0.22f, 1f));
        Sprite healthFillSprite = CreateBarTexture(new Color(0.92f, 0.16f, 0.16f, 1f), new Color(1.0f, 0.5f, 0.5f, 1f), new Color(0.55f, 0.05f, 0.05f, 1f));
        Sprite catchUpFillSprite = CreateBarTexture(new Color(0.95f, 0.55f, 0.1f, 1f), new Color(1.0f, 0.8f, 0.3f, 1f), new Color(0.6f, 0.3f, 0.0f, 1f));
        Sprite manaFillSprite = CreateBarTexture(new Color(0.12f, 0.65f, 0.98f, 1f), new Color(0.6f, 0.88f, 1.0f, 1f), new Color(0.05f, 0.35f, 0.65f, 1f));

        // 4. Create or Recreate PlayerHUD Panel
        Transform existingHUD = canvasObj.transform.Find("PlayerHUD");
        if (existingHUD != null)
        {
            Undo.DestroyObjectImmediate(existingHUD.gameObject);
        }

        GameObject hudObj = new GameObject("PlayerHUD");
        hudObj.transform.SetParent(canvasObj.transform, false);
        Undo.RegisterCreatedObjectUndo(hudObj, "Create PlayerHUD");

        // Main HUD Frame
        RectTransform hudRect = hudObj.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0f, 1f); // Top Left
        hudRect.anchorMax = new Vector2(0f, 1f);
        hudRect.pivot = new Vector2(0f, 1f);
        hudRect.anchoredPosition = new Vector2(30f, -30f);
        hudRect.sizeDelta = new Vector2(380f, 120f);

        Image hudBg = hudObj.AddComponent<Image>();
        if (customRpgFrameSprite != null)
        {
            hudBg.sprite = customRpgFrameSprite;
            hudBg.type = Image.Type.Simple;
            hudBg.preserveAspect = true;
        }
        else
        {
            hudBg.sprite = frameSprite;
            hudBg.type = Image.Type.Sliced;
        }

        // Add PlayerStatsUI manager component
        PlayerStatsUI uiManager = hudObj.AddComponent<PlayerStatsUI>();
        uiManager.playerStats = playerStats;
        uiManager.hudRectTransform = hudRect;

        // Font selection
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // ========================================================
        // 5. AVATAR / ICON BOX (LEFT SIDE)
        // ========================================================
        GameObject avatarBoxObj = new GameObject("AvatarBox");
        avatarBoxObj.transform.SetParent(hudObj.transform, false);

        RectTransform avatarRect = avatarBoxObj.AddComponent<RectTransform>();
        avatarRect.anchorMin = new Vector2(0f, 1f);
        avatarRect.anchorMax = new Vector2(0f, 1f);
        avatarRect.pivot = new Vector2(0f, 1f);
        avatarRect.anchoredPosition = new Vector2(15f, -15f);
        avatarRect.sizeDelta = new Vector2(90f, 90f);

        Image avatarBg = avatarBoxObj.AddComponent<Image>();
        avatarBg.sprite = frameSprite;
        avatarBg.type = Image.Type.Sliced;

        // Avatar Icon Text
        GameObject avatarTextObj = new GameObject("AvatarText");
        avatarTextObj.transform.SetParent(avatarBoxObj.transform, false);
        RectTransform avatarTextRect = avatarTextObj.AddComponent<RectTransform>();
        avatarTextRect.anchorMin = Vector2.zero;
        avatarTextRect.anchorMax = Vector2.one;
        avatarTextRect.sizeDelta = Vector2.zero;

        Text avatarTxt = avatarTextObj.AddComponent<Text>();
        avatarTxt.text = "HERO";
        avatarTxt.font = font;
        avatarTxt.fontSize = 20;
        avatarTxt.fontStyle = FontStyle.Bold;
        avatarTxt.alignment = TextAnchor.MiddleCenter;
        avatarTxt.color = new Color(1.0f, 0.84f, 0.2f, 1.0f);

        uiManager.avatarText = avatarTxt;

        // ========================================================
        // 6. HEALTH BAR (TOP BAR - HP)
        // ========================================================
        GameObject healthContainer = new GameObject("HealthContainer");
        healthContainer.transform.SetParent(hudObj.transform, false);

        RectTransform healthContRect = healthContainer.AddComponent<RectTransform>();
        healthContRect.anchorMin = new Vector2(0f, 1f);
        healthContRect.anchorMax = new Vector2(0f, 1f);
        healthContRect.pivot = new Vector2(0f, 1f);
        healthContRect.anchoredPosition = new Vector2(120f, -20f);
        healthContRect.sizeDelta = new Vector2(240f, 34f);

        Image healthBg = healthContainer.AddComponent<Image>();
        healthBg.sprite = slotBgSprite;
        healthBg.type = Image.Type.Sliced;

        // CatchUp Fill Bar (Yellow/Orange smooth damage lerp)
        GameObject catchUpObj = new GameObject("CatchUpBarFill");
        catchUpObj.transform.SetParent(healthContainer.transform, false);
        RectTransform catchUpRect = catchUpObj.AddComponent<RectTransform>();
        catchUpRect.anchorMin = Vector2.zero;
        catchUpRect.anchorMax = Vector2.one;
        catchUpRect.sizeDelta = Vector2.zero;

        Image catchUpImage = catchUpObj.AddComponent<Image>();
        catchUpImage.sprite = catchUpFillSprite;
        catchUpImage.type = Image.Type.Filled;
        catchUpImage.fillMethod = Image.FillMethod.Horizontal;
        catchUpImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        catchUpImage.fillAmount = 1.0f;

        uiManager.catchUpBarFill = catchUpImage;

        // Health Fill Bar (Vibrant Crimson Red 32x32 sprite)
        GameObject healthFillObj = new GameObject("HealthBarFill");
        healthFillObj.transform.SetParent(healthContainer.transform, false);
        RectTransform healthFillRect = healthFillObj.AddComponent<RectTransform>();
        healthFillRect.anchorMin = Vector2.zero;
        healthFillRect.anchorMax = Vector2.one;
        healthFillRect.sizeDelta = Vector2.zero;

        Image healthFillImage = healthFillObj.AddComponent<Image>();
        healthFillImage.sprite = healthFillSprite;
        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Horizontal;
        healthFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthFillImage.fillAmount = 1.0f;

        uiManager.healthBarFill = healthFillImage;

        // Health Text Label (High Contrast Crisp White with Heavy Black Outline)
        GameObject healthTextObj = new GameObject("HealthText");
        healthTextObj.transform.SetParent(healthContainer.transform, false);
        RectTransform healthTextRect = healthTextObj.AddComponent<RectTransform>();
        healthTextRect.anchorMin = Vector2.zero;
        healthTextRect.anchorMax = Vector2.one;
        healthTextRect.sizeDelta = Vector2.zero;

        Text healthText = healthTextObj.AddComponent<Text>();
        healthText.alignment = TextAnchor.MiddleCenter;
        healthText.fontSize = 20;
        healthText.fontStyle = FontStyle.Bold;
        healthText.color = Color.white;
        healthText.font = font;
        healthText.horizontalOverflow = HorizontalWrapMode.Overflow;
        healthText.verticalOverflow = VerticalWrapMode.Overflow;
        healthText.text = "100 / 100";

        Outline healthTextOutline = healthTextObj.AddComponent<Outline>();
        healthTextOutline.effectColor = Color.black;
        healthTextOutline.effectDistance = new Vector2(1.5f, -1.5f);

        uiManager.healthText = healthText;

        // ========================================================
        // 7. MANA BAR (BOTTOM BAR - MP)
        // ========================================================
        GameObject manaContainer = new GameObject("ManaContainer");
        manaContainer.transform.SetParent(hudObj.transform, false);

        RectTransform manaContRect = manaContainer.AddComponent<RectTransform>();
        manaContRect.anchorMin = new Vector2(0f, 1f);
        manaContRect.anchorMax = new Vector2(0f, 1f);
        manaContRect.pivot = new Vector2(0f, 1f);
        manaContRect.anchoredPosition = new Vector2(120f, -65f);
        manaContRect.sizeDelta = new Vector2(240f, 30f);

        Image manaBg = manaContainer.AddComponent<Image>();
        manaBg.sprite = slotBgSprite;
        manaBg.type = Image.Type.Sliced;

        // Mana Fill Bar (Vibrant Crystal Magic Blue 32x32 sprite)
        GameObject manaFillObj = new GameObject("ManaBarFill");
        manaFillObj.transform.SetParent(manaContainer.transform, false);
        RectTransform manaFillRect = manaFillObj.AddComponent<RectTransform>();
        manaFillRect.anchorMin = Vector2.zero;
        manaFillRect.anchorMax = Vector2.one;
        manaFillRect.sizeDelta = Vector2.zero;

        Image manaFillImage = manaFillObj.AddComponent<Image>();
        manaFillImage.sprite = manaFillSprite;
        manaFillImage.type = Image.Type.Filled;
        manaFillImage.fillMethod = Image.FillMethod.Horizontal;
        manaFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        manaFillImage.fillAmount = 1.0f;

        uiManager.manaBarFill = manaFillImage;

        // Mana Text Label (High Contrast Crisp White with Heavy Black Outline)
        GameObject manaTextObj = new GameObject("ManaText");
        manaTextObj.transform.SetParent(manaContainer.transform, false);
        RectTransform manaTextRect = manaTextObj.AddComponent<RectTransform>();
        manaTextRect.anchorMin = Vector2.zero;
        manaTextRect.anchorMax = Vector2.one;
        manaTextRect.sizeDelta = Vector2.zero;

        Text manaText = manaTextObj.AddComponent<Text>();
        manaText.alignment = TextAnchor.MiddleCenter;
        manaText.fontSize = 18;
        manaText.fontStyle = FontStyle.Bold;
        manaText.color = Color.white;
        manaText.font = font;
        manaText.horizontalOverflow = HorizontalWrapMode.Overflow;
        manaText.verticalOverflow = VerticalWrapMode.Overflow;
        manaText.text = "100 / 100";

        Outline manaTextOutline = manaTextObj.AddComponent<Outline>();
        manaTextOutline.effectColor = Color.black;
        manaTextOutline.effectDistance = new Vector2(1.5f, -1.5f);

        uiManager.manaText = manaText;

        // 8. Save modifications
        EditorUtility.SetDirty(playerStats);
        EditorUtility.SetDirty(uiManager);
        EditorUtility.SetDirty(canvasObj);
        
        Selection.activeGameObject = hudObj;

        Debug.Log("<color=green>[SetupPlayerStatsUI]</color> Đã tạo mới Sprite 32x32 và cài đặt thành công 2D RPG HUD cực kỳ sắc nét!");
    }

    private static Sprite CreateBarTexture(Color mainColor, Color topHighlight, Color bottomBorder)
    {
        Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                if (y >= 29) tex.SetPixel(x, y, topHighlight);
                else if (y <= 2 || x == 0 || x == 31) tex.SetPixel(x, y, bottomBorder);
                else tex.SetPixel(x, y, mainColor);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }

    private static Sprite CreateBoxTexture(Color bodyColor, Color borderColor)
    {
        Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                if (x < 2 || x > 29 || y < 2 || y > 29) tex.SetPixel(x, y, borderColor);
                else tex.SetPixel(x, y, bodyColor);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
    }
}
