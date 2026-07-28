using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MapAutoGenerator
{
    static MapAutoGenerator()
    {
        EditorApplication.delayCall += Initialize;
    }

    private static void Initialize()
    {
        // Only run if not in play mode and Unity is not compiling
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) return;

        // Only generate if SampleScene is active
        string scenePath = "Assets/Scenes/SampleScene.unity";
        var currentScene = EditorSceneManager.GetActiveScene();
        
        if (currentScene.path != scenePath)
        {
            return;
        }

        // Find or create MapGenerator GameObject
        MapGenerator generator = null;
#if UNITY_2021_3_OR_NEWER
        generator = Object.FindFirstObjectByType<MapGenerator>();
#else
        generator = Object.FindObjectOfType<MapGenerator>();
#endif

        if (generator == null)
        {
            GameObject genObj = new GameObject("MapGenerator");
            generator = genObj.AddComponent<MapGenerator>();
            Debug.Log("Created MapGenerator GameObject automatically in scene.");
        }

        // Auto-slice tilesets if not already sliced
        string grassPath = TilesetSlicer.FindAssetPath("TX Tileset Grass.png") ?? "Assets/tainguyen/map/TX Tileset Grass.png";
        object[] grassAssets = AssetDatabase.LoadAllAssetsAtPath(grassPath);
        int spriteCount = 0;
        foreach (object asset in grassAssets)
        {
            if (asset is Sprite) spriteCount++;
        }
        if (spriteCount <= 1) // If only the main texture is imported as a sprite
        {
            Debug.Log("Un-sliced tileset detected. Auto-slicing tilesets...");
            TilesetSlicer.SliceTilesets();
        }

        // Check if map is empty or needs regeneration
        bool needsRegen = generator.transform.childCount == 0 || generator.transform.Find("Ground") == null;
        if (needsRegen)
        {
            Debug.Log("Map is empty or outdated. Auto-generating flat wilderness map...");
            generator.ClearMap();
            generator.GenerateMap();
            EditorSceneManager.MarkSceneDirty(currentScene);
            EditorSceneManager.SaveScene(currentScene);
            Debug.Log("Wilderness map generated and scene saved automatically!");
        }
    }
}
