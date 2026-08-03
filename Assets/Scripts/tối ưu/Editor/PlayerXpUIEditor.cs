using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerXpUI))]
public class PlayerXpUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlayerXpUI xpUI = (PlayerXpUI)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("=== BỘ CÔNG CỤ LƯU VỊ TRÍ XP ===", EditorStyles.boldLabel);

        RectTransform rect = xpUI.GetComponent<RectTransform>();
        if (rect != null)
        {
            EditorGUILayout.HelpBox($"Vị trí hiện tại (AnchoredPosition): X={rect.anchoredPosition.x:F1}, Y={rect.anchoredPosition.y:F1}", MessageType.Info);
        }

        if (GUILayout.Button("📌 Áp dụng & Lưu vị trí hiện tại (Apply & Save Position)", GUILayout.Height(30)))
        {
            xpUI.SaveCurrentPosition();
            EditorUtility.SetDirty(xpUI);
            Debug.Log("<color=green>[PlayerXpUI]</color> Đã áp dụng và lưu vị trí thanh XP!");
        }

        if (GUILayout.Button("🗑️ Xóa bộ nhớ lưu vị trí cũ (Clear PlayerPrefs)", GUILayout.Height(28)))
        {
            PlayerXpUI.ClearSavedRuntimePosition();
            xpUI.ApplySavedPosition();
            EditorUtility.SetDirty(xpUI);
            Debug.Log("<color=yellow>[PlayerXpUI]</color> Đã xóa vị trí cũ lưu trong PlayerPrefs!");
        }
    }
}
