using UnityEditor;
using UnityEngine;

/// <summary>
/// Đảm bảo PlayerPrefab luôn có Tag "Player" và các component cần thiết.
/// KHÔNG gắn Camera vào trong Prefab (Camera là object riêng trong Scene).
/// </summary>
[InitializeOnLoad]
public class AutoSetupPlayerPrefabWithCamera : EditorWindow
{
    static AutoSetupPlayerPrefabWithCamera()
    {
        EditorApplication.delayCall += EnsurePlayerHasCameraInPrefabAndScene;
    }

    [MenuItem("Tools/🎥 Setup Player Prefab (Tag + Components)")]
    public static void EnsurePlayerHasCameraInPrefabAndScene()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) return;

        string prefabPath = "Assets/prefab/Player/PlayerPrefab.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (playerPrefab != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(playerPrefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
            bool changed = false;

            // Gắn Tag "Player"
            if (!prefabRoot.CompareTag("Player"))
            {
                prefabRoot.tag = "Player";
                changed = true;
            }

            // Xóa Camera con bên trong Prefab nếu có (Camera phải ở ngoài, riêng biệt)
            Transform existingCam = prefabRoot.transform.Find("Main Camera");
            if (existingCam != null)
            {
                Object.DestroyImmediate(existingCam.gameObject);
                changed = true;
                Debug.Log("<color=yellow>[Setup]</color> Đã xóa Main Camera bên trong PlayerPrefab (Camera phải là object riêng trong Scene)");
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                Debug.Log("<color=green>[Setup]</color> Đã cập nhật PlayerPrefab!");
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }
}
