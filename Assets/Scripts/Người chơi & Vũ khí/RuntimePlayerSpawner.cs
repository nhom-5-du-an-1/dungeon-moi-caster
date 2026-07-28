using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Dự phòng Runtime: Tự động spawn Player nếu không tìm thấy khi game bắt đầu.
/// Cũng đảm bảo luôn có Camera hoạt động.
/// </summary>
public static class RuntimePlayerSpawner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnFirstSceneLoaded()
    {
        EnsureCameraExists();
        EnsurePlayerExists();
        SceneManager.sceneLoaded += OnNewSceneLoaded;
    }

    static void OnNewSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureCameraExists();
    }

    static void EnsureCameraExists()
    {
        // Kiểm tra xem có Camera nào đang hoạt động không
        if (Camera.main != null) return;

        Camera[] allCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allCams.Length > 0) return;

        // Không có Camera nào -> tạo mới
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.18f, 0.22f, 0.15f, 1f);
        camObj.AddComponent<AudioListener>();
        camObj.AddComponent<DungeonCastle.CameraControl.CameraFollow>();
        camObj.transform.position = new Vector3(0f, 0f, -10f);
        Object.DontDestroyOnLoad(camObj);
        Debug.Log("<color=green>[RuntimeSpawner]</color> Đã tạo Main Camera mới!");
    }

    static void EnsurePlayerExists()
    {
        // Nếu đã có Player (Singleton), không cần spawn
        if (PlayerStats.Instance != null && PlayerStats.Instance.gameObject.activeInHierarchy)
        {
            return;
        }

        // Tìm Player trong scene
        GameObject existingPlayer = GameObject.FindWithTag("Player");
        if (existingPlayer != null && existingPlayer.GetComponent<PlayerMovement>() != null)
        {
            return;
        }

        // Spawn từ Resources
        GameObject playerPrefab = Resources.Load<GameObject>("PlayerPrefab");
        if (playerPrefab != null)
        {
            GameObject spawnedPlayer = Object.Instantiate(playerPrefab);
            spawnedPlayer.name = "PlayerPrefab";
            spawnedPlayer.tag = "Player";
            spawnedPlayer.transform.position = Vector3.zero;
            Debug.Log("<color=green>[RuntimeSpawner]</color> Đã spawn Player!");
        }
    }
}
