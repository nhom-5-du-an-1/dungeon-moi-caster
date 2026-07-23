using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapGenerator : MonoBehaviour
{
    [Header("Map Dimensions")]
    public int mapWidth = 60;
    public int mapHeight = 60;
    public float tileSize = 0.32f;

    [Header("Tree Settings")]
    [Range(0f, 1f)]
    public float treeDensity = 0.03f;
    [Range(0f, 1f)]
    public float bushDensity = 0.02f;

    [Header("Wall Settings")]
    public int wallClusterCount = 8;
    [Range(3, 12)]
    public int wallClusterMinSize = 4;
    [Range(5, 20)]
    public int wallClusterMaxSize = 10;

    [Header("Sprites")]
    public List<Sprite> grassSprites = new List<Sprite>();
    public List<Sprite> stoneSprites = new List<Sprite>();
    public List<Sprite> treeSprites = new List<Sprite>();
    public List<Sprite> shadowSprites = new List<Sprite>();
    public List<Sprite> wallSprites = new List<Sprite>();
    public List<Sprite> propsSprites = new List<Sprite>();
    public List<Sprite> structSprites = new List<Sprite>();

    // Offset definitions for shadow plants 0 to 8 matching plant 0 to 8
    private static readonly Vector3[] ShadowOffsets = new Vector3[]
    {
        new Vector3(0.24f, 0.01f, 0f),   // Plant 0
        new Vector3(0.12f, 0.17f, 0f),   // Plant 1
        new Vector3(0.09f, 0.00f, 0f),   // Plant 2
        new Vector3(0.01f, -0.02f, 0f),  // Plant 3
        new Vector3(0.01f, -0.02f, 0f),  // Plant 4
        new Vector3(0.04f, -0.01f, 0f),  // Plant 5
        new Vector3(0.04f, -0.02f, 0f),  // Plant 6
        new Vector3(0.03f, -0.02f, 0f),  // Plant 7
        new Vector3(0.02f, -0.02f, 0f)   // Plant 8
    };

    // Occupancy grid: true = occupied (wall/tree already here)
    private bool[,] occupied;

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        ClearMap();
        LoadSpritesIfNeeded();

        occupied = new bool[mapWidth, mapHeight];

        // Camera setup
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.18f, 0.22f, 0.15f, 1f);
            float camX = (mapWidth * tileSize) / 2f;
            float camY = (mapHeight * tileSize) / 2f;
            mainCam.transform.position = new Vector3(camX, camY, -10f);
            mainCam.orthographicSize = (mapHeight * tileSize) / 2f + 0.5f;
        }

        // Parent GameObjects
        GameObject groundParent = new GameObject("Ground");
        groundParent.transform.SetParent(transform);

        GameObject wallsParent = new GameObject("Walls");
        wallsParent.transform.SetParent(transform);

        GameObject treesParent = new GameObject("Trees & Plants");
        treesParent.transform.SetParent(transform);

        int centerX = mapWidth / 2;
        int centerY = mapHeight / 2;
        int arenaRadius = 12; // Stone arena floor radius
        int[] stoneIndices = new int[] { 0, 1, 2, 3, 4, 5 };

        // =============================================
        // STEP 1: Lay down the ground (grass + stone arena)
        // =============================================
        int[] grassIndices = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                Vector3 pos = new Vector3(x * tileSize, y * tileSize, 0);

                // Check if inside stone arena circle
                float dist = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                bool isArena = dist <= arenaRadius;

                Sprite floorTile;
                if (isArena && stoneSprites.Count > 0)
                {
                    int idx = stoneIndices[Random.Range(0, stoneIndices.Length)];
                    floorTile = (idx < stoneSprites.Count) ? stoneSprites[idx] : stoneSprites[0];
                }
                else
                {
                    floorTile = GetRandomGrassSprite(grassIndices);
                }
                if (floorTile == null) continue;

                GameObject tileObj = new GameObject($"Tile_{x}_{y}");
                tileObj.transform.position = pos;
                tileObj.transform.SetParent(groundParent.transform);

                SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                sr.sprite = floorTile;
                sr.sortingOrder = -10000;
            }
        }

        // =============================================
        // STEP 2: Solid tree wall border (acts like an impassable fence)
        // =============================================
        int wallThickness = 2;
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                bool isEdge = (x < wallThickness || x >= mapWidth - wallThickness ||
                               y < wallThickness || y >= mapHeight - wallThickness);
                if (!isEdge) continue;
                if (occupied[x, y]) continue;

                occupied[x, y] = true;
                Vector3 pos = new Vector3(x * tileSize, y * tileSize, 0);
                SpawnTree(pos, treesParent.transform);
            }
        }

        // =============================================
        // STEP 3: Arena wall pillars in a ring around the stone floor
        // =============================================
        if (wallSprites.Count > 0)
        {
            int pillarRadius = arenaRadius + 2;
            for (int angle = 0; angle < 360; angle += 15)
            {
                float rad = angle * Mathf.Deg2Rad;
                int px = centerX + Mathf.RoundToInt(Mathf.Cos(rad) * pillarRadius);
                int py = centerY + Mathf.RoundToInt(Mathf.Sin(rad) * pillarRadius);

                if (px < wallThickness || px >= mapWidth - wallThickness || py < wallThickness || py >= mapHeight - wallThickness) continue;
                if (occupied[px, py]) continue;
                occupied[px, py] = true;

                Vector3 pos = new Vector3(px * tileSize, py * tileSize, 0);
                int[] wallIndices = { 0, 8, 9 };
                int wallIdx = wallIndices[Random.Range(0, wallIndices.Length)];
                Sprite wallSprite = (wallIdx < wallSprites.Count) ? wallSprites[wallIdx] : wallSprites[0];

                GameObject wallObj = new GameObject($"ArenaPillar_{px}_{py}");
                wallObj.transform.position = pos;
                wallObj.transform.SetParent(wallsParent.transform);

                SpriteRenderer sr = wallObj.AddComponent<SpriteRenderer>();
                sr.sprite = wallSprite;
                sr.sortingOrder = 10000 - Mathf.RoundToInt(pos.y * 100);

                BoxCollider2D bc = wallObj.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(tileSize, tileSize);
            }
        }

        // =============================================
        // STEP 4: Scatter trees in the grass zone (between border and arena)
        // =============================================
        int innerMargin = wallThickness + 1;
        for (int y = innerMargin; y < mapHeight - innerMargin; y++)
        {
            for (int x = innerMargin; x < mapWidth - innerMargin; x++)
            {
                if (occupied[x, y]) continue;

                // Don't place trees inside the arena
                float dist = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                if (dist <= arenaRadius + 3) continue;

                if (Random.value < treeDensity)
                {
                    Vector3 pos = new Vector3(x * tileSize, y * tileSize, 0);
                    SpawnTree(pos, treesParent.transform);
                    occupied[x, y] = true;
                }
            }
        }

        // =============================================
        // STEP 5: Scatter bushes in grass zone
        // =============================================
        for (int y = innerMargin; y < mapHeight - innerMargin; y++)
        {
            for (int x = innerMargin; x < mapWidth - innerMargin; x++)
            {
                if (occupied[x, y]) continue;

                float dist = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                if (dist <= arenaRadius + 3) continue;

                if (Random.value < bushDensity)
                {
                    Vector3 pos = new Vector3(x * tileSize, y * tileSize, 0);
                    SpawnBush(pos, treesParent.transform);
                    occupied[x, y] = true;
                }
            }
        }

        Debug.Log($"Boss arena map generated: {mapWidth}x{mapHeight}, arena radius {arenaRadius}.");
    }

    // =========================================================================
    // Wall Cluster Generation
    // =========================================================================
    private void PlaceWallCluster(Transform parent)
    {
        if (wallSprites.Count == 0) return;

        // Pick a random center point (keep away from borders to leave room for trees)
        int cx = Random.Range(8, mapWidth - 8);
        int cy = Random.Range(8, mapHeight - 8);

        int clusterSize = Random.Range(wallClusterMinSize, wallClusterMaxSize + 1);

        // Determine wall cluster shape: 0 = horizontal line, 1 = vertical line, 2 = L-shape, 3 = rectangle
        int shape = Random.Range(0, 4);

        List<Vector2Int> wallPositions = new List<Vector2Int>();

        switch (shape)
        {
            case 0: // Horizontal line
                for (int i = 0; i < clusterSize; i++)
                    wallPositions.Add(new Vector2Int(cx + i, cy));
                break;

            case 1: // Vertical line
                for (int i = 0; i < clusterSize; i++)
                    wallPositions.Add(new Vector2Int(cx, cy + i));
                break;

            case 2: // L-shape
                int half = clusterSize / 2;
                for (int i = 0; i < half; i++)
                    wallPositions.Add(new Vector2Int(cx + i, cy));
                for (int i = 1; i <= clusterSize - half; i++)
                    wallPositions.Add(new Vector2Int(cx, cy + i));
                break;

            case 3: // Small rectangle
                int w = Mathf.Max(2, clusterSize / 3);
                int h = Mathf.Max(2, clusterSize / w);
                for (int wy = 0; wy < h; wy++)
                {
                    for (int wx = 0; wx < w; wx++)
                    {
                        // Only place perimeter walls for the rectangle
                        if (wy == 0 || wy == h - 1 || wx == 0 || wx == w - 1)
                            wallPositions.Add(new Vector2Int(cx + wx, cy + wy));
                    }
                }
                break;
        }

        // Spawn walls
        foreach (Vector2Int wp in wallPositions)
        {
            if (wp.x < 0 || wp.x >= mapWidth || wp.y < 0 || wp.y >= mapHeight) continue;
            if (occupied[wp.x, wp.y]) continue;

            occupied[wp.x, wp.y] = true;

            Vector3 pos = new Vector3(wp.x * tileSize, wp.y * tileSize, 0);

            // Pick a wall sprite - use indices 0, 8, 9 for variety
            int[] wallIndices = { 0, 8, 9 };
            int wallIdx = wallIndices[Random.Range(0, wallIndices.Length)];
            Sprite wallSprite = (wallIdx < wallSprites.Count) ? wallSprites[wallIdx] : wallSprites[0];

            GameObject wallObj = new GameObject($"Wall_{wp.x}_{wp.y}");
            wallObj.transform.position = pos;
            wallObj.transform.SetParent(parent);

            SpriteRenderer sr = wallObj.AddComponent<SpriteRenderer>();
            sr.sprite = wallSprite;
            sr.sortingOrder = 10000 - Mathf.RoundToInt(pos.y * 100);

            BoxCollider2D bc = wallObj.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(tileSize, tileSize);
        }
    }

    // =========================================================================
    // Grass Tile Helper
    // =========================================================================
    private Sprite GetRandomGrassSprite(int[] indices)
    {
        if (grassSprites.Count == 0) return null;
        int idx = indices[Random.Range(0, indices.Length)];
        if (idx < grassSprites.Count) return grassSprites[idx];
        return grassSprites[0];
    }

    // =========================================================================
    // Tree & Bush Spawning
    // =========================================================================
    private void SpawnTree(Vector3 position, Transform parent)
    {
        int maxIndex = Mathf.Min(treeSprites.Count, shadowSprites.Count, ShadowOffsets.Length);
        if (maxIndex == 0) return;

        int index = Random.Range(0, Mathf.Min(3, maxIndex)); // Big trees (0 to 2)

        GameObject treeObj = new GameObject("Tree");
        treeObj.transform.position = position;
        treeObj.transform.SetParent(parent);

        SpriteRenderer treeSr = treeObj.AddComponent<SpriteRenderer>();
        treeSr.sprite = treeSprites[index];
        int sortingOrder = 10000 - Mathf.RoundToInt(position.y * 100);
        treeSr.sortingOrder = sortingOrder;

        CircleCollider2D col = treeObj.AddComponent<CircleCollider2D>();
        col.radius = 0.15f;
        col.offset = new Vector2(tileSize * 0.5f, tileSize * 0.3f);

        // Shadow child
        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(treeObj.transform);
        shadowObj.transform.localPosition = ShadowOffsets[index];

        SpriteRenderer shadowSr = shadowObj.AddComponent<SpriteRenderer>();
        shadowSr.sprite = shadowSprites[index];
        shadowSr.sortingOrder = sortingOrder - 1;
    }

    private void SpawnBush(Vector3 position, Transform parent)
    {
        int maxIndex = Mathf.Min(treeSprites.Count, shadowSprites.Count, ShadowOffsets.Length);
        if (maxIndex == 0) return;

        int index = Random.Range(3, Mathf.Min(9, maxIndex)); // Small bushes/plants (3 to 8)

        GameObject bushObj = new GameObject("Bush");
        bushObj.transform.position = position;
        bushObj.transform.SetParent(parent);

        SpriteRenderer bushSr = bushObj.AddComponent<SpriteRenderer>();
        bushSr.sprite = treeSprites[index];
        int sortingOrder = 10000 - Mathf.RoundToInt(position.y * 100);
        bushSr.sortingOrder = sortingOrder;

        // Shadow child
        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(bushObj.transform);
        shadowObj.transform.localPosition = ShadowOffsets[index];

        SpriteRenderer shadowSr = shadowObj.AddComponent<SpriteRenderer>();
        shadowSr.sprite = shadowSprites[index];
        shadowSr.sortingOrder = sortingOrder - 1;
    }

    // =========================================================================
    // Clear
    // =========================================================================
    [ContextMenu("Clear Map")]
    public void ClearMap()
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }
        foreach (GameObject child in children)
        {
            DestroyImmediate(child);
        }
    }

    // =========================================================================
    // Sprite Loading
    // =========================================================================
    private void LoadSpritesIfNeeded()
    {
#if UNITY_EDITOR
        if (grassSprites.Count == 0)
        {
            object[] grassAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/map/TX Tileset Grass.png");
            foreach (object asset in grassAssets)
            {
                if (asset is Sprite sprite) grassSprites.Add(sprite);
            }
            grassSprites.Sort((a, b) => NaturalCompare(a.name, b.name));
        }

        if (stoneSprites.Count == 0)
        {
            object[] stoneAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/map/TX Tileset Stone Ground.png");
            foreach (object asset in stoneAssets)
            {
                if (asset is Sprite sprite) stoneSprites.Add(sprite);
            }
            stoneSprites.Sort((a, b) => NaturalCompare(a.name, b.name));
        }

        if (treeSprites.Count == 0)
        {
            object[] treeAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/map/TX Plant.png");
            foreach (object asset in treeAssets)
            {
                if (asset is Sprite sprite) treeSprites.Add(sprite);
            }
            treeSprites.Sort((a, b) => NaturalCompare(a.name, b.name));
        }

        if (shadowSprites.Count == 0)
        {
            object[] shadowAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/map/TX Shadow Plant.png");
            foreach (object asset in shadowAssets)
            {
                if (asset is Sprite sprite) shadowSprites.Add(sprite);
            }
            shadowSprites.Sort((a, b) => NaturalCompare(a.name, b.name));
        }

        if (wallSprites.Count == 0)
        {
            object[] wallAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/map/TX Tileset Wall.png");
            foreach (object asset in wallAssets)
            {
                if (asset is Sprite sprite) wallSprites.Add(sprite);
            }
            wallSprites.Sort((a, b) => NaturalCompare(a.name, b.name));
        }

        if (propsSprites.Count == 0)
        {
            object[] propsAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/map/TX Props.png");
            foreach (object asset in propsAssets)
            {
                if (asset is Sprite sprite) propsSprites.Add(sprite);
            }
            propsSprites.Sort((a, b) => NaturalCompare(a.name, b.name));
        }

        if (structSprites.Count == 0)
        {
            object[] structAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/map/TX Struct.png");
            foreach (object asset in structAssets)
            {
                if (asset is Sprite sprite) structSprites.Add(sprite);
            }
            structSprites.Sort((a, b) => NaturalCompare(a.name, b.name));
        }
#endif
    }

    private int NaturalCompare(string x, string y)
    {
        return System.Text.RegularExpressions.Regex.Replace(x, @"\d+", m => m.Value.PadLeft(10, '0'))
            .CompareTo(System.Text.RegularExpressions.Regex.Replace(y, @"\d+", m => m.Value.PadLeft(10, '0')));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapGenerator generator = (MapGenerator)target;
        EditorGUILayout.Space();

        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
        if (GUILayout.Button("Generate Map", GUILayout.Height(35)))
        {
            Undo.RegisterCreatedObjectUndo(generator.gameObject, "Generate Map");
            generator.GenerateMap();
            EditorUtility.SetDirty(generator);
        }

        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
        if (GUILayout.Button("Clear Map", GUILayout.Height(25)))
        {
            Undo.RegisterCreatedObjectUndo(generator.gameObject, "Clear Map");
            generator.ClearMap();
            EditorUtility.SetDirty(generator);
        }
    }
}
#endif
