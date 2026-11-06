using UnityEngine;

public static class ExitArrowSprite
{
    private const int Size = 32;
    private static Sprite sprite;

    public static Sprite Get()
    {
        if (sprite != null) return sprite;

        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "ExitArrow",
            hideFlags = HideFlags.DontSave
        };

        var pixels = new Color32[Size * Size];
        var clear = new Color32(255, 255, 255, 0);
        var solid = new Color32(255, 255, 255, 255);

        int headStart = Size / 2;
        int shaftHalf = Size / 8;

        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                bool filled;
                if (y >= headStart)
                {
                    float progress = (y - headStart) / (float)(Size - 1 - headStart);
                    float half = (1f - progress) * (Size * 0.5f - 1f);
                    filled = Mathf.Abs(x - (Size - 1) * 0.5f) <= half;
                }
                else
                {
                    filled = y >= 2 && Mathf.Abs(x - (Size - 1) * 0.5f) <= shaftHalf;
                }
                pixels[y * Size + x] = filled ? solid : clear;
            }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "ExitArrow";
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }
}
