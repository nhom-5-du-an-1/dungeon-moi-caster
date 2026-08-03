using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public class AutoFixSceneError : EditorWindow
{
    static AutoFixSceneError()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private static void OnHierarchyChanged()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        FixCurrentSceneSilent();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            FixCurrentSceneSilent();
        }
    }

    [MenuItem("Tools/🔧 TỰ ĐỘNG SỬA LỖI SCENE (Fix Missing Scripts & EventSystem)")]
    public static void FixCurrentSceneMenu()
    {
        FixCurrentSceneSilent(true);
    }

    public static void FixCurrentSceneSilent(bool verbose = false)
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) return;

        int totalMissingScriptsRemoved = 0;
        int duplicateEventSystemsRemoved = 0;
        bool sceneChanged = false;

        // 0. Đảm bảo các Tag và Layer cần thiết đã được đăng ký trong Project Settings
        EnsureTagsAndLayersRegistered();

        // 0.1. Tự động spawn Player nếu chưa có
        AutoSpawnPlayerIfMissing.CheckAndSpawnPlayer();
        
        // 0.2. Tự động dọn dẹp trùng lặp component PlayerStats trên Prefab gốc
        CleanDuplicatePlayerStatsOnPrefab();

        // 1. Đảm bảo Scene có Main Camera với CameraFollow
        EnsureSceneHasCamera();

        // 2. Quét toàn bộ GameObject trong Scene để xóa Missing Scripts
#if UNITY_2021_3_OR_NEWER
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
        EventSystem[] eventSystems = Object.FindObjectsOfType<EventSystem>(true);
#endif

        foreach (GameObject go in allObjects)
        {
            if (go == null) continue;
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (count > 0)
            {
                totalMissingScriptsRemoved += count;
                Debug.Log($"<color=yellow>[Auto Fix]</color> Đã tự động xóa {count} Component Missing Script trên đối tượng: <b>{go.name}</b>");
                EditorUtility.SetDirty(go);
            }
        }

        // 3. Xóa EventSystem trùng lặp nếu có từ 2 cái trở lên
        if (eventSystems != null && eventSystems.Length > 1)
        {
            for (int i = 1; i < eventSystems.Length; i++)
            {
                if (eventSystems[i] != null && eventSystems[i].gameObject != null)
                {
                    Debug.Log($"<color=yellow>[Auto Fix]</color> Đã tự động xóa EventSystem trùng lặp: <b>{eventSystems[i].gameObject.name}</b>");
                    DestroyImmediate(eventSystems[i].gameObject);
                    duplicateEventSystemsRemoved++;
                }
            }
        }

        // 4. Kiểm tra Tag & Layer cho Player và Enemies
        foreach (GameObject go in allObjects)
        {
            if (go == null) continue;

            // Xử lý Player
            if (go.GetComponent<PlayerMovement>() != null || go.GetComponent<PlayerStats>() != null)
            {
                if (!go.CompareTag("Player"))
                {
                    go.tag = "Player";
                    Debug.Log($"<color=yellow>[Auto Fix]</color> Đã tự động gắn Tag <b>'Player'</b> cho: {go.name}");
                    EditorUtility.SetDirty(go);
                    sceneChanged = true;
                }

                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer >= 0 && go.layer != playerLayer)
                {
                    SetLayerRecursive(go, playerLayer);
                    Debug.Log($"<color=yellow>[Auto Fix]</color> Đã tự động đặt Layer <b>'Player'</b> cho: {go.name} và các con");
                    sceneChanged = true;
                }
            }

            // Xử lý Enemy / Quái vật
            if (go.GetComponent<EnemyAI>() != null || 
                go.GetComponent<EnemyHealth>() != null || 
                go.GetComponent<OrcWarriorAI>() != null || 
                go.GetComponent<OrcBerserkerAI>() != null || 
                go.GetComponent<OrcShamanAI>() != null || 
                go.GetComponent<PlantAI>() != null ||
                go.GetComponent<BossOrcAI>() != null)
            {
                // Bỏ qua nếu là Player (phòng trường hợp hy hữu)
                if (go.GetComponent<PlayerMovement>() != null || go.GetComponent<PlayerStats>() != null)
                    continue;

                if (!go.CompareTag("Enemy"))
                {
                    go.tag = "Enemy";
                    Debug.Log($"<color=yellow>[Auto Fix]</color> Đã tự động gắn Tag <b>'Enemy'</b> cho: {go.name}");
                    EditorUtility.SetDirty(go);
                    sceneChanged = true;
                }

                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0 && go.layer != enemyLayer)
                {
                    SetLayerRecursive(go, enemyLayer);
                    Debug.Log($"<color=yellow>[Auto Fix]</color> Đã tự động đặt Layer <b>'Enemy'</b> cho: {go.name} và các con");
                    sceneChanged = true;
                }
            }
        }

        if (totalMissingScriptsRemoved > 0 || duplicateEventSystemsRemoved > 0 || sceneChanged)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }
        else if (verbose)
        {
            Debug.Log("<color=green><b>[Auto Fix]</b> Scene sạch sẽ!</color>");
        }
    }

    private static void EnsureTagsAndLayersRegistered()
    {
        var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAsset == null || tagManagerAsset.Length == 0) return;
        
        SerializedObject tagManager = new SerializedObject(tagManagerAsset[0]);
        if (tagManager == null) return;

        // Đảm bảo các Tag "Player" và "Enemy" tồn tại
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        if (tagsProp != null)
        {
            string[] requiredTags = { "Player", "Enemy" };
            foreach (string tag in requiredTags)
            {
                bool exists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                    Debug.Log($"<color=green>[Auto Fix]</color> Đã tự động đăng ký Tag mới: <b>{tag}</b>");
                }
            }
        }

        // Đảm bảo các Layer "Player" và "Enemy" tồn tại
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        if (layersProp != null)
        {
            string[] requiredLayers = { "Player", "Enemy" };
            foreach (string layer in requiredLayers)
            {
                bool exists = false;
                int emptyIndex = -1;
                for (int i = 8; i < layersProp.arraySize; i++)
                {
                    SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
                    if (layerProp.stringValue == layer)
                    {
                        exists = true;
                        break;
                    }
                    if (emptyIndex == -1 && string.IsNullOrEmpty(layerProp.stringValue))
                    {
                        emptyIndex = i;
                    }
                }
                if (!exists && emptyIndex != -1)
                {
                    SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(emptyIndex);
                    layerProp.stringValue = layer;
                    Debug.Log($"<color=green>[Auto Fix]</color> Đã tự động đăng ký Layer mới tại index {emptyIndex}: <b>{layer}</b>");
                }
            }
        }

        tagManager.ApplyModifiedProperties();
    }

    private static void SetLayerRecursive(GameObject obj, int layerIndex)
    {
        if (obj == null) return;
        obj.layer = layerIndex;
        EditorUtility.SetDirty(obj);
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layerIndex);
        }
    }

    /// <summary>
    /// Đảm bảo Scene có ít nhất 1 Main Camera hoạt động với CameraFollow.
    /// KHÔNG xóa Camera, KHÔNG di chuyển Camera làm con của Player.
    /// </summary>
    static void EnsureSceneHasCamera()
    {
        // Tìm Camera trong scene
        GameObject camObj = GameObject.FindWithTag("MainCamera");
        if (camObj == null) camObj = GameObject.Find("Main Camera");

        if (camObj == null)
        {
            // Không có Camera nào -> Tạo mới
            camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.22f, 0.15f, 1f);
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<DungeonCastle.CameraControl.CameraFollow>();
            camObj.transform.position = new Vector3(0f, 0f, -10f);
            Debug.Log("<color=green>[Auto Fix]</color> Đã tạo Main Camera mới cho Scene!");

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid()) EditorSceneManager.MarkSceneDirty(activeScene);
        }
        else
        {
            // Camera có rồi -> đảm bảo có CameraFollow
            if (camObj.GetComponent<DungeonCastle.CameraControl.CameraFollow>() == null)
            {
                camObj.AddComponent<DungeonCastle.CameraControl.CameraFollow>();
                Debug.Log("<color=green>[Auto Fix]</color> Đã gắn CameraFollow vào Main Camera!");
            }
            // Đảm bảo Camera KHÔNG phải con của Player (tách ra nếu cần)
            if (camObj.transform.parent != null)
            {
                camObj.transform.SetParent(null);
                Debug.Log("<color=yellow>[Auto Fix]</color> Đã tách Main Camera ra khỏi Player!");
            }
        }
    }

    /// <summary>
    /// Tìm và loại bỏ các component PlayerStats bị trùng lặp trên PlayerPrefab gốc để tránh lỗi tự hủy GameObject lúc khởi chạy.
    /// </summary>
    static void CleanDuplicatePlayerStatsOnPrefab()
    {
        string prefabPath = "Assets/prefab/Player/PlayerPrefab.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (playerPrefab != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(playerPrefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
            PlayerStats[] stats = prefabRoot.GetComponents<PlayerStats>();
            if (stats.Length > 1)
            {
                Debug.Log($"<color=yellow>[Auto Fix]</color> Phát hiện {stats.Length} component PlayerStats trên PlayerPrefab gốc! Đang tự động dọn dẹp...");
                // Giữ lại cái đầu tiên, xóa những cái sau
                for (int i = 1; i < stats.Length; i++)
                {
                    Object.DestroyImmediate(stats[i], true);
                }
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                Debug.Log("<color=green>[Auto Fix] Đã dọn dẹp xong component PlayerStats trùng lặp trên PlayerPrefab!</color>");
            }
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        // Đồng thời dọn dẹp trong Hierarchy Scene hiện tại
#if UNITY_2021_3_OR_NEWER
        PlayerStats[] sceneStats = Object.FindObjectsByType<PlayerStats>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        PlayerStats[] sceneStats = Object.FindObjectsOfType<PlayerStats>(true);
#endif
        foreach (var ps in sceneStats)
        {
            if (ps == null) continue;
            PlayerStats[] duplicates = ps.GetComponents<PlayerStats>();
            if (duplicates.Length > 1)
            {
                Debug.Log($"<color=yellow>[Auto Fix]</color> Phát hiện {duplicates.Length} component PlayerStats trên GameObject {ps.name} trong Scene! Đang dọn dẹp...");
                for (int i = 1; i < duplicates.Length; i++)
                {
                    DestroyImmediate(duplicates[i]);
                }
                EditorUtility.SetDirty(ps.gameObject);
                var activeScene = EditorSceneManager.GetActiveScene();
                if (activeScene.IsValid()) EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }
    }
}
