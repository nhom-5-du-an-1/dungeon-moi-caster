using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class AutoSpawnPlayerIfMissing : EditorWindow
{
    static AutoSpawnPlayerIfMissing()
    {
        // Chạy khi Editor khởi động
        EditorApplication.delayCall += CheckAndSpawnPlayer;
        // Chạy khi mở scene mới
        EditorSceneManager.sceneOpened += OnSceneOpened;
        // Chạy NGAY TRƯỚC khi bấm Play -> đảm bảo Player luôn có mặt
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += CheckAndSpawnPlayer;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // ExitingEditMode = đang chuẩn bị chuyển sang Play -> spawn Player trước khi Play bắt đầu
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            CheckAndSpawnPlayer();
        }
    }

    [MenuItem("Tools/👤 Tự Động Thay Square Bằng Player (Auto Spawn Player)")]
    public static void CheckAndSpawnPlayer()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode == false && EditorApplication.isPlaying) return;
        if (EditorApplication.isCompiling) return;
        // Cho phép chạy khi ExitingEditMode (isPlayingOrWillChangePlaymode = true nhưng isPlaying = false)
        // Chặn khi đã thực sự đang Play
        if (EditorApplication.isPlaying) return;

        // 1. Kiểm tra xem Scene có Player chưa
        GameObject existingPlayer = GameObject.FindWithTag("Player");
        if (existingPlayer == null) existingPlayer = GameObject.Find("PlayerPrefab");
        if (existingPlayer == null) existingPlayer = GameObject.Find("DarkFantasyPlayer");

        // Kiểm tra Player tìm được có thực sự là Player (có PlayerMovement hoặc PlayerStats)
        if (existingPlayer != null)
        {
            bool isRealPlayer = existingPlayer.GetComponent<PlayerMovement>() != null ||
                                existingPlayer.GetComponent<PlayerStats>() != null;
            if (isRealPlayer) return; // Đã có Player thật -> không cần spawn thêm
        }

        // Kiểm tra xem có ô vuông "Square" thừa không -> Xóa Square
        GameObject square = GameObject.Find("Square");
        if (square != null)
        {
            DestroyImmediate(square);
            Debug.Log("<color=yellow>[Auto Fix]</color> Đã tự động xóa ô vuông 'Square' thừa trong Scene!");
        }

        // Tải PlayerPrefab từ Assets/prefab/Player/PlayerPrefab.prefab
        string playerPrefabPath = "Assets/prefab/Player/PlayerPrefab.prefab";
        GameObject playerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);

        if (playerAsset != null)
        {
            GameObject spawnedPlayer = PrefabUtility.InstantiatePrefab(playerAsset) as GameObject;
            if (spawnedPlayer != null)
            {
                spawnedPlayer.transform.position = Vector3.zero;
                spawnedPlayer.name = "PlayerPrefab";
                spawnedPlayer.tag = "Player";
                Debug.Log($"<color=green><b>[THÀNH CÔNG]</b> Đã tự động đưa nhân vật Player vào Scene!</color>");

                if (!EditorApplication.isPlaying)
                {
                    var activeScene = EditorSceneManager.GetActiveScene();
                    if (activeScene.IsValid())
                    {
                        EditorSceneManager.MarkSceneDirty(activeScene);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"[Auto Fix] Không tìm thấy Prefab nhân vật tại: {playerPrefabPath}");
        }
    }
}
