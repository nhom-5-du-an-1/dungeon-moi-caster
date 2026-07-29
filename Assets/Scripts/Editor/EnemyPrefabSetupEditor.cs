using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class EnemyPrefabSetupEditor : AssetPostprocessor
{
    // Tự động chạy khi tải xong Editor hoặc compilation xong
    [InitializeOnLoadMethod]
    public static void InitializeOnLoad()
    {
        // Đăng ký tự động chạy sau khi import tài nguyên xong để đảm bảo mọi thứ sẵn sàng
    }

    [MenuItem("Dungeon Tools/Setup Enemy Prefabs")]
    public static void SetupAllEnemyPrefabs()
    {
        Debug.Log("<color=yellow>[Enemy Setup Tool]</color> Bắt đầu thiết lập quái vật...");

        // 1. Tạo hình đạn tròn trắng
        string projectileSpritePath = "Assets/tainguyen/quai/projectile.png";
        CreateProjectileSprite(projectileSpritePath);

        // 2. Thiết lập Prefab đạn MonsterProjectile
        string projectilePrefabPath = "Assets/prefab/QUAI/MonsterProjectile.prefab";
        GameObject projectilePrefab = SetupProjectilePrefab(projectileSpritePath, projectilePrefabPath);

        if (projectilePrefab == null)
        {
            Debug.LogError("<color=red>[Enemy Setup Tool]</color> Không thể tạo hoặc tải Projectile Prefab!");
            return;
        }

        // 3. Cấu hình 3 Plants quái thường
        SetupPlantPrefab("Assets/prefab/QUAI/plants/Plant.prefab", MonsterProjectile.ElementType.Poison, projectilePrefab, 2, 4.0f, 5.0f, 1.8f);
        SetupPlantPrefab("Assets/prefab/QUAI/plants/Plant2.prefab", MonsterProjectile.ElementType.Frost, projectilePrefab, 2, 5.0f, 5.0f, 2.0f);
        SetupPlantPrefab("Assets/prefab/QUAI/plants/Plant3.prefab", MonsterProjectile.ElementType.Fire, projectilePrefab, 4, 4.5f, 6.0f, 1.5f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[Enemy Setup Tool]</color> Thiết lập hoàn tất! Tất cả Prefab đã sẵn sàng sử dụng.");
    }

    private static void CreateProjectileSprite(string path)
    {
        if (File.Exists(path)) return;

        Debug.Log($"<color=cyan>[Enemy Setup Tool]</color> Tạo sprite đạn tròn tại: {path}");
        
        int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        float center = size / 2.0f - 0.5f;
        float radius = size / 2.0f - 1.0f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= radius)
                {
                    // Lõi đạn trắng tinh, viền hơi mờ (Antialiasing)
                    float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        }

        texture.Apply();
        
        byte[] bytes = texture.EncodeToPNG();
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);

        // Cấu hình import Texture thành Sprite
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }

    private static GameObject SetupProjectilePrefab(string spritePath, string prefabPath)
    {
        Sprite projectileSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (projectileSprite == null)
        {
            Debug.LogError($"Không tìm thấy Sprite đạn tại: {spritePath}");
            return null;
        }

        GameObject prefabRoot = null;
        bool isNew = false;

        if (File.Exists(prefabPath))
        {
            prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        }
        else
        {
            prefabRoot = new GameObject("MonsterProjectile");
            isNew = true;
            string dir = Path.GetDirectoryName(prefabPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        // Cấu hình SpriteRenderer
        SpriteRenderer sr = prefabRoot.GetComponent<SpriteRenderer>();
        if (sr == null) sr = prefabRoot.AddComponent<SpriteRenderer>();
        sr.sprite = projectileSprite;
        sr.sortingOrder = 1005;

        // Cấu hình Rigidbody2D
        Rigidbody2D rb = prefabRoot.GetComponent<Rigidbody2D>();
        if (rb == null) rb = prefabRoot.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Cấu hình Collider2D (Trigger)
        CircleCollider2D col = prefabRoot.GetComponent<CircleCollider2D>();
        if (col == null) col = prefabRoot.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;

        // Cấu hình MonsterProjectile Script
        MonsterProjectile mp = prefabRoot.GetComponent<MonsterProjectile>();
        if (mp == null) mp = prefabRoot.AddComponent<MonsterProjectile>();
        mp.speed = 5.0f;
        mp.lifeTime = 5.0f;

        // Ghi lại thành Prefab
        GameObject savedPrefab;
        if (isNew)
        {
            savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Object.DestroyImmediate(prefabRoot);
        }
        else
        {
            savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        return savedPrefab;
    }

    private static void SetupPlantPrefab(string path, MonsterProjectile.ElementType element, GameObject projectilePrefab, int damage, float shootRange, float bulletSpeed, float cooldown)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"<color=orange>[Enemy Setup Tool]</color> Không tìm thấy prefab quái thường tại: {path}");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);

        // 1. Thêm Rigidbody2D
        Rigidbody2D rb = root.GetComponent<Rigidbody2D>();
        if (rb == null) rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 2. Thêm Collider2D làm vùng cản vật lý
        CapsuleCollider2D col = root.GetComponent<CapsuleCollider2D>();
        if (col == null) col = root.AddComponent<CapsuleCollider2D>();
        col.isTrigger = false;
        col.offset = new Vector2(0f, 0.12f);
        col.size = new Vector2(0.55f, 0.75f);
        col.direction = CapsuleDirection2D.Vertical;

        // 3. Thêm script EnemyHealth
        EnemyHealth health = root.GetComponent<EnemyHealth>();
        if (health == null) health = root.AddComponent<EnemyHealth>();
        health.maxHealth = 10;
        health.currentHealth = 10;
        health.useHeartDisplay = true;
        health.heartCount = 5;
        health.alwaysShowHealthBar = true;
        health.barHeightOffset = 0.08f;
        health.barWidth = 0.28f;
        health.barHeight = 0.04f;

        // Load các sprite Tim từ Thư mục tainguyen
        health.enemyHeartFull = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_enemy.png");
        health.enemyHeartHalf = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_half.png");
        health.enemyHeartEmpty = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_empty.png");

        // 4. Thêm script PlantAI
        PlantAI ai = root.GetComponent<PlantAI>();
        if (ai == null) ai = root.AddComponent<PlantAI>();
        ai.elementType = element;
        ai.projectilePrefab = projectilePrefab;
        ai.projectileDamage = damage;
        ai.shootRange = shootRange;
        ai.projectileSpeed = bulletSpeed;
        ai.attackCooldown = cooldown;
        ai.detectionRadius = 5.5f;
        ai.loseSightRadius = 8.5f;
        ai.moveSpeed = 1.3f;
        ai.patrolSpeed = 0.7f;
        ai.attackDuration = 0.6f;
        ai.attackDelay = 0.22f;

        // Thêm tag để dễ nhận diện nếu cần
        root.tag = "Enemy";

        // Thiết lập Layer (Layer 8 là Enemy)
        root.layer = 8;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log($"<color=cyan>[Enemy Setup Tool]</color> Đã thiết lập thành công Plant Prefab tại: {path}");
    }
}
