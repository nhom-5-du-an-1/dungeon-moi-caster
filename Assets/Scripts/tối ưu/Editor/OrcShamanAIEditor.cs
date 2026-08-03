using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OrcShamanAI))]
[CanEditMultipleObjects]
public class OrcShamanAIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        OrcShamanAI shaman = (OrcShamanAI)target;
        if (shaman == null) return;

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("=== BỘ CÔNG CỤ CÂN CHỈNH HITBOX TRỰC QUAN ===", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "💡 HƯỚNG DẪN:\n" +
            "1. Bạn có thể KÉO THẢ TRỰC TIẾP tâm hitbox và bán kính/kích thước trong cửa sổ Scene View.\n" +
            "2. Khi chỉnh sửa xong, nhấn nút '💾 Apply & Save' bên dưới để áp dụng vĩnh viễn thay đổi vào Prefab gốc và lưu Scene.",
            MessageType.Info
        );

        if (GUILayout.Button("💾 Apply & Save Hitbox (Áp Dụng)", GUILayout.Height(36)))
        {
            EditorUtility.SetDirty(shaman);

            if (PrefabUtility.IsPartOfPrefabInstance(shaman))
            {
                PrefabUtility.ApplyPrefabInstance(shaman.gameObject, InteractionMode.UserAction);
                Debug.Log("<color=green>[OrcShamanAIEditor]</color> Đã tự động ghi đè thông số lên Prefab gốc thành công!");
            }

            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(shaman.gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(shaman.gameObject.scene);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Thành công!", "Đã áp dụng và lưu thông số Hitbox của Orc Shaman thành công!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Lưu ý", "Đang trong Play Mode! Thông số sẽ chỉ tạm thời áp dụng.", "Đã hiểu");
            }
        }
    }

    private void OnSceneGUI()
    {
        OrcShamanAI shaman = (OrcShamanAI)target;
        if (shaman == null) return;

        Vector3 center = shaman.GetCenterPosition();

        // 1. Vẽ tầm bắn phép thuật (shootRange)
        Handles.color = new Color(0.9f, 0.9f, 0f, 0.1f);
        Handles.DrawSolidDisc(center, Vector3.forward, shaman.shootRange);
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(center, Vector3.forward, shaman.shootRange);

        // Kéo tầm bắn
        EditorGUI.BeginChangeCheck();
        float newShootRange = Handles.RadiusHandle(Quaternion.identity, center, shaman.shootRange);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(shaman, "Adjust Shoot Range");
            shaman.shootRange = Mathf.Max(0.1f, newShootRange);
            EditorUtility.SetDirty(shaman);
        }

        // 2. Vẽ và kéo bán kính hồi máu đồng đội (healRadius)
        Handles.color = new Color(0.2f, 0.9f, 0.3f, 0.08f);
        Handles.DrawSolidDisc(center, Vector3.forward, shaman.healRadius);
        Handles.color = new Color(0.2f, 0.9f, 0.3f, 0.4f);
        Handles.DrawWireDisc(center, Vector3.forward, shaman.healRadius);

        EditorGUI.BeginChangeCheck();
        float newHealRadius = Handles.RadiusHandle(Quaternion.identity, center, shaman.healRadius);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(shaman, "Adjust Heal Radius");
            shaman.healRadius = Mathf.Max(0.1f, newHealRadius);
            EditorUtility.SetDirty(shaman);
        }
    }
}
