using System.IO;
using UnityEditor;
using UnityEngine;

public class HeartSpriteGenerator
{
    private const string ITEM_DIR = "Assets/tainguyen/item";

    // 9x9 pixel matrix representations for Minecraft Heart
    // 0 = Transparent
    // 1 = Dark Border
    // 2 = White Highlight
    // 3 = Light Pink/Red Accent
    // 4 = Main Red
    // 5 = Dark Red Shading
    // 6 = Empty Inner Dark
    // 7 = Empty Inner Dark Shadow
    // 8 = Enemy Pink/Purple Highlight
    // 9 = Enemy Purple Main
    // 10 = Enemy Purple Shadow

    private static readonly int[,] fullGrid = new int[9, 9]
    {
        {0, 1, 1, 0, 0, 0, 1, 1, 0},
        {1, 2, 3, 1, 0, 1, 4, 4, 1},
        {1, 2, 4, 4, 1, 4, 4, 5, 1},
        {1, 4, 4, 4, 4, 4, 4, 5, 1},
        {0, 1, 4, 4, 4, 4, 5, 1, 0},
        {0, 0, 1, 4, 4, 5, 1, 0, 0},
        {0, 0, 0, 1, 4, 5, 1, 0, 0},
        {0, 0, 0, 0, 1, 1, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 0}
    };

    private static readonly int[,] halfGrid = new int[9, 9]
    {
        {0, 1, 1, 0, 0, 0, 1, 1, 0},
        {1, 2, 3, 1, 0, 1, 6, 6, 1},
        {1, 2, 4, 4, 1, 6, 6, 7, 1},
        {1, 4, 4, 4, 4, 6, 6, 7, 1},
        {0, 1, 4, 4, 4, 6, 7, 1, 0},
        {0, 0, 1, 4, 4, 7, 1, 0, 0},
        {0, 0, 0, 1, 4, 7, 1, 0, 0},
        {0, 0, 0, 0, 1, 1, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 0}
    };

    private static readonly int[,] emptyGrid = new int[9, 9]
    {
        {0, 1, 1, 0, 0, 0, 1, 1, 0},
        {1, 6, 6, 1, 0, 1, 6, 6, 1},
        {1, 6, 6, 6, 1, 6, 6, 7, 1},
        {1, 6, 6, 6, 6, 6, 6, 7, 1},
        {0, 1, 6, 6, 6, 6, 7, 1, 0},
        {0, 0, 1, 6, 6, 7, 1, 0, 0},
        {0, 0, 0, 1, 6, 7, 1, 0, 0},
        {0, 0, 0, 0, 1, 1, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 0}
    };

    private static readonly int[,] enemyGrid = new int[9, 9]
    {
        {0, 1, 1, 0, 0, 0, 1, 1, 0},
        {1, 2, 8, 1, 0, 1, 9, 9, 1},
        {1, 2, 9, 9, 1, 9, 9, 10, 1},
        {1, 9, 9, 9, 9, 9, 9, 10, 1},
        {0, 1, 9, 9, 9, 9, 10, 1, 0},
        {0, 0, 1, 9, 9, 10, 1, 0, 0},
        {0, 0, 0, 1, 9, 10, 1, 0, 0},
        {0, 0, 0, 0, 1, 1, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 0}
    };

    [MenuItem("Tools/Minecraft Heart System/Generate Heart Sprites")]
    public static void GenerateAllHeartSprites()
    {
        if (!Directory.Exists(ITEM_DIR))
        {
            Directory.CreateDirectory(ITEM_DIR);
        }

        CreateAndSaveHeartTexture(fullGrid, Path.Combine(ITEM_DIR, "heart_full.png"));
        CreateAndSaveHeartTexture(halfGrid, Path.Combine(ITEM_DIR, "heart_half.png"));
        CreateAndSaveHeartTexture(emptyGrid, Path.Combine(ITEM_DIR, "heart_empty.png"));
        CreateAndSaveHeartTexture(enemyGrid, Path.Combine(ITEM_DIR, "heart_enemy.png"));

        AssetDatabase.Refresh();

        // Configure Importers
        ConfigureSpriteImporter(Path.Combine(ITEM_DIR, "heart_full.png"));
        ConfigureSpriteImporter(Path.Combine(ITEM_DIR, "heart_half.png"));
        ConfigureSpriteImporter(Path.Combine(ITEM_DIR, "heart_empty.png"));
        ConfigureSpriteImporter(Path.Combine(ITEM_DIR, "heart_enemy.png"));

        AssetDatabase.Refresh();
        Debug.Log("<color=green>[HeartSpriteGenerator]</color> Đã tạo thành công 4 ảnh trái tim Minecraft PNG nền trong suốt tại: " + ITEM_DIR);
    }

    private static void CreateAndSaveHeartTexture(int[,] grid, string savePath, int scale = 4)
    {
        int srcH = grid.GetLength(0);
        int srcW = grid.GetLength(1);
        int texW = srcW * scale;
        int texH = srcH * scale;

        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color transparent = new Color(0, 0, 0, 0);
        Color border = new Color(0.08f, 0.02f, 0.02f, 1.0f);
        Color white = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        Color pinkAccent = new Color(1.0f, 0.6f, 0.6f, 1.0f);
        Color mainRed = new Color(0.88f, 0.05f, 0.05f, 1.0f);
        Color darkRed = new Color(0.55f, 0.0f, 0.0f, 1.0f);
        Color emptyInner = new Color(0.18f, 0.12f, 0.12f, 0.85f);
        Color emptyShadow = new Color(0.10f, 0.06f, 0.06f, 0.85f);
        
        // Enemy colors
        Color enemyAccent = new Color(0.85f, 0.45f, 0.95f, 1.0f);
        Color enemyMain = new Color(0.60f, 0.10f, 0.75f, 1.0f);
        Color enemyShadow = new Color(0.35f, 0.02f, 0.45f, 1.0f);

        for (int r = 0; r < srcH; r++)
        {
            for (int c = 0; c < srcW; c++)
            {
                int val = grid[r, c];
                Color pixelColor = transparent;

                switch (val)
                {
                    case 1: pixelColor = border; break;
                    case 2: pixelColor = white; break;
                    case 3: pixelColor = pinkAccent; break;
                    case 4: pixelColor = mainRed; break;
                    case 5: pixelColor = darkRed; break;
                    case 6: pixelColor = emptyInner; break;
                    case 7: pixelColor = emptyShadow; break;
                    case 8: pixelColor = enemyAccent; break;
                    case 9: pixelColor = enemyMain; break;
                    case 10: pixelColor = enemyShadow; break;
                }

                // Invert row index because Texture2D (0,0) is bottom-left
                int flippedRow = (srcH - 1) - r;

                for (int sy = 0; sy < scale; sy++)
                {
                    for (int sx = 0; sx < scale; sx++)
                    {
                        tex.SetPixel(c * scale + sx, flippedRow * scale + sy, pixelColor);
                    }
                }
            }
        }

        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(savePath, bytes);
        Object.DestroyImmediate(tex);
    }

    private static void ConfigureSpriteImporter(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
