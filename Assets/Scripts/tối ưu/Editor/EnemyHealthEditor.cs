using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyHealth))]
[CanEditMultipleObjects]
public class EnemyHealthEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EnemyHealth enemyHealth = (EnemyHealth)target;

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("=== BỘ CÔNG CỤ ĐIỀU CHỈNH TIM QUÁI VẬT ===", EditorStyles.boldLabel);

        if (enemyHealth.useHeartDisplay)
        {
            EditorGUILayout.HelpBox("Bạn có thể tùy chỉnh độ cao (Offset Y), độ lệch ngang (Offset X), kích thước (Heart Scale) và khoảng cách tim trực tiếp ở trên!", MessageType.Info);

            if (GUILayout.Button("🧹 Dọn dẹp các ô Tim rác bị trùng lặp", GUILayout.Height(28)))
            {
                EnemyHealth[] allEnemies = Object.FindObjectsOfType<EnemyHealth>();
                foreach (EnemyHealth enemy in allEnemies)
                {
                    enemy.CreateSpriteHealthBar();
                    enemy.UpdateHealthBarUI(true);
                    EditorUtility.SetDirty(enemy);
                }
                Debug.Log("<color=green>[EnemyHealthEditor]</color> Đã xóa toàn bộ các ô tim rác bị trùng lặp!");
            }

            if (GUILayout.Button("🔄 Căn giữa đỉnh đầu (Reset Offset X)", GUILayout.Height(28)))
            {
                Undo.RecordObject(enemyHealth, "Center Enemy Hearts");
                enemyHealth.heartOffsetX = 0f;
                enemyHealth.CreateSpriteHealthBar();
                enemyHealth.UpdateHealthBarUI(true);
                EditorUtility.SetDirty(enemyHealth);
            }

            if (GUILayout.Button("⚡ Áp dụng thông số Tim này cho TẤT CẢ Quái trong Scene", GUILayout.Height(32)))
            {
                EnemyHealth[] allEnemies = Object.FindObjectsOfType<EnemyHealth>();
                int count = 0;
                foreach (EnemyHealth enemy in allEnemies)
                {
                    Undo.RecordObject(enemy, "Apply Heart Settings to All Enemies");
                    enemy.useHeartDisplay = true;
                    enemy.heartOffsetX = enemyHealth.heartOffsetX;
                    enemy.heartOffsetY = enemyHealth.heartOffsetY;
                    enemy.heartScale = enemyHealth.heartScale;
                    enemy.heartSpacing = enemyHealth.heartSpacing;
                    enemy.heartCount = enemyHealth.heartCount;
                    enemy.enemyHeartFull = enemyHealth.enemyHeartFull;
                    enemy.enemyHeartHalf = enemyHealth.enemyHeartHalf;
                    enemy.enemyHeartEmpty = enemyHealth.enemyHeartEmpty;
                    enemy.CreateSpriteHealthBar();
                    enemy.UpdateHealthBarUI(true);
                    EditorUtility.SetDirty(enemy);
                    count++;
                }
                Debug.Log($"<color=green>[EnemyHealthEditor]</color> Đã áp dụng thông số tim cho {count} quái vật trong Scene!");
                EditorUtility.DisplayDialog("Thành công!", $"Đã đồng bộ thông số vị trí & kích thước tim cho {count} quái vật!", "OK");
            }
        }
    }
}
