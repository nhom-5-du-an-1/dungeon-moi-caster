using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class FixSceneEnemyHealth
{
    static FixSceneEnemyHealth()
    {
        EditorApplication.delayCall += FixAllEnemiesInScene;
    }

    [MenuItem("Tools/Fix All Enemy Health Bars in Scene")]
    public static void FixAllEnemiesInScene()
    {
        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");

        EnemyHealth[] enemies = Object.FindObjectsOfType<EnemyHealth>();
        foreach (EnemyHealth health in enemies)
        {
            // Xóa child cũ nếu có (HealthBar_SpriteOverlay)
            Transform oldChild = health.transform.Find("HealthBar_SpriteOverlay");
            if (oldChild != null)
            {
                Object.DestroyImmediate(oldChild.gameObject);
            }

            Transform newChild = health.transform.Find("HealthBar_Overlay");
            if (newChild != null)
            {
                Object.DestroyImmediate(newChild.gameObject);
            }

            health.maxHealth = 50;
            health.currentHealth = 50;
            health.alwaysShowHealthBar = true;
            health.barWidth = 0.28f;
            health.barHeight = 0.04f;
            health.barHeightOffset = 0.06f;

            health.CreateSpriteHealthBar();
            health.UpdateHealthBarUI(true);

            // Tự động chuyển quái về Layer "Enemy"
            if (enemyLayerIndex != -1 && health.gameObject.layer != enemyLayerIndex)
            {
                health.gameObject.layer = enemyLayerIndex;
                EditorUtility.SetDirty(health.gameObject);
            }

            EditorUtility.SetDirty(health);
        }

        EnemyAI[] aiEnemies = Object.FindObjectsOfType<EnemyAI>();
        foreach (EnemyAI ai in aiEnemies)
        {
            if (enemyLayerIndex != -1 && ai.gameObject.layer != enemyLayerIndex)
            {
                ai.gameObject.layer = enemyLayerIndex;
                EditorUtility.SetDirty(ai.gameObject);
            }

            if (ai.GetComponent<EnemyHealth>() == null)
            {
                EnemyHealth health = ai.gameObject.AddComponent<EnemyHealth>();
                health.maxHealth = 50;
                health.currentHealth = 50;
                health.alwaysShowHealthBar = true;
                health.barWidth = 0.28f;
                health.barHeight = 0.04f;
                health.barHeightOffset = 0.06f;

                health.CreateSpriteHealthBar();
                health.UpdateHealthBarUI(true);
                EditorUtility.SetDirty(ai.gameObject);
            }
        }

        // Tự động kiểm tra và sửa LayerMask cho Player trong Scene
        PlayerMovement player = Object.FindObjectOfType<PlayerMovement>();
        if (player != null && enemyLayerIndex != -1)
        {
            int targetMask = 1 << enemyLayerIndex;
            if ((player.enemyLayer.value & targetMask) == 0)
            {
                player.enemyLayer = player.enemyLayer.value | targetMask;
                EditorUtility.SetDirty(player);
                Debug.Log("<color=green>[FixSceneEnemyHealth]</color> Đã tự động cập nhật LayerMask 'enemyLayer' cho Player!");
            }
        }

        Debug.Log("<color=green>[FixSceneEnemyHealth]</color> Đã dọn dẹp, đồng bộ Layer và cập nhật hệ thống thanh máu mới cho toàn bộ Quái vật!");
    }
}
