using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyAI))]
[CanEditMultipleObjects]
public class EnemyAIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Vẽ Inspector mặc định
        DrawDefaultInspector();

        EnemyAI enemy = (EnemyAI)target;
        if (enemy == null) return;

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("=== BỘ CÔNG CỤ CÂN CHỈNH HITBOX TRỰC QUAN ===", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "💡 HƯỚNG DẪN:\n" +
            "1. Bạn có thể KÉO THẢ TRỰC TIẾP tâm hitbox và bán kính/kích thước trong cửa sổ Scene View.\n" +
            "2. Khi chỉnh sửa xong, nhấn nút '💾 Apply & Save' bên dưới để áp dụng vĩnh viễn thay đổi vào Prefab gốc và lưu Scene.", 
            MessageType.Info
        );

        // Nút bấm Apply để lưu thông số
        if (GUILayout.Button("💾 Apply & Save Hitbox (Áp Dụng)", GUILayout.Height(36)))
        {
            // Ghi nhận thay đổi cho đối tượng để Unity biết cần lưu
            EditorUtility.SetDirty(enemy);

            // Kiểm tra và tự động áp dụng đè lên Prefab gốc nếu đối tượng này là một Prefab Instance trong Scene
            if (PrefabUtility.IsPartOfPrefabInstance(enemy))
            {
                PrefabUtility.ApplyPrefabInstance(enemy.gameObject, InteractionMode.UserAction);
                Debug.Log("<color=green>[EnemyAIEditor]</color> Đã tự động ghi đè thông số lên Prefab gốc thành công!");
            }

            // Lưu Scene hiện tại nếu không ở chế độ Play mode
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(enemy.gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(enemy.gameObject.scene);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>[EnemyAIEditor]</color> Đã lưu thông số Hitbox vào Scene thành công!");
                EditorUtility.DisplayDialog("Thành công!", "Đã áp dụng và lưu thông số Hitbox thành công!", "OK");
            }
            else
            {
                Debug.LogWarning("[EnemyAIEditor] Bạn đang ở chế độ Play mode! Thông số thay đổi sẽ bị reset khi bạn tắt game.");
                EditorUtility.DisplayDialog("Lưu ý", "Đang trong Play Mode! Thông số sẽ chỉ tạm thời áp dụng, hãy nhấn Apply ở chế độ Edit Mode để lưu vĩnh viễn.", "Đã hiểu");
            }
        }
    }

    private void OnSceneGUI()
    {
        EnemyAI enemy = (EnemyAI)target;
        if (enemy == null) return;

        Vector3 center = enemy.GetCenterPosition();
        Vector2 facing = Application.isPlaying ? (Vector2)enemy.transform.right : Vector2.right;
        
        // Tính toán tâm của Hitbox trong World Space
        Vector3 attackPoint = center + (Vector3)(facing.normalized * enemy.attackOffset);

        // Vẽ tâm nhân vật và đường nối tới tâm Hitbox
        Handles.color = Color.cyan;
        Handles.DrawWireDisc(center, Vector3.forward, 0.04f);
        Handles.DrawLine(center, attackPoint);

        // Handle 1: Kéo thả vị trí tâm để điều chỉnh Offset (attackOffset)
        EditorGUI.BeginChangeCheck();
        Vector3 newAttackPoint = Handles.PositionHandle(attackPoint, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(enemy, "Adjust Hitbox Offset");
            // Chiếu vector kéo của người chơi lên hướng nhìn để tính offset chính xác
            Vector3 offsetVec = newAttackPoint - center;
            enemy.attackOffset = Vector3.Dot(offsetVec, (Vector3)facing.normalized);
            EditorUtility.SetDirty(enemy);
        }

        // Handle 2: Thay đổi kích thước (Bán kính hình tròn hoặc Kích thước hình chữ nhật)
        if (enemy.useCircleHitbox)
        {
            // Vẽ chu vi hình tròn hiện tại
            Handles.color = new Color(1f, 0.92f, 0.016f, 0.2f);
            Handles.DrawSolidDisc(attackPoint, Vector3.forward, enemy.attackRadius);
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(attackPoint, Vector3.forward, enemy.attackRadius);

            // Bán kính kéo thả trực quan
            EditorGUI.BeginChangeCheck();
            float newRadius = Handles.RadiusHandle(Quaternion.identity, attackPoint, enemy.attackRadius);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(enemy, "Adjust Hitbox Radius");
                enemy.attackRadius = Mathf.Max(0.05f, newRadius);
                EditorUtility.SetDirty(enemy);
            }
        }
        else
        {
            // Vẽ hình chữ nhật hitbox
            Vector2 size = enemy.attackBoxSize;
            Vector3[] corners = new Vector3[]
            {
                attackPoint + new Vector3(-size.x * 0.5f, -size.y * 0.5f, 0f),
                attackPoint + new Vector3(size.x * 0.5f, -size.y * 0.5f, 0f),
                attackPoint + new Vector3(size.x * 0.5f, size.y * 0.5f, 0f),
                attackPoint + new Vector3(-size.x * 0.5f, size.y * 0.5f, 0f)
            };
            Handles.color = new Color(1f, 0.92f, 0.016f, 0.1f);
            Handles.DrawSolidRectangleWithOutline(corners, new Color(1f, 0.92f, 0.016f, 0.1f), Color.yellow);

            // Tạo các điểm nút kéo chỉnh kích thước ở góc trên và góc phải
            Vector3 rightEdge = attackPoint + new Vector3(size.x * 0.5f, 0f, 0f);
            Vector3 topEdge = attackPoint + new Vector3(0f, size.y * 0.5f, 0f);

            float handleSize = HandleUtility.GetHandleSize(attackPoint) * 0.05f;
            Handles.color = Color.red;

            EditorGUI.BeginChangeCheck();
            var fmh_125_66_639208666848248946 = Quaternion.identity; Vector3 newRight = Handles.FreeMoveHandle(rightEdge, handleSize, Vector3.zero, Handles.DotHandleCap);
            var fmh_126_62_639208666848265230 = Quaternion.identity; Vector3 newTop = Handles.FreeMoveHandle(topEdge, handleSize, Vector3.zero, Handles.DotHandleCap);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(enemy, "Adjust Hitbox Box Size");
                float newWidth = Mathf.Max(0.05f, (newRight.x - attackPoint.x) * 2f);
                float newHeight = Mathf.Max(0.05f, (newTop.y - attackPoint.y) * 2f);
                enemy.attackBoxSize = new Vector2(newWidth, newHeight);
                EditorUtility.SetDirty(enemy);
            }
        }
    }
}
