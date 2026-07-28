using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using System.Collections.Generic;

public static class TilesetSlicer
{
    [MenuItem("Tools/Slice Tilesets")]
    public static void SliceTilesets()
    {
        string grassPath = FindAssetPath("TX Tileset Grass.png");
        if (!string.IsNullOrEmpty(grassPath))
        {
            SliceTexture(grassPath, 32, 32);
        }
        else
        {
            Debug.LogError("Could not find asset: TX Tileset Grass.png in project!");
        }

        string stonePath = FindAssetPath("TX Tileset Stone Ground.png");
        if (!string.IsNullOrEmpty(stonePath))
        {
            SliceTexture(stonePath, 32, 32);
        }
        else
        {
            Debug.LogError("Could not find asset: TX Tileset Stone Ground.png in project!");
        }
    }

    public static string FindAssetPath(string filename)
    {
        string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filename);
        string[] guids = AssetDatabase.FindAssets($"{nameWithoutExt} t:Texture2D");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(filename, System.StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        string[] fallbackPaths = new string[]
        {
            "Assets/tainguyen/map/" + filename,
            "Assets/map/" + filename
        };
        foreach (string p in fallbackPaths)
        {
            if (AssetImporter.GetAtPath(p) != null) return p;
        }
        return null;
    }

    private static void SliceTexture(string assetPath, int tileWidth, int tileHeight)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Could not find TextureImporter for asset: {assetPath}");
            return;
        }

        // Configure importer settings for pixel-perfect 2D sprites
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Point; // Keep pixel art sharp and crisp
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // Load the texture to get dimensions
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
        {
            Debug.LogError($"Could not load Texture2D: {assetPath}");
            return;
        }

        int width = texture.width;
        int height = texture.height;
        int columns = width / tileWidth;
        int rows = height / tileHeight;

        // Use ISpriteEditorDataProvider (new API replacing obsolete spritesheet)
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        // Build sprite rects
        string baseName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        List<SpriteRect> spriteRects = new List<SpriteRect>();

        int index = 0;
        for (int r = rows - 1; r >= 0; r--)
        {
            for (int c = 0; c < columns; c++)
            {
                SpriteRect rect = new SpriteRect();
                rect.name = $"{baseName}_{index}";
                rect.rect = new Rect(c * tileWidth, r * tileHeight, tileWidth, tileHeight);
                rect.alignment = SpriteAlignment.BottomLeft;
                rect.pivot = new Vector2(0f, 0f);
                rect.spriteID = GUID.Generate();
                spriteRects.Add(rect);
                index++;
            }
        }

        // Apply via new API
        dataProvider.SetSpriteRects(spriteRects.ToArray());
        dataProvider.Apply();

        // Save and reimport
        var assetImporter = dataProvider.targetObject as AssetImporter;
        assetImporter.SaveAndReimport();

        Debug.Log($"Sliced {assetPath} into {spriteRects.Count} sprites successfully.");
    }
}
