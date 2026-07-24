using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

[InitializeOnLoad]
public class CheckUnityRuntimeState
{
    static CheckUnityRuntimeState()
    {
        EditorApplication.update += RunCheck;
    }

    private static float lastCheckTime = 0f;

    private static void RunCheck()
    {
        if (Time.realtimeSinceStartup - lastCheckTime < 1.0f) return;
        lastCheckTime = Time.realtimeSinceStartup;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== RUNTIME DIAGNOSTICS ({System.DateTime.Now:HH:mm:ss}) ===");
        sb.AppendLine($"Is Playing: {Application.isPlaying}");

        // Find Player
        GameObject player = GameObject.Find("DarkFantasyPlayer");
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            sb.AppendLine($"\n[PLAYER]: '{player.name}'");
            sb.AppendLine($"  Position: {player.transform.position}");
            sb.AppendLine($"  LocalScale: {player.transform.localScale}");

            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sb.AppendLine($"  SpriteRenderer bounds center: {sr.bounds.center} | size: {sr.bounds.size}");
                sb.AppendLine($"  Sprite name: {sr.sprite?.name}");
            }

            CapsuleCollider2D col = player.GetComponent<CapsuleCollider2D>();
            if (col != null)
            {
                sb.AppendLine($"  CapsuleCollider2D offset: {col.offset} | size: {col.size} | bounds: {col.bounds}");
            }

            PlayerMovement pm = player.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                sb.AppendLine($"  PlayerMovement: attackRange/Offset={pm.attackOffset}, boxSize={pm.attackBoxSize}, spriteOffset={pm.spriteOffset}");
                sb.AppendLine($"  GetCenterPosition(): {pm.GetCenterPosition()}");
            }
        }
        else
        {
            sb.AppendLine("\n[PLAYER]: Not found");
        }

        // Find Enemies
        EnemyAI[] enemies = Object.FindObjectsOfType<EnemyAI>();
        sb.AppendLine($"\n[ENEMIES COUNT]: {enemies.Length}");

        foreach (EnemyAI enemy in enemies)
        {
            sb.AppendLine($"\n[ENEMY]: '{enemy.name}'");
            sb.AppendLine($"  Position: {enemy.transform.position}");
            sb.AppendLine($"  LocalScale: {enemy.transform.localScale}");
            sb.AppendLine($"  attackRange: {enemy.attackRange} | attackOffset: {enemy.attackOffset} | attackBoxSize: {enemy.attackBoxSize}");
            sb.AppendLine($"  spriteOffset: {enemy.spriteOffset} | showHitbox: {enemy.showHitbox}");
            sb.AppendLine($"  GetCenterPosition(): {enemy.GetCenterPosition()}");

            SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sb.AppendLine($"  SpriteRenderer bounds center: {sr.bounds.center} | size: {sr.bounds.size}");
                sb.AppendLine($"  Sprite name: {sr.sprite?.name}");
            }

            CapsuleCollider2D col = enemy.GetComponent<CapsuleCollider2D>();
            if (col != null)
            {
                sb.AppendLine($"  CapsuleCollider2D offset: {col.offset} | size: {col.size} | bounds: {col.bounds}");
            }

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                sb.AppendLine($"  EnemyHealth: current={health.currentHealth}/{health.maxHealth} | show={health.alwaysShowHealthBar}");
                Transform barChild = enemy.transform.Find("HealthBar_Overlay");
                if (barChild != null)
                {
                    sb.AppendLine($"  HealthBar_Overlay found! Active: {barChild.gameObject.activeSelf} | Pos: {barChild.position} | Scale: {barChild.localScale}");
                    SpriteRenderer[] srs = barChild.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var s in srs)
                    {
                        sb.AppendLine($"    Child SR: '{s.name}' | active={s.gameObject.activeInHierarchy} | color={s.color} | order={s.sortingOrder} | scale={s.transform.localScale} | sprite={s.sprite?.name}");
                    }
                }
                else
                {
                    sb.AppendLine("  HealthBar_Overlay: NOT FOUND in enemy hierarchy!");
                }
            }
            else
            {
                sb.AppendLine("  EnemyHealth Component: NOT ATTACHED!");
            }

            // Print Scene Hierarchy
            sb.AppendLine("\n=== SCENE HIERARCHY ===");
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            sb.AppendLine($"Active Scene: '{activeScene.name}'");
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                PrintObjectHierarchy(root, "", sb);
            }

            if (player != null)
            {
                float distPos = Vector3.Distance(enemy.transform.position, player.transform.position);
                float distCenter = Vector3.Distance(enemy.GetCenterPosition(), enemy.GetPlayerCenterPosition());
                sb.AppendLine($"  Distance to Player (Raw Position): {distPos:F3}");
                sb.AppendLine($"  Distance to Player (GetCenterPosition): {distCenter:F3}");
            }
        }

        string path = Path.Combine(Directory.GetCurrentDirectory(), "diagnostics.txt");
        File.WriteAllText(path, sb.ToString());
    }

    private static void PrintObjectHierarchy(GameObject obj, string indent, StringBuilder sb)
    {
        sb.AppendLine($"{indent}{obj.name} (active: {obj.activeSelf})");
        foreach (Transform child in obj.transform)
        {
            PrintObjectHierarchy(child.gameObject, indent + "  ", sb);
        }
    }
}
