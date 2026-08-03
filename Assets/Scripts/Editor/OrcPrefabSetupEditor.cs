using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Công cụ tự động thiết lập Prefab cho 3 Quái Quỷ Orc.
/// Tự động đọc Animation Clips từ file Aseprite đã import.
/// Truy cập từ menu: Dungeon Tools → Setup Orc Prefabs
/// </summary>
public class OrcPrefabSetupEditor : EditorWindow
{
    // Đường dẫn thư mục chứa sprite Aseprite
    private const string SPRITE_BASE_PATH = "Assets/tainguyen/quai/Orc";
    // Đường dẫn thư mục chứa Prefab xuất ra
    private const string PREFAB_BASE_PATH = "Assets/prefab/QUAI/orc";
    // Đường dẫn Projectile Prefab cho Shaman
    private const string PROJECTILE_PREFAB_PATH = "Assets/prefab/QUAI/MonsterProjectile.prefab";

    // Đường dẫn Aseprite cho từng quái
    private const string ORC1_ASEPRITE = SPRITE_BASE_PATH + "/quái quỷ 1.aseprite";
    private const string ORC2_ASEPRITE = SPRITE_BASE_PATH + "/quái quỷ 2.aseprite";
    private const string ORC3_ASEPRITE = SPRITE_BASE_PATH + "/quái quỷ 3.aseprite";

    // Tầm đánh tùy chỉnh
    private static float warriorAttackRange = 0.45f;
    private static float shamanShootRange = 5.0f;
    private static float berserkerAttackRange = 0.48f;

    private static void LoadSettings()
    {
        warriorAttackRange = EditorPrefs.GetFloat("OrcSetup_WarriorRange", 0.45f);
        shamanShootRange = EditorPrefs.GetFloat("OrcSetup_ShamanRange", 5.0f);
        berserkerAttackRange = EditorPrefs.GetFloat("OrcSetup_BerserkerRange", 0.48f);
    }

    private static void SaveSettings()
    {
        EditorPrefs.SetFloat("OrcSetup_WarriorRange", warriorAttackRange);
        EditorPrefs.SetFloat("OrcSetup_ShamanRange", shamanShootRange);
        EditorPrefs.SetFloat("OrcSetup_BerserkerRange", berserkerAttackRange);
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    [MenuItem("Dungeon Tools/Setup Orc Prefabs (Quái Quỷ 1-2-3)")]
    public static void ShowWindow()
    {
        var window = GetWindow<OrcPrefabSetupEditor>("Orc Setup Tool");
        window.minSize = new Vector2(450, 600);
    }

    private void OnGUI()
    {
        GUILayout.Space(8);

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        GUILayout.Label("⚔️ THIẾT LẬP QUÁI QUỶ ORC ⚔️", headerStyle);
        GUILayout.Space(4);

        EditorGUILayout.HelpBox(
            "Công cụ tự động tạo Prefab cho 3 Quái Quỷ Orc:\n\n" +
            "🗡️ Quái Quỷ 1 (OrcWarrior) - Chiến Binh: Chém + Húc Khiên\n" +
            "🔮 Quái Quỷ 2 (OrcShaman) - Pháp Sư: Bắn đạn phép + Hồi máu\n" +
            "🔥 Quái Quỷ 3 (OrcBerserker) - Cuồng Nộ: Chém + Lao Đâm + Rage\n\n" +
            "✨ TỰ ĐỘNG đọc Animation Clips từ file Aseprite!\n" +
            "✨ TỰ ĐỘNG tạo Animator Controller với transitions!\n" +
            "✨ TỰ ĐỘNG gắn sprite, collider, health, AI script!",
            MessageType.Info
        );

        GUILayout.Space(8);

        // Hiển thị trạng thái file Aseprite
        DrawAsepriteStatus("Quái Quỷ 1", ORC1_ASEPRITE);
        DrawAsepriteStatus("Quái Quỷ 2", ORC2_ASEPRITE);
        DrawAsepriteStatus("Quái Quỷ 3", ORC3_ASEPRITE);

        GUILayout.Space(4);
        EditorGUILayout.LabelField("🔧 CẤU HÌNH TẦM ĐÁNH (ATTACK RANGE)", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        warriorAttackRange = EditorGUILayout.FloatField("  🗡️ Tầm đánh Chiến Binh (Warrior)", warriorAttackRange);
        shamanShootRange = EditorGUILayout.FloatField("  🔮 Tầm bắn Pháp Sư (Shaman)", shamanShootRange);
        berserkerAttackRange = EditorGUILayout.FloatField("  🔥 Tầm đánh Cuồng Nộ (Berserker)", berserkerAttackRange);
        if (EditorGUI.EndChangeCheck())
        {
            SaveSettings();
        }

        GUILayout.Space(8);
        GUI.backgroundColor = new Color(0.2f, 0.6f, 1.0f);
        if (GUILayout.Button("💾 APPLY TẦM ĐÁNH LÊN PREFABS", GUILayout.Height(30)))
        {
            ApplyRangesToPrefabs();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(12);

        // Nút tạo tất cả
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
        if (GUILayout.Button("🚀 TẠO TẤT CẢ 3 QUÁI QUỶ ORC", GUILayout.Height(42)))
        {
            SetupAllOrcPrefabs();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(8);

        // Nút Debug Sub-assets
        if (GUILayout.Button("🔍 XEM CÁC HOẠT ẢNH TRONG ASEPRITE", GUILayout.Height(30)))
        {
            LogAsepriteSubAssets();
        }

        GUILayout.Space(8);
        EditorGUILayout.LabelField("── Hoặc tạo từng con ──", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(4);

        GUI.backgroundColor = new Color(1.0f, 0.85f, 0.3f);
        if (GUILayout.Button("🗡️ Tạo Quái Quỷ 1 - Orc Chiến Binh", GUILayout.Height(30)))
        {
            SetupOrcWarrior();
            FinalSave();
        }

        GUI.backgroundColor = new Color(0.6f, 0.4f, 1.0f);
        if (GUILayout.Button("🔮 Tạo Quái Quỷ 2 - Orc Pháp Sư", GUILayout.Height(30)))
        {
            SetupOrcShaman();
            FinalSave();
        }

        GUI.backgroundColor = new Color(1.0f, 0.3f, 0.2f);
        if (GUILayout.Button("🔥 Tạo Quái Quỷ 3 - Orc Cuồng Nộ", GUILayout.Height(30)))
        {
            SetupOrcBerserker();
            FinalSave();
        }
        GUI.backgroundColor = Color.white;
    }

    private void DrawAsepriteStatus(string label, string path)
    {
        bool exists = File.Exists(path);
        var clips = exists ? LoadAnimClipsFromAseprite(path) : new List<AnimationClip>();
        var sprites = exists ? LoadSpritesFromAseprite(path) : new List<Sprite>();

        EditorGUILayout.BeginHorizontal();
        string icon = exists ? "✅" : "❌";
        string status = exists ? $"{sprites.Count} sprites, {clips.Count} anim clips" : "KHÔNG TÌM THẤY";
        EditorGUILayout.LabelField($"  {icon} {label}: {status}");
        EditorGUILayout.EndHorizontal();
    }

    // ==========================
    // TẠO TẤT CẢ 3 ORC
    // ==========================
    [MenuItem("Dungeon Tools/Setup Orc Prefabs (Quái Quỷ 1-2-3) _F7", priority = 100)]
    public static void SetupAllOrcPrefabs()
    {
        Debug.Log("<color=yellow>[Orc Setup]</color> ═══════════════════════════════════════");
        Debug.Log("<color=yellow>[Orc Setup]</color> Bắt đầu thiết lập 3 Quái Quỷ Orc...");

        EnsureDirectories();

        SetupOrcWarrior();
        SetupOrcShaman();
        SetupOrcBerserker();

        FinalSave();

        Debug.Log("<color=green>[Orc Setup]</color> ✅ Hoàn tất thiết lập 3 Quái Quỷ Orc!");
        Debug.Log("<color=green>[Orc Setup]</color> ═══════════════════════════════════════");
        EditorUtility.DisplayDialog("Thành công!", "Đã tạo xong 3 Prefab Quái Quỷ Orc!\n\nĐường dẫn: " + PREFAB_BASE_PATH + "\n\nMỗi Prefab đã có đầy đủ:\n• Sprite + Animation Clips\n• Animator Controller\n• Collider + Rigidbody2D\n• EnemyHealth + AI Script", "OK");
    }

    // ==========================
    // QUÁI QUỶ 1: ORC CHIẾN BINH
    // ==========================
    public static void SetupOrcWarrior()
    {
        EnsureDirectories();
        string prefabPath = PREFAB_BASE_PATH + "/OrcWarrior.prefab";

        // Load assets từ Aseprite
        List<Sprite> sprites = LoadSpritesFromAseprite(ORC1_ASEPRITE);
        List<AnimationClip> clips = LoadAnimClipsFromAseprite(ORC1_ASEPRITE);
        Sprite mainSprite = sprites.Count > 0 ? sprites[0] : null;

        Debug.Log($"<color=yellow>[Orc Setup]</color> 🗡️ Quái Quỷ 1: Tìm thấy {sprites.Count} sprites, {clips.Count} animation clips");

        GameObject root = CreateOrLoadPrefab(prefabPath, "OrcWarrior");

        // SpriteRenderer
        SpriteRenderer sr = EnsureComponent<SpriteRenderer>(root);
        if (mainSprite != null) sr.sprite = mainSprite;
        sr.sortingOrder = 10;

        // Rigidbody2D
        Rigidbody2D rb = EnsureComponent<Rigidbody2D>(root);
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Collider2D
        CapsuleCollider2D col = EnsureComponent<CapsuleCollider2D>(root);
        col.isTrigger = false;
        col.offset = new Vector2(0f, 0.10f);
        col.size = new Vector2(0.50f, 0.65f);
        col.direction = CapsuleDirection2D.Vertical;

        // Animator Controller với clips từ Aseprite
        Animator anim = EnsureComponent<Animator>(root);
        AnimatorController controller = BuildAnimatorController("OrcWarrior", clips, sprites);
        if (controller != null) anim.runtimeAnimatorController = controller;

        // EnemyHealth
        EnemyHealth health = EnsureComponent<EnemyHealth>(root);
        health.maxHealth = 14;
        health.currentHealth = 14;
        health.useHeartDisplay = true;
        health.heartCount = 7;
        health.alwaysShowHealthBar = true;
        health.barHeightOffset = 0.08f;
        health.barWidth = 0.32f;
        health.barHeight = 0.04f;
        LoadHeartSprites(health);

        // OrcWarriorAI
        LoadSettings();
        OrcWarriorAI ai = EnsureComponent<OrcWarriorAI>(root);
        ai.detectionRadius = 5.5f;
        ai.loseSightRadius = 9.0f;
        ai.moveSpeed = 2.8f;
        ai.patrolSpeed = 1.2f;
        ai.slashDamage = 3;
        ai.slashCooldown = 1.2f;
        ai.slashDelay = 0.28f;
        ai.slashDuration = 0.45f;
        ai.attackRange = warriorAttackRange;
        ai.bashDamage = 2;
        ai.bashCooldown = 5.0f;
        ai.bashKnockback = 7.0f;
        ai.playerLayer = LayerMask.GetMask("Player");

        root.tag = "Enemy";
        SetLayerRecursive(root, "Enemy");

        SavePrefab(root, prefabPath);
        LogClipInfo("Quái Quỷ 1 - OrcWarrior", clips, prefabPath);
    }

    // ==========================
    // QUÁI QUỶ 2: ORC PHÁP SƯ
    // ==========================
    public static void SetupOrcShaman()
    {
        EnsureDirectories();
        string prefabPath = PREFAB_BASE_PATH + "/OrcShaman.prefab";

        List<Sprite> sprites = LoadSpritesFromAseprite(ORC2_ASEPRITE);
        List<AnimationClip> clips = LoadAnimClipsFromAseprite(ORC2_ASEPRITE);
        Sprite mainSprite = sprites.Count > 0 ? sprites[0] : null;

        Debug.Log($"<color=magenta>[Orc Setup]</color> 🔮 Quái Quỷ 2: Tìm thấy {sprites.Count} sprites, {clips.Count} animation clips");

        GameObject root = CreateOrLoadPrefab(prefabPath, "OrcShaman");

        SpriteRenderer sr = EnsureComponent<SpriteRenderer>(root);
        if (mainSprite != null) sr.sprite = mainSprite;
        sr.sortingOrder = 10;

        Rigidbody2D rb = EnsureComponent<Rigidbody2D>(root);
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        CapsuleCollider2D col = EnsureComponent<CapsuleCollider2D>(root);
        col.isTrigger = false;
        col.offset = new Vector2(0f, 0.10f);
        col.size = new Vector2(0.45f, 0.60f);
        col.direction = CapsuleDirection2D.Vertical;

        Animator anim = EnsureComponent<Animator>(root);
        AnimatorController controller = BuildAnimatorController("OrcShaman", clips, sprites);
        if (controller != null) anim.runtimeAnimatorController = controller;

        EnemyHealth health = EnsureComponent<EnemyHealth>(root);
        health.maxHealth = 10;
        health.currentHealth = 10;
        health.useHeartDisplay = true;
        health.heartCount = 5;
        health.alwaysShowHealthBar = true;
        health.barHeightOffset = 0.08f;
        health.barWidth = 0.28f;
        health.barHeight = 0.04f;
        LoadHeartSprites(health);

        LoadSettings();
        OrcShamanAI ai = EnsureComponent<OrcShamanAI>(root);
        ai.detectionRadius = 7.0f;
        ai.loseSightRadius = 10.0f;
        ai.moveSpeed = 2.0f;
        ai.patrolSpeed = 0.9f;
        ai.retreatSpeed = 2.5f;
        ai.shootRange = shamanShootRange;
        ai.safeDistance = 2.5f;
        ai.elementType = MonsterProjectile.ElementType.Fire;
        ai.projectileDamage = 3;
        ai.projectileSpeed = 5.5f;
        ai.shootCooldown = 2.0f;
        ai.shootDuration = 0.85f;
        ai.shootDelay = 0.45f;
        ai.burstCount = 2;
        ai.healRadius = 3.5f;
        ai.healAmount = 4;
        ai.healCooldown = 10.0f;

        // Gắn Projectile Prefab
        GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PROJECTILE_PREFAB_PATH);
        if (projectilePrefab != null)
        {
            ai.projectilePrefab = projectilePrefab;
        }
        else
        {
            Debug.LogWarning("<color=orange>[Orc Setup]</color> Chưa có MonsterProjectile.prefab! Hãy chạy 'Dungeon Tools → Setup Enemy Prefabs' trước.");
        }

        root.tag = "Enemy";
        SetLayerRecursive(root, "Enemy");

        SavePrefab(root, prefabPath);
        LogClipInfo("Quái Quỷ 2 - OrcShaman", clips, prefabPath);
    }

    // ==========================
    // QUÁI QUỶ 3: ORC CUỒNG NỘ
    // ==========================
    public static void SetupOrcBerserker()
    {
        EnsureDirectories();
        string prefabPath = PREFAB_BASE_PATH + "/OrcBerserker.prefab";

        List<Sprite> sprites = LoadSpritesFromAseprite(ORC3_ASEPRITE);
        List<AnimationClip> clips = LoadAnimClipsFromAseprite(ORC3_ASEPRITE);
        Sprite mainSprite = sprites.Count > 0 ? sprites[0] : null;

        Debug.Log($"<color=red>[Orc Setup]</color> 🔥 Quái Quỷ 3: Tìm thấy {sprites.Count} sprites, {clips.Count} animation clips");

        GameObject root = CreateOrLoadPrefab(prefabPath, "OrcBerserker");

        SpriteRenderer sr = EnsureComponent<SpriteRenderer>(root);
        if (mainSprite != null) sr.sprite = mainSprite;
        sr.sortingOrder = 10;

        Rigidbody2D rb = EnsureComponent<Rigidbody2D>(root);
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        CapsuleCollider2D col = EnsureComponent<CapsuleCollider2D>(root);
        col.isTrigger = false;
        col.offset = new Vector2(0f, 0.12f);
        col.size = new Vector2(0.55f, 0.70f);
        col.direction = CapsuleDirection2D.Vertical;

        Animator anim = EnsureComponent<Animator>(root);
        AnimatorController controller = BuildAnimatorController("OrcBerserker", clips, sprites);
        if (controller != null) anim.runtimeAnimatorController = controller;

        EnemyHealth health = EnsureComponent<EnemyHealth>(root);
        health.maxHealth = 20;
        health.currentHealth = 20;
        health.useHeartDisplay = true;
        health.heartCount = 10;
        health.alwaysShowHealthBar = true;
        health.barHeightOffset = 0.08f;
        health.barWidth = 0.36f;
        health.barHeight = 0.04f;
        LoadHeartSprites(health);

        LoadSettings();
        OrcBerserkerAI ai = EnsureComponent<OrcBerserkerAI>(root);
        ai.detectionRadius = 6.0f;
        ai.loseSightRadius = 10.0f;
        ai.moveSpeed = 3.0f;
        ai.patrolSpeed = 1.3f;
        ai.slashDamage = 3;
        ai.slashCooldown = 0.9f;
        ai.slashDelay = 0.25f;
        ai.slashDuration = 0.40f;
        ai.attackRange = berserkerAttackRange;
        ai.chargeDamage = 5;
        ai.chargeSpeed = 10.0f;
        ai.chargeCooldown = 6.0f;
        ai.chargeKnockback = 8.0f;
        ai.maxRageSpeedMultiplier = 1.8f;
        ai.maxRageDamageMultiplier = 2.0f;
        ai.rageStartThreshold = 0.75f;
        ai.playerLayer = LayerMask.GetMask("Player");

        root.tag = "Enemy";
        SetLayerRecursive(root, "Enemy");

        SavePrefab(root, prefabPath);
        LogClipInfo("Quái Quỷ 3 - OrcBerserker", clips, prefabPath);
    }

    // =========================================
    // LOAD ANIMATION CLIPS TỪ ASEPRITE
    // =========================================

    /// <summary>
    /// Đọc tất cả AnimationClip sub-assets từ file Aseprite đã import
    /// </summary>
    private static List<AnimationClip> LoadAnimClipsFromAseprite(string asepritePath)
    {
        List<AnimationClip> clips = new List<AnimationClip>();
        if (!File.Exists(asepritePath)) return clips;

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(asepritePath);
        if (subAssets == null) return clips;

        foreach (Object asset in subAssets)
        {
            if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                clips.Add(clip);
            }
        }

        // Sắp xếp theo tên để có thứ tự nhất quán
        clips.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        return clips;
    }

    /// <summary>
    /// Đọc tất cả Sprite sub-assets từ file Aseprite đã import
    /// </summary>
    private static List<Sprite> LoadSpritesFromAseprite(string asepritePath)
    {
        List<Sprite> sprites = new List<Sprite>();
        if (!File.Exists(asepritePath)) return sprites;

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(asepritePath);
        if (subAssets == null) return sprites;

        foreach (Object asset in subAssets)
        {
            if (asset is Sprite sprite)
            {
                sprites.Add(sprite);
            }
        }

        // Sắp xếp theo tên frame
        sprites.Sort((a, b) =>
        {
            // Cố gắng sắp xếp theo số frame: Frame_0, Frame_1, ..., Frame_10, ...
            int numA = ExtractFrameNumber(a.name);
            int numB = ExtractFrameNumber(b.name);
            if (numA >= 0 && numB >= 0) return numA.CompareTo(numB);
            return string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase);
        });

        return sprites;
    }

    private static int ExtractFrameNumber(string name)
    {
        // Tách số từ tên frame: "Frame_0" → 0, "Frame_12" → 12
        string numStr = "";
        bool foundDigit = false;
        for (int i = name.Length - 1; i >= 0; i--)
        {
            if (char.IsDigit(name[i]))
            {
                numStr = name[i] + numStr;
                foundDigit = true;
            }
            else if (foundDigit) break;
        }
        if (int.TryParse(numStr, out int num)) return num;
        return -1;
    }

    // =========================================
    // TẠO ANIMATOR CONTROLLER TỪ CLIPS
    // =========================================

    /// <summary>
    /// Tạo Animator Controller hoàn chỉnh từ các AnimationClip đã load.
    /// Tự động phân loại clips theo tên (idle, walk/run, attack, hurt, death).
    /// Nếu Aseprite chỉ có 1 clip (tất cả frames gộp), tự tách thành các animation riêng.
    /// </summary>
    private static AnimatorController BuildAnimatorController(string name, List<AnimationClip> clips, List<Sprite> sprites)
    {
        string controllerPath = $"{PREFAB_BASE_PATH}/{name}_AnimController.controller";

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }
        else
        {
            // Xóa tất cả parameters hiện tại
            while (controller.parameters.Length > 0)
            {
                controller.RemoveParameter(0);
            }

            // Xóa tất cả layers hiện tại
            while (controller.layers.Length > 0)
            {
                controller.RemoveLayer(0);
            }

            // Tạo Base Layer mặc định
            controller.AddLayer("Base Layer");
        }

        // Thêm parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack1", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack2", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Phân loại clips theo tên
        AnimationClip idleClip = null, walkClip = null;
        AnimationClip attack1Clip = null, attack2Clip = null;
        AnimationClip hurtClip = null, deathClip = null;
        List<AnimationClip> unclassified = new List<AnimationClip>();

        foreach (AnimationClip clip in clips)
        {
            string lower = clip.name.ToLowerInvariant();

            if (lower.Contains("idle") || lower.Contains("stand") || lower.Contains("dung") || lower.Contains("dungyen"))
            {
                idleClip = clip;
            }
            else if (lower.Contains("walk") || lower.Contains("run") || lower.Contains("move") || lower.Contains("di") || lower.Contains("chay") || lower.Contains("dichuyen"))
            {
                walkClip = clip;
            }
            else if (lower.Contains("attack2") || lower.Contains("atk2") || lower.Contains("skill") || lower.Contains("special") || lower.Contains("chieu") || lower.Contains("khien") || lower.Contains("phép") || lower.Contains("phep") || lower.Contains("nam") || lower.Contains("aoe") || lower.Contains("huc") || lower.Contains("lamcham") || lower.Contains("hoimau") || lower.Contains("laodam"))
            {
                attack2Clip = clip;
            }
            else if (lower.Contains("attack") || lower.Contains("atk") || lower.Contains("hit") || lower.Contains("slash") || lower.Contains("chem") || lower.Contains("ban") || lower.Contains("tancong") || lower.Contains("danh") || lower.Contains("chuong"))
            {
                if (attack1Clip == null) attack1Clip = clip;
                else if (attack2Clip == null) attack2Clip = clip;
            }
            else if (lower.Contains("hurt") || lower.Contains("damage") || lower.Contains("pain") || lower.Contains("bi_thuong") || lower.Contains("bithuong") || lower.Contains("dau"))
            {
                hurtClip = clip;
            }
            else if (lower.Contains("death") || lower.Contains("die") || lower.Contains("dead") || lower.Contains("chet") || lower.Contains("guc"))
            {
                deathClip = clip;
            }
            else
            {
                unclassified.Add(clip);
            }
        }

        // Nếu Aseprite không có tag (1 clip duy nhất hoặc không clip):
        // Tự tạo clips từ sprites (chia đều frame)
        if (clips.Count <= 1 && sprites.Count >= 4)
        {
            Debug.Log($"<color=cyan>[Orc Setup]</color> {name}: Aseprite không có tag riêng, tự tạo animation clips từ {sprites.Count} frames...");

            // Tính frame phân bổ hợp lý dựa trên tổng số frame
            int totalFrames = sprites.Count;

            // Phân bổ frame cho từng animation:
            // Idle: ~7 frames đầu (đứng yên nhấp nhả)
            // Walk: ~7 frames tiếp (đi bộ)
            // Attack1: ~8 frames tiếp (tấn công 1)
            // Attack2: ~8 frames tiếp (tấn công 2)
            // Hurt: ~4 frames tiếp (bị đánh)
            // Death: ~4 frames cuối (chết)

            int idleFrames = Mathf.Min(7, totalFrames);
            int walkFrames = Mathf.Min(7, totalFrames - idleFrames);
            int attackFrames = Mathf.Min(8, totalFrames - idleFrames - walkFrames);
            int attack2Frames = Mathf.Min(8, totalFrames - idleFrames - walkFrames - attackFrames);
            int hurtFrames = Mathf.Min(4, totalFrames - idleFrames - walkFrames - attackFrames - attack2Frames);
            int deathFrames = totalFrames - idleFrames - walkFrames - attackFrames - attack2Frames - hurtFrames;

            int startIdx = 0;

            if (idleFrames > 0)
            {
                idleClip = CreateClipFromSprites($"{name}_Idle", sprites, startIdx, idleFrames, 8f, true);
                startIdx += idleFrames;
            }
            if (walkFrames > 0)
            {
                walkClip = CreateClipFromSprites($"{name}_Walk", sprites, startIdx, walkFrames, 10f, true);
                startIdx += walkFrames;
            }
            if (attackFrames > 0)
            {
                attack1Clip = CreateClipFromSprites($"{name}_Attack1", sprites, startIdx, attackFrames, 12f, false);
                startIdx += attackFrames;
            }
            if (attack2Frames > 0)
            {
                attack2Clip = CreateClipFromSprites($"{name}_Attack2", sprites, startIdx, attack2Frames, 12f, false);
                startIdx += attack2Frames;
            }
            if (hurtFrames > 0)
            {
                hurtClip = CreateClipFromSprites($"{name}_Hurt", sprites, startIdx, hurtFrames, 10f, false);
                startIdx += hurtFrames;
            }
            if (deathFrames > 0)
            {
                deathClip = CreateClipFromSprites($"{name}_Death", sprites, startIdx, deathFrames, 8f, false);
            }
        }
        else if (clips.Count > 1)
        {
            // Nếu có nhiều clips mà một số vẫn chưa phân loại được, gán tự động theo thứ tự
            if (idleClip == null && unclassified.Count > 0) { idleClip = unclassified[0]; unclassified.RemoveAt(0); }
            if (walkClip == null && unclassified.Count > 0) { walkClip = unclassified[0]; unclassified.RemoveAt(0); }
            if (attack1Clip == null && unclassified.Count > 0) { attack1Clip = unclassified[0]; unclassified.RemoveAt(0); }
            if (attack2Clip == null && unclassified.Count > 0) { attack2Clip = unclassified[0]; unclassified.RemoveAt(0); }
            if (hurtClip == null && unclassified.Count > 0) { hurtClip = unclassified[0]; unclassified.RemoveAt(0); }
            if (deathClip == null && unclassified.Count > 0) { deathClip = unclassified[0]; unclassified.RemoveAt(0); }
        }

        // ===== TẠO STATES =====

        // State Idle (mặc định)
        AnimatorState idleState = rootStateMachine.AddState("Idle");
        rootStateMachine.defaultState = idleState;
        if (idleClip != null) idleState.motion = idleClip;

        // State Walk
        AnimatorState walkState = rootStateMachine.AddState("Walk");
        if (walkClip != null) walkState.motion = walkClip;

        // State Attack1
        AnimatorState attack1State = rootStateMachine.AddState("Attack1");
        if (attack1Clip != null) attack1State.motion = attack1Clip;

        // State Attack2
        AnimatorState attack2State = rootStateMachine.AddState("Attack2");
        if (attack2Clip != null) attack2State.motion = attack2Clip;

        // State Hurt
        AnimatorState hurtState = rootStateMachine.AddState("Hurt");
        if (hurtClip != null) hurtState.motion = hurtClip;

        // State Death
        AnimatorState deathState = rootStateMachine.AddState("Death");
        if (deathClip != null) deathState.motion = deathClip;

        // ===== TẠO TRANSITIONS =====

        // Idle ↔ Walk
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0f;

        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0f;

        // Any State → Attack1
        var anyToAttack1 = rootStateMachine.AddAnyStateTransition(attack1State);
        anyToAttack1.AddCondition(AnimatorConditionMode.If, 0, "Attack1");
        anyToAttack1.hasExitTime = false;
        anyToAttack1.duration = 0f;
        anyToAttack1.canTransitionToSelf = false;

        // Attack1 → Idle
        var attack1ToIdle = attack1State.AddTransition(idleState);
        attack1ToIdle.hasExitTime = true;
        attack1ToIdle.exitTime = 1.0f;
        attack1ToIdle.duration = 0f;

        // Any State → Attack2
        var anyToAttack2 = rootStateMachine.AddAnyStateTransition(attack2State);
        anyToAttack2.AddCondition(AnimatorConditionMode.If, 0, "Attack2");
        anyToAttack2.hasExitTime = false;
        anyToAttack2.duration = 0f;
        anyToAttack2.canTransitionToSelf = false;

        // Attack2 → Idle
        var attack2ToIdle = attack2State.AddTransition(idleState);
        attack2ToIdle.hasExitTime = true;
        attack2ToIdle.exitTime = 1.0f;
        attack2ToIdle.duration = 0f;

        // Any State → Hurt
        var anyToHurt = rootStateMachine.AddAnyStateTransition(hurtState);
        anyToHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        anyToHurt.hasExitTime = false;
        anyToHurt.duration = 0f;
        anyToHurt.canTransitionToSelf = false;

        // Hurt → Idle
        var hurtToIdle = hurtState.AddTransition(idleState);
        hurtToIdle.hasExitTime = true;
        hurtToIdle.exitTime = 1.0f;
        hurtToIdle.duration = 0f;

        // Any State → Death
        var anyToDeath = rootStateMachine.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");
        anyToDeath.hasExitTime = false;
        anyToDeath.duration = 0f;
        anyToDeath.canTransitionToSelf = false;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);

        Debug.Log($"<color=cyan>[Orc Setup]</color> Animator Controller tạo tại: {controllerPath}");
        Debug.Log($"  Idle: {(idleClip != null ? idleClip.name : "TRỐNG")}");
        Debug.Log($"  Walk: {(walkClip != null ? walkClip.name : "TRỐNG")}");
        Debug.Log($"  Attack1: {(attack1Clip != null ? attack1Clip.name : "TRỐNG")}");
        Debug.Log($"  Attack2: {(attack2Clip != null ? attack2Clip.name : "TRỐNG")}");
        Debug.Log($"  Hurt: {(hurtClip != null ? hurtClip.name : "TRỐNG")}");
        Debug.Log($"  Death: {(deathClip != null ? deathClip.name : "TRỐNG")}");

        return controller;
    }

    /// <summary>
    /// Tạo AnimationClip từ danh sách sprites (khi Aseprite không có tag)
    /// </summary>
    private static AnimationClip CreateClipFromSprites(string clipName, List<Sprite> allSprites, int startIndex, int frameCount, float sampleRate, bool isLooping)
    {
        string clipPath = $"{PREFAB_BASE_PATH}/{clipName}.anim";

        AnimationClip clip = new AnimationClip();
        clip.name = clipName;
        clip.frameRate = sampleRate;

        // Tạo keyframes cho SpriteRenderer.sprite
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            int spriteIndex = Mathf.Min(startIndex + i, allSprites.Count - 1);
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / sampleRate,
                value = allSprites[spriteIndex]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        // Cài đặt loop
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = isLooping;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    // =========================================
    // HÀM TIỆN ÍCH DÙNG CHUNG
    // =========================================

    private static void EnsureDirectories()
    {
        if (!Directory.Exists(PREFAB_BASE_PATH))
        {
            Directory.CreateDirectory(PREFAB_BASE_PATH);
            AssetDatabase.Refresh();
        }
    }

    private static GameObject CreateOrLoadPrefab(string prefabPath, string name)
    {
        if (File.Exists(prefabPath))
        {
            return PrefabUtility.LoadPrefabContents(prefabPath);
        }
        return new GameObject(name);
    }

    private static void SavePrefab(GameObject root, string prefabPath)
    {
        bool isNew = !File.Exists(prefabPath);
        if (isNew)
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }
        else
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void FinalSave()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }

    private static void LoadHeartSprites(EnemyHealth health)
    {
        health.enemyHeartFull = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_enemy.png");
        if (health.enemyHeartFull == null)
            health.enemyHeartFull = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_full.png");
        health.enemyHeartHalf = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_half.png");
        health.enemyHeartEmpty = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/tainguyen/item/heart_empty.png");
    }

    private static void SetLayerRecursive(GameObject obj, string layerName)
    {
        int layerIndex = LayerMask.NameToLayer(layerName);
        if (layerIndex < 0)
        {
            Debug.LogWarning($"<color=orange>[Orc Setup]</color> Layer '{layerName}' không tồn tại! Hãy tạo trong Project Settings → Tags and Layers.");
            return;
        }
        obj.layer = layerIndex;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layerName);
        }
    }

    private static void LogClipInfo(string name, List<AnimationClip> clips, string prefabPath)
    {
        string clipNames = clips.Count > 0 ? string.Join(", ", clips.Select(c => c.name)) : "Tự tạo từ frames";
        Debug.Log($"<color=green>[Orc Setup]</color> ✅ {name} đã tạo tại: {prefabPath} | Clips: [{clipNames}]");
    }

    private static void LogAsepriteSubAssets()
    {
        LogAsepriteFile(ORC1_ASEPRITE, "Quái Quỷ 1");
        LogAsepriteFile(ORC2_ASEPRITE, "Quái Quỷ 2");
        LogAsepriteFile(ORC3_ASEPRITE, "Quái Quỷ 3");
    }

    private static void LogAsepriteFile(string path, string label)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"[Orc Setup Debug] {label}: Không tìm thấy file tại {path}");
            return;
        }

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        Debug.Log($"<color=cyan>[Orc Setup Debug] ======= {label} Sub-Assets ({subAssets.Length} items) =======</color>");
        
        int spriteCount = 0;
        int clipCount = 0;
        
        foreach (var asset in subAssets)
        {
            if (asset is Sprite)
            {
                spriteCount++;
                Debug.Log($"   [Sprite] Name: {asset.name}");
            }
            else if (asset is AnimationClip)
            {
                clipCount++;
                Debug.Log($"   <color=yellow>[AnimationClip]</color> Name: {asset.name}");
            }
        }
        Debug.Log($"<color=cyan>[Orc Setup Debug] {label} summary: {spriteCount} Sprites, {clipCount} AnimationClips</color>");
    }

    private static void ApplyRangesToPrefabs()
    {
        LoadSettings();
        
        // 1. Quái 1 (Warrior)
        string path1 = PREFAB_BASE_PATH + "/OrcWarrior.prefab";
        GameObject root1 = AssetDatabase.LoadAssetAtPath<GameObject>(path1);
        if (root1 != null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(root1) as GameObject;
            if (instance != null)
            {
                OrcWarriorAI ai = instance.GetComponent<OrcWarriorAI>();
                if (ai != null)
                {
                    ai.attackRange = warriorAttackRange;
                    PrefabUtility.SaveAsPrefabAsset(instance, path1);
                    Debug.Log($"<color=green>[Orc Setup]</color> Đã áp dụng tầm đánh {warriorAttackRange} lên {path1}");
                }
                DestroyImmediate(instance);
            }
        }
        else
        {
            Debug.LogWarning($"[Orc Setup] Không tìm thấy Prefab tại {path1} để apply.");
        }

        // 2. Quái 2 (Shaman)
        string path2 = PREFAB_BASE_PATH + "/OrcShaman.prefab";
        GameObject root2 = AssetDatabase.LoadAssetAtPath<GameObject>(path2);
        if (root2 != null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(root2) as GameObject;
            if (instance != null)
            {
                OrcShamanAI ai = instance.GetComponent<OrcShamanAI>();
                if (ai != null)
                {
                    ai.shootRange = shamanShootRange;
                    PrefabUtility.SaveAsPrefabAsset(instance, path2);
                    Debug.Log($"<color=green>[Orc Setup]</color> Đã áp dụng tầm bắn {shamanShootRange} lên {path2}");
                }
                DestroyImmediate(instance);
            }
        }
        else
        {
            Debug.LogWarning($"[Orc Setup] Không tìm thấy Prefab tại {path2} để apply.");
        }

        // 3. Quái 3 (Berserker)
        string path3 = PREFAB_BASE_PATH + "/OrcBerserker.prefab";
        GameObject root3 = AssetDatabase.LoadAssetAtPath<GameObject>(path3);
        if (root3 != null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(root3) as GameObject;
            if (instance != null)
            {
                OrcBerserkerAI ai = instance.GetComponent<OrcBerserkerAI>();
                if (ai != null)
                {
                    ai.attackRange = berserkerAttackRange;
                    PrefabUtility.SaveAsPrefabAsset(instance, path3);
                    Debug.Log($"<color=green>[Orc Setup]</color> Đã áp dụng tầm đánh {berserkerAttackRange} lên {path3}");
                }
                DestroyImmediate(instance);
            }
        }
        else
        {
            Debug.LogWarning($"[Orc Setup] Không tìm thấy Prefab tại {path3} để apply.");
        }
        
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Thành công!", "Đã áp dụng (Apply) các thông số tầm đánh mới trực tiếp vào Prefabs!", "OK");
    }
}
