using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

[InitializeOnLoad]
public class PrintHierarchy
{
    static PrintHierarchy()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== DIAGNOSTICS: FULL SCENE HIERARCHY ===");

        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        sb.AppendLine($"Active Scene: '{activeScene.name}'");

        GameObject[] rootObjects = activeScene.GetRootGameObjects();
        foreach (GameObject root in rootObjects)
        {
            PrintObject(root, "", sb);
        }

        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "hierarchy.txt");
        File.WriteAllText(outputPath, sb.ToString());
        Debug.Log("Scene hierarchy diagnostics written to: " + outputPath);
    }

    private static void PrintObject(GameObject obj, string indent, StringBuilder sb)
    {
        sb.AppendLine($"{indent}- GameObject: '{obj.name}' | Active: {obj.activeSelf} | LocalPosition: {obj.transform.localPosition} | Scale: {obj.transform.localScale}");
        
        Component[] components = obj.GetComponents<Component>();
        foreach (Component comp in components)
        {
            if (comp == null) continue;
            sb.AppendLine($"{indent}  * Component: {comp.GetType().Name}");
            if (comp is SpriteRenderer sr)
            {
                sb.AppendLine($"{indent}    - Sprite: {sr.sprite?.name} | Bounds: {sr.bounds}");
            }
            else if (comp is CapsuleCollider2D col)
            {
                sb.AppendLine($"{indent}    - Offset: {col.offset} | Size: {col.size}");
            }
        }

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            PrintObject(obj.transform.GetChild(i).gameObject, indent + "  ", sb);
        }
    }
}
