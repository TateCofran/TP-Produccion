using System.Collections.Generic;
using UnityEngine;

public static class TileIconGenerator
{
    public const int CellPixels = 16;

    private const int EntryBorderPixels = 2;
    private const int ExitDotPixels = 6;

    private static readonly Dictionary<TileLayout, Texture2D> cache = new();
    private static TileIconPalette palette;
    private static TileIconPalette defaultPalette;

    public static TileIconPalette Palette
    {
        get
        {
            if (palette != null) return palette;
            if (defaultPalette == null)
            {
                defaultPalette = ScriptableObject.CreateInstance<TileIconPalette>();
                defaultPalette.name = "TileIconPaletteDefault";
                defaultPalette.hideFlags = HideFlags.DontSave;
            }
            return defaultPalette;
        }
    }

    public static void SetPalette(TileIconPalette newPalette)
    {
        if (newPalette == palette) return;
        palette = newPalette;
        ClearCache();
    }

    public static Texture2D GetIcon(TileLayout layout)
    {
        if (layout == null) return null;
        if (cache.TryGetValue(layout, out var cached) && cached != null) return cached;

        var tex = Build(layout);
        cache[layout] = tex;
        return tex;
    }

    public static void ClearCache()
    {
        foreach (var tex in cache.Values)
        {
            if (tex == null) continue;
            if (Application.isPlaying) Object.Destroy(tex);
            else Object.DestroyImmediate(tex);
        }
        cache.Clear();
    }

    private static Texture2D Build(TileLayout layout)
    {
        var pal = Palette;
        int w = Mathf.Max(1, layout.gridWidth) * CellPixels;
        int h = Mathf.Max(1, layout.gridHeight) * CellPixels;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"TileIcon_{layout.name}",
            hideFlags = HideFlags.DontSave
        };

        var pixels = new Color32[w * h];

        for (int y = 0; y < layout.gridHeight; y++)
            for (int x = 0; x < layout.gridWidth; x++)
            {
                var cell = new Vector2Int(x, y);
                Color color = layout.IsPath(cell) ? pal.GetPathColor(layout.GetPathModifiers(cell)) : pal.grass;
                FillRect(pixels, w, x * CellPixels, y * CellPixels, CellPixels, CellPixels, color);
            }

        if (layout.IsInside(layout.entry))
            DrawBorder(pixels, w, layout.entry.x * CellPixels, layout.entry.y * CellPixels, CellPixels, EntryBorderPixels, pal.marker);

        if (layout.exits != null)
        {
            int offset = (CellPixels - ExitDotPixels) / 2;
            foreach (var exit in layout.exits)
            {
                if (!layout.IsInside(exit)) continue;
                FillRect(pixels, w, exit.x * CellPixels + offset, exit.y * CellPixels + offset, ExitDotPixels, ExitDotPixels, pal.marker);
            }
        }

        if (layout.IsDeadEnd)
        {
            var end = FarthestPathCell(layout);
            DrawCross(pixels, w, end.x * CellPixels, end.y * CellPixels, CellPixels, pal.deadEndMark);
        }

        if (layout.activeIn != WorldAffinity.Neutral)
        {
            var mark = layout.activeIn == WorldAffinity.Normal ? pal.normalWorldMark : pal.otherWorldMark;
            int size = CellPixels;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size - y; x++)
                    pixels[(h - 1 - y) * w + (w - size + x + y)] = mark;
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static void FillRect(Color32[] pixels, int texWidth, int x0, int y0, int width, int height, Color color)
    {
        Color32 c = color;
        int texHeight = pixels.Length / texWidth;
        for (int y = y0; y < y0 + height; y++)
        {
            if (y < 0 || y >= texHeight) continue;
            for (int x = x0; x < x0 + width; x++)
            {
                if (x < 0 || x >= texWidth) continue;
                pixels[y * texWidth + x] = c;
            }
        }
    }

    private static Vector2Int FarthestPathCell(TileLayout layout)
    {
        Vector2Int best = layout.entry;
        int bestDist = -1;
        for (int y = 0; y < layout.gridHeight; y++)
            for (int x = 0; x < layout.gridWidth; x++)
            {
                var c = new Vector2Int(x, y);
                if (!layout.IsPath(c)) continue;
                int d = Mathf.Abs(c.x - layout.entry.x) + Mathf.Abs(c.y - layout.entry.y);
                if (d > bestDist) { bestDist = d; best = c; }
            }
        return best;
    }

    private static void DrawCross(Color32[] pixels, int texWidth, int x0, int y0, int size, Color color)
    {
        Color32 c = color;
        int texHeight = pixels.Length / texWidth;
        for (int i = 2; i < size - 2; i++)
        {
            for (int t = -1; t <= 1; t++)
            {
                int ax = x0 + i + t, ay = y0 + i;
                int bx = x0 + size - 1 - i + t, by = y0 + i;
                if (ax >= 0 && ax < texWidth && ay >= 0 && ay < texHeight) pixels[ay * texWidth + ax] = c;
                if (bx >= 0 && bx < texWidth && by >= 0 && by < texHeight) pixels[by * texWidth + bx] = c;
            }
        }
    }

    private static void DrawBorder(Color32[] pixels, int texWidth, int x0, int y0, int size, int thickness, Color color)
    {
        FillRect(pixels, texWidth, x0, y0, size, thickness, color);
        FillRect(pixels, texWidth, x0, y0 + size - thickness, size, thickness, color);
        FillRect(pixels, texWidth, x0, y0, thickness, size, color);
        FillRect(pixels, texWidth, x0 + size - thickness, y0, thickness, size, color);
    }
}
