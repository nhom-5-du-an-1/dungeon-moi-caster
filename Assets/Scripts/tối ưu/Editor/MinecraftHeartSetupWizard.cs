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

        Canvas canvas = Object.FindObjectOfType<Canvas>();
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

        EditorUtility.SetDirty(heartUIObj);
        Debug.Log("<color=green>[PlayerHeartUI]</color> Đã setup thành công UI 10 trái tim Minecraft cho Player trên Canvas!");
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
}
