using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor.Events;

public class SetupInventoryUI : EditorWindow
{
    [MenuItem("Tools/Setup Inventory UI")]
    public static void CreateInventoryUI()
    {
        // 1. Find or create Canvas
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
            Debug.Log("Created new UI Canvas.");
        }
        else
        {
            canvasObj = canvas.gameObject;
            
            // CRITICAL FIX: Ensure GraphicRaycaster is attached to existing Canvas so UI clicks work!
            GraphicRaycaster raycaster = canvasObj.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                canvasObj.AddComponent<GraphicRaycaster>();
                Debug.Log("Added missing GraphicRaycaster to existing Canvas.");
            }
        }

        // 2. Find or create EventSystem
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        GameObject eventSystemObj;
        if (eventSystem == null)
        {
            eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
            Debug.Log("Created new EventSystem.");
        }
        else
        {
            eventSystemObj = eventSystem.gameObject;
        }

        // CRITICAL FIX: Configure EventSystem modules based on project settings (compatibility with both Legacy and New Input System)
        System.Type newModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (newModuleType != null)
        {
            Component newModule = eventSystemObj.GetComponent(newModuleType);
            if (newModule == null)
            {
                eventSystemObj.AddComponent(newModuleType);
                Debug.Log("Added InputSystemUIInputModule to EventSystem for New Input System compatibility.");
            }
            
            // Remove legacy module to prevent warnings/errors that block UI interaction
            StandaloneInputModule legacyModule = eventSystemObj.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                Undo.DestroyObjectImmediate(legacyModule);
            }
        }
        else
        {
            StandaloneInputModule legacyModule = eventSystemObj.GetComponent<StandaloneInputModule>();
            if (legacyModule == null)
            {
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Debug.Log("Added StandaloneInputModule to EventSystem for legacy input support.");
            }
        }

        // 3. Find or create InventoryManager
        InventoryManager manager = Object.FindAnyObjectByType<InventoryManager>();
        if (manager == null)
        {
            manager = canvasObj.AddComponent<InventoryManager>();
            Undo.RegisterCompleteObjectUndo(canvasObj, "Add InventoryManager");
            Debug.Log("Added InventoryManager to Canvas.");
        }

        // 4. Create or recreate Inventory Panel
        Transform existingPanel = canvasObj.transform.Find("InventoryPanel");
        if (existingPanel != null)
        {
            Undo.DestroyObjectImmediate(existingPanel.gameObject);
        }

        // Tự động tối ưu hình ảnh túi đồ cho game Pixel Art
        string spritePath = "Assets/tainguyen/item/túi đồ.png";
        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
            bool needsReimport = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                needsReimport = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                needsReimport = true;
            }
            if (needsReimport)
            {
                importer.SaveAndReimport();
                AssetDatabase.Refresh();
                Debug.Log("[SetupInventoryUI] Đã tự động chuyển túi đồ.png sang Sprite và bật Point Filter để giữ nét đứt pixel!");
            }
        }

        GameObject panelObj = new GameObject("InventoryPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        panelObj.AddComponent<UIWindowAnimator>(); // Bật hiệu ứng mở/đóng túi đồ mượt mà bằng co giãn scale
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panelObj.AddComponent<Image>();
        Sprite inventorySprite = null;
        
        // Tải sprite an toàn kể cả khi ảnh được import ở chế độ Multiple
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        foreach (Object asset in assets)
        {
            if (asset is Sprite s)
            {
                inventorySprite = s;
                break;
            }
        }

        if (inventorySprite != null)
        {
            panelImage.sprite = inventorySprite;
            panelImage.type = Image.Type.Simple;
            
            // Tính toán tỉ lệ chuẩn để tránh bị giãn hình méo mó
            float spriteWidth = inventorySprite.rect.width;
            float spriteHeight = inventorySprite.rect.height;
            float aspectRatio = spriteWidth / spriteHeight;
            
            // Đặt kích thước hiển thị giới hạn tối đa phù hợp màn hình
            float targetWidth = 700f;
            float targetHeight = targetWidth / aspectRatio;
            
            if (targetHeight > 550f)
            {
                targetHeight = 550f;
                targetWidth = targetHeight * aspectRatio;
            }
            
            panelRect.sizeDelta = new Vector2(targetWidth, targetHeight);
            Debug.Log($"Loaded 'túi đồ.png' successfully. Size set to {targetWidth}x{targetHeight} based on aspect ratio: {aspectRatio:F2}.");
        }
        else
        {
            panelRect.sizeDelta = new Vector2(800, 600);
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            Debug.LogWarning("'túi đồ.png' sprite not found at Assets/tainguyen/item/túi đồ.png! Using fallback dark panel.");
        }

        // 5. Create Close Button (Một nút đóng X rõ ràng để người chơi di chuyển đè lên hình vẽ sẵn)
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform btnRect = closeBtnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 1f); // Anchored to top right
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(1f, 1f);
        btnRect.sizeDelta = new Vector2(50, 50); 
        // Vị trí mặc định ở góc trên bên phải của khung. Bạn có thể kéo trong Editor để khớp với dấu X vẽ sẵn trên ảnh.
        btnRect.anchoredPosition = new Vector2(-15, -15);

        Image btnImage = closeBtnObj.AddComponent<Image>();
        // Nút tròn/vuông màu đỏ nhạt bán trong suốt, giúp nhìn rõ vùng click chuột để căn chỉnh.
        btnImage.color = new Color(0.85f, 0.2f, 0.2f, 0.85f); 

        Button btn = closeBtnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        closeBtnObj.AddComponent<UIHoverEffect>(); // Bật hiệu ứng hover nổi to lên và co lại khi click

        // Add text "X" inside close button
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        Text txt = textObj.AddComponent<Text>();
        txt.text = "X";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 24;
        
        Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (legacyFont == null) legacyFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.font = legacyFont;
        txt.color = Color.white;

        // Bind onClick event using UnityEventTools
        UnityEventTools.AddPersistentListener(btn.onClick, manager.CloseInventory);
        
        // Assign references in InventoryManager
        manager.inventoryPanel = panelObj;
        
        // Make panel inactive by default
        panelObj.SetActive(false);

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(canvasObj);
        
        // Select the new panel
        Selection.activeGameObject = panelObj;
        
        Debug.Log("<color=green>[SetupInventoryUI]</color> Setup Inventory UI completed successfully! Press E to open/close or click X to close.");
    }
}
