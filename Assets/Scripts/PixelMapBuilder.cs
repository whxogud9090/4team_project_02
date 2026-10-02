using UnityEngine;

/// <summary>Creates one connected fishing lake from the Pixel Crawler Water_tiles source.</summary>
[ExecuteAlways]
public sealed class PixelMapBuilder : MonoBehaviour
{
    public Vector2 PlayerStart { get; private set; } = new(0f, 4.2f);
    public Vector2 FishingSpot { get; private set; } = new(0f, 3.15f);
    public Vector2 BobberSpot { get; private set; } = new(0f, -.8f);

    private void OnEnable()
    {
        // Keep the new art visible in the Scene view without rebuilding or deleting
        // any of the existing fishing preview objects.
        if (!Application.isPlaying) CreateDecorations();
    }

    public void EnsureDecorations()
    {
        CreateDecorations();
    }

    public void Build()
    {
        var source = Resources.Load<Texture2D>("PixelCrawlerWaterTiles");
        if (source == null) { Debug.LogError("PixelCrawlerWaterTiles is missing from Resources."); return; }
        source.filterMode = FilterMode.Point;
        var lakeTexture = BuildLakeTexture(source);
        CreateGround();

        var lake = new GameObject("Fishing Lake");
        lake.transform.SetParent(transform, false);
        lake.transform.position = new Vector2(0f, -1.8f);
        var renderer = lake.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(lakeTexture, new Rect(0, 0, 224, 160), new Vector2(.5f, .5f), 16f);
        renderer.sortingOrder = -2;
        var collider = lake.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(13.5f, 9.5f);

        CreateDecorations();
    }

    private void CreateDecorations()
    {
        if (transform.Find("Art Decorations") != null) return;

        var treeTexture = Resources.Load<Texture2D>("Sprites/Environment/Tree1");
        var grassTexture = Resources.Load<Texture2D>("Sprites/Environment/grass1");
        if (treeTexture == null || grassTexture == null) return;

        treeTexture.filterMode = FilterMode.Point;
        grassTexture.filterMode = FilterMode.Point;

        var root = new GameObject("Art Decorations");
        root.transform.SetParent(transform, false);

        Vector2[] treePositions =
        {
            new(-5.8f, 5.2f), new(5.9f, 5.5f), new(-8.2f, 1.2f), new(8.3f, 1.5f)
        };
        foreach (var position in treePositions)
            CreateDecoration(root.transform, "Tree", treeTexture, position, 32f, 2, true);

        Vector2[] grassPositions =
        {
            new(-4.5f, 4.1f), new(-2.8f, 5.6f), new(2.9f, 4.5f), new(4.7f, 6.0f),
            new(-7.3f, 3.8f), new(7.1f, 3.9f), new(-9.0f, -2.8f), new(9.1f, -3.2f)
        };
        foreach (var position in grassPositions)
            CreateDecoration(root.transform, "Grass", grassTexture, position, 32f, 0, false);
    }

    private static void CreateDecoration(
        Transform parent,
        string objectName,
        Texture2D texture,
        Vector2 position,
        float pixelsPerUnit,
        int sortingOrder,
        bool blocksPlayer)
    {
        var decoration = new GameObject(objectName);
        decoration.transform.SetParent(parent, false);
        decoration.transform.position = position;

        var renderer = decoration.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            blocksPlayer ? new Vector2(.5f, 0f) : new Vector2(.5f, .5f),
            pixelsPerUnit);
        renderer.sortingOrder = sortingOrder;

        if (!blocksPlayer) return;
        var collider = decoration.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(.55f, .42f);
        collider.offset = new Vector2(0f, .2f);
    }

    private void CreateGround()
    {
        var ground = new GameObject("Ground");
        ground.transform.SetParent(transform, false);
        ground.transform.position = new Vector2(0f, -1f);
        var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
        texture.SetPixel(0, 0, new Color(.35f, .60f, .24f)); texture.Apply();
        var renderer = ground.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
        renderer.sortingOrder = -5;
        ground.transform.localScale = new Vector3(30f, 24f, 1f);
    }

    private static Texture2D BuildLakeTexture(Texture2D source)
    {
        // Inspected from Water_tiles.png: this fully opaque 16px area is the repeating water-surface center.
        Color[] center = source.GetPixels(16, 184, 16, 16);
        var lake = new Texture2D(224, 160, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 160; y += 16)
            for (int x = 0; x < 224; x += 16) lake.SetPixels(x, y, 16, 16, center);
        lake.Apply();
        return lake;
    }
}
