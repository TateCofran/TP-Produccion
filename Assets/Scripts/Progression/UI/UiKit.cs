using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UiKit
{
    public static readonly Color Gold = new Color(1f, 0.78f, 0.2f, 1f);
    public static readonly Color NormalColor = new Color(0.35f, 0.65f, 1f, 1f);
    public static readonly Color OtherColor = new Color(1f, 0.35f, 0.3f, 1f);
    public static readonly Color PanelColor = new Color(0.06f, 0.06f, 0.1f, 0.92f);
    public static readonly Color CardColor = new Color(0.16f, 0.16f, 0.24f, 1f);

    public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }

    public static RectTransform Fill(Transform parent, string name) => Rect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

    public static RectTransform Sized(Transform parent, string name, Vector2 size)
    {
        var rt = Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rt.sizeDelta = size;
        return rt;
    }

    public static Image Image(RectTransform rt, Color color, bool raycast = false)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, TextAlignmentOptions align, Color color)
    {
        var rt = Fill(parent, name);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    public static Button Button(Transform parent, string name, string label, Vector2 size, Action onClick)
    {
        var rt = Sized(parent, name, size);
        var img = Image(rt, new Color(0.22f, 0.2f, 0.3f, 1f), true);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var colors = b.colors;
        colors.highlightedColor = new Color(1.25f, 1.2f, 1f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        b.colors = colors;
        Text(rt, "Label", label, 24f, TextAlignmentOptions.Center, Color.white);
        if (onClick != null) b.onClick.AddListener(() => onClick());
        return b;
    }

    public static RectTransform Bar(Transform parent, string name, Color fillColor, out RectTransform fill)
    {
        var rt = Fill(parent, name);
        Image(rt, new Color(0f, 0f, 0f, 0.6f));
        fill = Rect(rt, "Fill", Vector2.zero, new Vector2(0f, 1f), new Vector2(2f, 2f), new Vector2(-2f, -2f));
        Image(fill, fillColor);
        return rt;
    }

    public static void SetBar(RectTransform fill, float value)
    {
        if (fill == null) return;
        fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
    }

    public static Graphic Icon(Transform parent, string name, TileLayout tile, TurretDataSO turret, bool silhouette)
    {
        var rt = Fill(parent, name);
        Color tint = silhouette ? new Color(0.12f, 0.12f, 0.16f, 1f) : Color.white;
        if (tile != null)
        {
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = TileIconGenerator.GetIcon(tile);
            raw.color = tint;
            raw.raycastTarget = false;
            return raw;
        }
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = turret != null ? turret.icon : null;
        img.preserveAspect = true;
        img.color = img.sprite != null ? tint : new Color(0.3f, 0.3f, 0.4f, 1f);
        img.raycastTarget = false;
        return img;
    }

    public static Canvas RootCanvasOf(Component c)
    {
        var canvas = c != null ? c.GetComponentInParent<Canvas>() : null;
        return canvas != null ? canvas.rootCanvas : null;
    }
}
