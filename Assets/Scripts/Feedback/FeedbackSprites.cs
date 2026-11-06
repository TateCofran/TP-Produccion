using System.Collections.Generic;
using UnityEngine;

public static class FeedbackSprites
{
    private const int Size = 32;
    private static readonly Dictionary<string, Sprite> cache = new();

    public static Sprite Drop => Get("Drop", DropMask);
    public static Sprite Flame => Get("Flame", FlameMask);
    public static Sprite Circle => Get("Circle", CircleMask);
    public static Sprite SoftCircle => Get("SoftCircle", SoftCircleMask);
    public static Sprite White => Get("White", (u, v) => 1f);
    public static Sprite Vignette => Get("Vignette", VignetteMask);

    private static Sprite Get(string key, System.Func<float, float, float> mask)
    {
        if (cache.TryGetValue(key, out var s) && s != null) return s;

        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "Feedback" + key,
            hideFlags = HideFlags.DontSave
        };

        var pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = (x + 0.5f) / Size * 2f - 1f;
                float v = (y + 0.5f) / Size * 2f - 1f;
                float a = Mathf.Clamp01(mask(u, v));
                pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        s = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        s.name = tex.name;
        s.hideFlags = HideFlags.DontSave;
        cache[key] = s;
        return s;
    }

    private static float Edge(float signedDistance) => Mathf.Clamp01(0.5f - signedDistance * Size * 0.5f);

    private static float CircleMask(float u, float v) => Edge(Mathf.Sqrt(u * u + v * v) - 0.9f);

    private static float SoftCircleMask(float u, float v)
    {
        float d = Mathf.Sqrt(u * u + v * v);
        return Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f);
    }

    private static float VignetteMask(float u, float v)
    {
        float d = Mathf.Sqrt(u * u * 0.8f + v * v * 0.8f);
        float t = Mathf.InverseLerp(0.55f, 1.15f, d);
        return t * t * (3f - 2f * t);
    }

    private static float DropMask(float u, float v)
    {
        float cy = -0.3f;
        float r = 0.55f;
        float body = Mathf.Sqrt(u * u + (v - cy) * (v - cy)) - r;
        float tipY = 0.95f;
        float t = Mathf.InverseLerp(cy, tipY, v);
        float halfWidth = Mathf.Lerp(r, 0f, t);
        float cone = v < cy ? 1f : Mathf.Abs(u) - halfWidth;
        if (v > tipY) cone = 1f;
        return Edge(Mathf.Min(body, cone));
    }

    private static float FlameMask(float u, float v)
    {
        float baseShape = DropMask(u, v);
        float leftTongue = Edge(Mathf.Sqrt((u + 0.45f) * (u + 0.45f) + (v - 0.15f) * (v - 0.15f)) - 0.25f);
        float rightTongue = Edge(Mathf.Sqrt((u - 0.45f) * (u - 0.45f) + (v - 0.05f) * (v - 0.05f)) - 0.22f);
        float hole = Edge(Mathf.Sqrt(u * u + (v + 0.35f) * (v + 0.35f)) - 0.18f);
        return Mathf.Max(baseShape, Mathf.Max(leftTongue, rightTongue) * (v > -0.4f ? 1f : 0f)) * (1f - hole * 0.6f);
    }
}
