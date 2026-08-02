using System.IO;
using UnityEditor;
using UnityEngine;

public class XpSpriteGenerator
{
    private const string TARGET_DIR = "Assets/tainguyen/quai/Orc";

    [MenuItem("Tools/Minecraft XP System/Generate XP Sprites")]
    public static void GenerateXpSprites()
    {
        if (!Directory.Exists(TARGET_DIR))
        {
            Directory.CreateDirectory(TARGET_DIR);
        }

        string emptyPath = Path.Combine(TARGET_DIR, "xp_bar_empty.png");
        string fillPath = Path.Combine(TARGET_DIR, "xp_bar_fill.png");
        string orbPath = Path.Combine(TARGET_DIR, "xp_orb.png");

        CreateAndSaveXpBarTexture(false, emptyPath);
        CreateAndSaveXpBarTexture(true, fillPath);
        CreateAndSaveXpOrbTexture(orbPath);

        AssetDatabase.Refresh();

        ConfigureSpriteImporter(emptyPath);
        ConfigureSpriteImporter(fillPath);
        ConfigureSpriteImporter(orbPath);

        AssetDatabase.Refresh();

        // Tạo prefab XP Orb trong thư mục Resources để load động được cả lúc build game
        CreateXpOrbPrefab(orbPath);

        AssetDatabase.Refresh();

        Debug.Log("<color=green>[XpSpriteGenerator]</color> Đã tạo thành công các tệp ảnh XP Minecraft PNG sắc nét và Prefab Orb tại: " + TARGET_DIR);
    }

    private static void CreateXpOrbPrefab(string orbSpritePath)
    {
        string resourcesDir = "Assets/Resources";
        if (!Directory.Exists(resourcesDir))
        {
            Directory.CreateDirectory(resourcesDir);
        }

        string prefabPath = Path.Combine(resourcesDir, "XpOrbPrefab.prefab");
        
        GameObject tempOrb = new GameObject("XpOrbPrefab");
        
        SpriteRenderer sr = tempOrb.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(orbSpritePath);
        sr.sortingOrder = 900;
        
        CircleCollider2D col = tempOrb.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.25f;
        
        Rigidbody2D rb = tempOrb.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 1.0f;
        
        tempOrb.AddComponent<XpOrb>();
        
        PrefabUtility.SaveAsPrefabAsset(tempOrb, prefabPath);
        Object.DestroyImmediate(tempOrb);
        
        Debug.Log("<color=green>[XpSpriteGenerator]</color> Đã tự động tạo và cấu hình Prefab: <b>" + prefabPath + "</b>");
    }

    private static void CreateAndSaveXpBarTexture(bool isFill, string savePath, int scale = 4)
    {
        int srcW = 182;
        int srcH = 5;
        int texW = srcW * scale;
        int texH = srcH * scale;

        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color border = new Color(0.12f, 0.12f, 0.12f, 1.0f);
        Color emptyBg = new Color(0.24f, 0.24f, 0.24f, 1.0f);
        Color fillBg = new Color(0.33f, 1.0f, 0.33f, 1.0f);
        Color fillShadow = new Color(0.0f, 0.72f, 0.0f, 1.0f);

        for (int r = 0; r < srcH; r++)
        {
            for (int c = 0; c < srcW; c++)
            {
                Color pixelColor;

                if (c == 0 || c == srcW - 1 || r == 0 || r == srcH - 1)
                {
                    pixelColor = border;
                }
                else
                {
                    if (isFill)
                    {
                        if (r == 1)
                        {
                            pixelColor = fillShadow;
                        }
                        else
                        {
                            pixelColor = fillBg;
                        }
                    }
                    else
                    {
                        pixelColor = emptyBg;
                    }
                }

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

    private static void CreateAndSaveXpOrbTexture(string savePath, int scale = 4)
    {
        int srcSize = 16;
        int texSize = srcSize * scale;

        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color trans = new Color(0f, 0f, 0f, 0f);
        Color border = new Color(0f, 0.16f, 0f, 1.0f);
        Color green = new Color(0f, 0.65f, 0f, 1.0f);
        Color lime = new Color(0.35f, 1.0f, 0.35f, 1.0f);
        Color yellow = new Color(0.85f, 1.0f, 0.15f, 1.0f);
        Color white = new Color(1.0f, 1.0f, 0.75f, 1.0f);

        int[,] grid = new int[16, 16]
        {
            {0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0},
            {0, 0, 0, 1, 1, 2, 2, 2, 2, 2, 2, 1, 1, 0, 0, 0},
            {0, 0, 1, 2, 2, 3, 3, 3, 3, 3, 2, 2, 2, 1, 0, 0},
            {0, 1, 2, 3, 3, 4, 4, 4, 4, 3, 3, 2, 2, 2, 1, 0},
            {0, 1, 2, 3, 4, 5, 5, 5, 4, 4, 3, 3, 2, 2, 1, 0},
            {1, 2, 3, 4, 5, 5, 5, 5, 4, 4, 4, 3, 3, 2, 2, 1},
            {1, 2, 3, 4, 5, 5, 5, 5, 5, 4, 4, 4, 3, 2, 2, 1},
            {1, 2, 3, 4, 5, 5, 5, 5, 5, 4, 4, 4, 3, 2, 2, 1},
            {1, 2, 3, 4, 4, 5, 5, 5, 4, 4, 4, 3, 3, 2, 2, 1},
            {1, 2, 2, 3, 4, 4, 4, 4, 4, 4, 3, 3, 2, 2, 2, 1},
            {0, 1, 2, 2, 3, 3, 4, 4, 4, 3, 3, 2, 2, 2, 1, 0},
            {0, 1, 2, 2, 2, 3, 3, 3, 3, 3, 2, 2, 2, 1, 0, 0},
            {0, 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 0, 0, 0},
            {0, 0, 0, 1, 1, 2, 2, 2, 2, 2, 1, 1, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        };

        for (int r = 0; r < srcSize; r++)
        {
            for (int c = 0; c < srcSize; c++)
            {
                int val = grid[r, c];
                Color pixelColor = trans;

                switch (val)
                {
                    case 1: pixelColor = border; break;
                    case 2: pixelColor = green; break;
                    case 3: pixelColor = lime; break;
                    case 4: pixelColor = yellow; break;
                    case 5: pixelColor = white; break;
                }

                int flippedRow = (srcSize - 1) - r;

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
