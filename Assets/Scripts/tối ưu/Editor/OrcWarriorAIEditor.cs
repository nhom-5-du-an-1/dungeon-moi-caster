using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OrcWarriorAI))]
[CanEditMultipleObjects]
public class OrcWarriorAIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        OrcWarriorAI warrior = (OrcWarriorAI)target;
        if (warrior == null) return;

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
            EditorUtility.SetDirty(warrior);

            if (PrefabUtility.IsPartOfPrefabInstance(warrior))
            {
                PrefabUtility.ApplyPrefabInstance(warrior.gameObject, InteractionMode.UserAction);
                Debug.Log("<color=green>[OrcWarriorAIEditor]</color> Đã tự động ghi đè thông số lên Prefab gốc thành công!");
            }

            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(warrior.gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(warrior.gameObject.scene);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Thành công!", "Đã áp dụng và lưu thông số Hitbox của Orc Warrior thành công!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Lưu ý", "Đang trong Play Mode! Thông số sẽ chỉ tạm thời áp dụng.", "Đã hiểu");
            }
        }
    }

    private void OnSceneGUI()
    {
        OrcWarriorAI warrior = (OrcWarriorAI)target;
        if (warrior == null) return;

        Vector3 center = warrior.GetCenterPosition();
        Vector2 facing = Application.isPlaying ? (Vector2)warrior.transform.right : Vector2.right; // Tránh lỗi trong Editor

        // 1. Vẽ tầm đánh cận chiến (attackRange)
        Handles.color = new Color(1f, 0f, 0f, 0.1f);
        Handles.DrawSolidDisc(center, Vector3.forward, warrior.attackRange);
        Handles.color = new Color(1f, 0f, 0f, 0.4f);
        Handles.DrawWireDisc(center, Vector3.forward, warrior.attackRange);

        // Kéo tầm đánh
        EditorGUI.BeginChangeCheck();
        float newAttackRange = Handles.RadiusHandle(Quaternion.identity, center, warrior.attackRange);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(warrior, "Adjust Attack Range");
            warrior.attackRange = Mathf.Max(0.1f, newAttackRange);
            EditorUtility.SetDirty(warrior);
        }

        // 2. Tính toán tâm của Hitbox trong World Space
        Vector3 attackPoint = center + (Vector3)(facing.normalized * warrior.attackOffset);

        Handles.color = Color.cyan;
        Handles.DrawWireDisc(center, Vector3.forward, 0.04f);
        Handles.DrawLine(center, attackPoint);

        // Kéo Offset tâm hitbox
        EditorGUI.BeginChangeCheck();
        Vector3 newAttackPoint = Handles.PositionHandle(attackPoint, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(warrior, "Adjust Hitbox Offset");
            Vector3 offsetVec = newAttackPoint - center;
            warrior.attackOffset = Vector3.Dot(offsetVec, (Vector3)facing.normalized);
            EditorUtility.SetDirty(warrior);
        }

        // 3. Vẽ và kéo bán kính chém thường (slashRadius)
        Handles.color = new Color(1f, 0.92f, 0.016f, 0.15f);
        Handles.DrawSolidDisc(attackPoint, Vector3.forward, warrior.slashRadius);
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(attackPoint, Vector3.forward, warrior.slashRadius);

        EditorGUI.BeginChangeCheck();
        float newSlashRadius = Handles.RadiusHandle(Quaternion.identity, attackPoint, warrior.slashRadius);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(warrior, "Adjust Slash Radius");
            warrior.slashRadius = Mathf.Max(0.05f, newSlashRadius);
            EditorUtility.SetDirty(warrior);
        }
    }
}
