using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OrcBerserkerAI))]
[CanEditMultipleObjects]
public class OrcBerserkerAIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        OrcBerserkerAI berserker = (OrcBerserkerAI)target;
        if (berserker == null) return;

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
            EditorUtility.SetDirty(berserker);

            if (PrefabUtility.IsPartOfPrefabInstance(berserker))
            {
                PrefabUtility.ApplyPrefabInstance(berserker.gameObject, InteractionMode.UserAction);
                Debug.Log("<color=green>[OrcBerserkerAIEditor]</color> Đã tự động ghi đè thông số lên Prefab gốc thành công!");
            }

            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(berserker.gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(berserker.gameObject.scene);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Thành công!", "Đã áp dụng và lưu thông số Hitbox của Orc Berserker thành công!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Lưu ý", "Đang trong Play Mode! Thông số sẽ chỉ tạm thời áp dụng.", "Đã hiểu");
            }
        }
    }

    private void OnSceneGUI()
    {
        OrcBerserkerAI berserker = (OrcBerserkerAI)target;
        if (berserker == null) return;

        Vector3 center = berserker.GetCenterPosition();
        Vector2 facing = Application.isPlaying ? (Vector2)berserker.transform.right : Vector2.right; // Tránh lỗi trong Editor

        // 1. Vẽ tầm đánh cận chiến (attackRange)
        Handles.color = new Color(1f, 0f, 0f, 0.1f);
        Handles.DrawSolidDisc(center, Vector3.forward, berserker.attackRange);
        Handles.color = new Color(1f, 0f, 0f, 0.4f);
        Handles.DrawWireDisc(center, Vector3.forward, berserker.attackRange);

        // Kéo tầm đánh
        EditorGUI.BeginChangeCheck();
        float newAttackRange = Handles.RadiusHandle(Quaternion.identity, center, berserker.attackRange);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(berserker, "Adjust Attack Range");
            berserker.attackRange = Mathf.Max(0.1f, newAttackRange);
            EditorUtility.SetDirty(berserker);
        }

        // 2. Tính toán tâm của Hitbox trong World Space
        Vector3 attackPoint = center + (Vector3)(facing.normalized * berserker.attackOffset);

        Handles.color = Color.cyan;
        Handles.DrawWireDisc(center, Vector3.forward, 0.04f);
        Handles.DrawLine(center, attackPoint);

        // Kéo Offset tâm hitbox
        EditorGUI.BeginChangeCheck();
        Vector3 newAttackPoint = Handles.PositionHandle(attackPoint, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(berserker, "Adjust Hitbox Offset");
            Vector3 offsetVec = newAttackPoint - center;
            berserker.attackOffset = Vector3.Dot(offsetVec, (Vector3)facing.normalized);
            EditorUtility.SetDirty(berserker);
        }

        // 3. Vẽ và kéo bán kính chém thường (slashRadius)
        Handles.color = new Color(1f, 0.92f, 0.016f, 0.15f);
        Handles.DrawSolidDisc(attackPoint, Vector3.forward, berserker.slashRadius);
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(attackPoint, Vector3.forward, berserker.slashRadius);

        EditorGUI.BeginChangeCheck();
        float newSlashRadius = Handles.RadiusHandle(Quaternion.identity, attackPoint, berserker.slashRadius);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(berserker, "Adjust Slash Radius");
            berserker.slashRadius = Mathf.Max(0.05f, newSlashRadius);
            EditorUtility.SetDirty(berserker);
        }
    }
}
