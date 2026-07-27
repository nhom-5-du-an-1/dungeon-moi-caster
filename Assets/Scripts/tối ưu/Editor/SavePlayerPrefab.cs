using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SavePlayerPrefab : EditorWindow
{
    [MenuItem("Tools/💾 Lưu Thay Đổi Player Prefab (Save Player Prefab Changes)")]
    public static void SavePlayerPrefabChangesNow()
    {
        GameObject player = GameObject.Find("DarkFantasyPlayer") ?? GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("<color=red>[Save Prefab] KHÔNG tìm thấy nhân vật DarkFantasyPlayer trong Scene!</color>");
            return;
        }

        // Đánh dấu dirty cho Player và Scene
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.scene);

        // Nếu là Prefab Instance -> Cập nhật đè lại bản gốc trong đĩa
        GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(player);
        if (prefabAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(prefabAsset);
            PrefabUtility.SaveAsPrefabAssetAndConnect(player, path, InteractionMode.UserAction);
            Debug.Log($"<color=green><b>[Thành Công] Đã lưu toàn bộ thay đổi của DarkFantasyPlayer vào Prefab tại: {path}!</b></color>");
        }
        else
        {
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("<color=green><b>[Thành Công] Đã lưu Scene hiện tại thành công!</b></color>");
        }
    }
}
