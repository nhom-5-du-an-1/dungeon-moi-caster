using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using System.Collections.Generic;

public static class TilesetSlicer
{
    [MenuItem("Tools/Slice Tilesets")]
    public static void SliceTilesets()
    {
        SliceTexture("Assets/map/TX Tileset Grass.png", 32, 32);
        SliceTexture("Assets/map/TX Tileset Stone Ground.png", 32, 32);
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
