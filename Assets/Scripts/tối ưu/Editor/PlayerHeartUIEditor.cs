using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerHeartUI))]
public class PlayerHeartUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlayerHeartUI heartUI = (PlayerHeartUI)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("=== BỘ CÔNG CỤ LƯU VỊ TRÍ UI ===", EditorStyles.boldLabel);

        RectTransform rect = heartUI.GetComponent<RectTransform>();
        if (rect != null)
        {
            EditorGUILayout.HelpBox($"Vị trí hiện tại (AnchoredPosition): X={rect.anchoredPosition.x:F1}, Y={rect.anchoredPosition.y:F1}", MessageType.Info);
        }

        if (GUILayout.Button("📌 Lưu vị trí hiện tại làm Mặc định", GUILayout.Height(30)))
        {
            heartUI.SaveCurrentPosition();
            EditorUtility.SetDirty(heartUI);
            Debug.Log("<color=green>[PlayerHeartUI]</color> Đã lưu cố định vị trí thanh tim vào Scene!");
        }

        if (GUILayout.Button("🗑️ Xóa bộ nhớ lưu vị trí cũ (Clear PlayerPrefs)", GUILayout.Height(28)))
        {
            PlayerHeartUI.ClearSavedRuntimePosition();
            heartUI.ApplySavedPosition();
            EditorUtility.SetDirty(heartUI);
            Debug.Log("<color=yellow>[PlayerHeartUI]</color> Đã xóa vị trí cũ lưu trong PlayerPrefs!");
        }
    }
}
