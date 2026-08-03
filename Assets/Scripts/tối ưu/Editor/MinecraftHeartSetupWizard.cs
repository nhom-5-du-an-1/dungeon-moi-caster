using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MinecraftHeartSetupWizard
{
    [MenuItem("Tools/Minecraft Heart System/Auto Setup All (Player & Enemies)")]
    public static void AutoSetupAll()
    {
        // 1. Generate PNG Heart Assets
        HeartSpriteGenerator.GenerateAllHeartSprites();

        // 2. Setup Player UI Canvas Hearts
        SetupPlayerHeartUI();

        // 3. Setup Enemy Health Components in Scene
        SetupEnemyHeartUI();

        Debug.Log("<color=green>[MinecraftHeartSetupWizard]</color> đã hoàn tất cài đặt toàn bộ hệ thống máu trái tim Minecraft cho Player và Quái vật!");
        EditorUtility.DisplayDialog("Thành công!", "Đã tự động tạo các file ảnh trái tim Minecraft trong Assets/tainguyen/item và cấu hình UI cho Player & Quái vật thành công!", "OK");
    }

    [MenuItem("Tools/Minecraft Heart System/Setup Player Hearts UI Only")]
    public static void SetupPlayerHeartUI()
    {
        HeartSpriteGenerator.GenerateAllHeartSprites();
        AssetDatabase.Refresh();

        // Cấu hình lại các file hình ảnh XP thành Single Sprite và Point filter (không bị mờ/sắt nét)
        ConfigureXpSpriteImporter("Assets/tainguyen/item/xp_bar_empty.png");
        ConfigureXpSpriteImporter("Assets/tainguyen/item/xp_bar_full.png");
        ConfigureXpSpriteImporter("Assets/tainguyen/item/xp_orb.png");
        ConfigureXpSpriteImporter("Assets/tainguyen/item/xp_orb_hd.png");
        for (int i = 0; i <= 9; i++)
        {
            ConfigureXpSpriteImporter($"Assets/tainguyen/item/num_{i}.png");
        }
        AssetDatabase.Refresh();

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log("[MinecraftHeartSetupWizard] Đã tạo Canvas mới.");
        }

        Transform heartUITransform = canvas.transform.Find("PlayerHeartsUI");
        GameObject heartUIObj;
        if (heartUITransform == null)
        {
            heartUIObj = new GameObject("PlayerHeartsUI");
            heartUIObj.transform.SetParent(canvas.transform, false);

            RectTransform rect = heartUIObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f); // Top Left
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
        }
        else
        {
            heartUIObj = heartUITransform.gameObject;
        }

        PlayerHeartUI heartUI = heartUIObj.GetComponent<PlayerHeartUI>();
        if (heartUI == null)
        {
            heartUI = heartUIObj.AddComponent<PlayerHeartUI>();
        }

        heartUI.heartsContainer = heartUIObj.transform;
        heartUI.fullHeartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_full.png");
        heartUI.halfHeartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_half.png");
        heartUI.emptyHeartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_empty.png");

        PlayerStats stats = Object.FindObjectOfType<PlayerStats>();
        if (stats != null)
        {
            heartUI.playerStats = stats;
        }

        heartUI.LoadSpritesIfNull();
        heartUI.SaveCurrentPosition();
        heartUI.UpdateHeartsUI(stats != null ? stats.currentHealth : 20, stats != null ? stats.maxHealth : 20);

        // Cấu hình thêm thanh kinh nghiệm XP UI độc lập dưới Canvas (cùng cấp với PlayerHeartsUI)
        Transform canvasTransform = heartUIObj.transform.parent;
        Transform xpUITransform = canvasTransform.Find("PlayerXpUI");
        GameObject xpUIObj;
        if (xpUITransform == null)
        {
            xpUIObj = new GameObject("PlayerXpUI", typeof(RectTransform));
            xpUIObj.transform.SetParent(canvasTransform, false);
        }
        else
        {
            xpUIObj = xpUITransform.gameObject;
        }

        PlayerXpUI xpUI = xpUIObj.GetComponent<PlayerXpUI>();
        if (xpUI == null)
        {
            xpUI = xpUIObj.AddComponent<PlayerXpUI>();
        }

        // Tự động gán các sprite kinh nghiệm
        xpUI.emptyBarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/xp_bar_empty.png");
        xpUI.fillBarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/xp_bar_full.png");
        
        // Tự động gán các sprite chữ số pixel art 0-9
        xpUI.digitSprites = new Sprite[10];
        for (int i = 0; i <= 9; i++)
        {
            xpUI.digitSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/tainguyen/item/num_{i}.png");
        }
        
        if (stats != null)
        {
            xpUI.playerStats = stats;
        }

        xpUI.savedAnchoredPosition = new Vector2(20f, -70f); // Default position
        xpUI.CreateUIElements();
        if (stats != null)
        {
            xpUI.UpdateXpUI(stats.currentXp, stats.xpToNextLevel, stats.level);
        }
        else
        {
            xpUI.UpdateXpUI(0, 100, 1);
        }

        EditorUtility.SetDirty(xpUIObj);
        EditorUtility.SetDirty(heartUIObj);
        Debug.Log("<color=green>[PlayerHeartUI & PlayerXpUI]</color> Đã setup thành công UI 10 trái tim và thanh kinh nghiệm XP!");
    }

    [MenuItem("Tools/Minecraft Heart System/Setup Enemy Hearts Only")]
    public static void SetupEnemyHeartUI()
    {
        HeartSpriteGenerator.GenerateAllHeartSprites();

        Sprite enemyFull = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_enemy.png");
        Sprite enemyHalf = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_half.png");
        Sprite enemyEmpty = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_empty.png");

        EnemyHealth[] enemies = Object.FindObjectsOfType<EnemyHealth>();
        int count = 0;
        foreach (EnemyHealth enemy in enemies)
        {
            enemy.useHeartDisplay = true;
            enemy.enemyHeartFull = enemyFull;
            enemy.enemyHeartHalf = enemyHalf;
            enemy.enemyHeartEmpty = enemyEmpty;
            enemy.heartCount = 5;
            enemy.CreateSpriteHealthBar();
            enemy.UpdateHealthBarUI(true);
            EditorUtility.SetDirty(enemy);
            count++;
        }

        Debug.Log($"<color=green>[EnemyHealth]</color> Đã cập nhật chế độ trái tim Minecraft cho {count} quái vật trong Scene!");
    }

    [MenuItem("Tools/Minecraft Heart System/Cleanup Duplicate Enemy Hearts")]
    public static void CleanupDuplicateEnemyHearts()
    {
        EnemyHealth[] enemies = Object.FindObjectsOfType<EnemyHealth>();
        int count = 0;
        foreach (EnemyHealth enemy in enemies)
        {
            enemy.CreateSpriteHealthBar();
            enemy.UpdateHealthBarUI(true);
            EditorUtility.SetDirty(enemy);
            count++;
        }
        Debug.Log($"<color=green>[EnemyHealth]</color> Đã dọn dẹp các ô tim trùng lặp cho {count} quái vật trong Scene!");
        EditorUtility.DisplayDialog("Hoàn tất!", $"Đã tự động xóa sạch các ô tim trùng lặp của {count} quái vật trong Scene!", "OK");
    }

    private static void ConfigureXpSpriteImporter(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
