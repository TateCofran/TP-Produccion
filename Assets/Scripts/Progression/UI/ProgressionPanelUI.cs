using TMPro;
using UnityEngine;
using World = ShiftingWorldMechanic.World;

[DisallowMultipleComponent]
public class ProgressionPanelUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 boxSize = new Vector2(1100f, 640f);
    [SerializeField, Min(1)] private int routeLevels = 3;

    private RectTransform root;

    public string NormalLabel { get; private set; }
    public string OtherLabel { get; private set; }

    private void OnEnable()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        if (root != null) Destroy(root.gameObject);
        root = UiKit.Sized(transform, "ProgressionRoot", boxSize);
        UiKit.Image(root, UiKit.PanelColor, true);

        var title = UiKit.Text(root, "Title", "Progression", 40f, TextAlignmentOptions.Top, Color.white);
        title.rectTransform.offsetMax = new Vector2(0f, -16f);

        BuildBar(World.Normal, new Vector2(0.03f, 0.08f), new Vector2(0.49f, 0.86f));
        BuildBar(World.Other, new Vector2(0.51f, 0.08f), new Vector2(0.97f, 0.86f));
    }

    private void BuildBar(World mode, Vector2 min, Vector2 max)
    {
        bool normal = mode == World.Normal;
        Color color = normal ? UiKit.NormalColor : UiKit.OtherColor;
        var col = UiKit.Rect(root, normal ? "NormalColumn" : "OtherColumn", min, max, Vector2.zero, Vector2.zero);

        var header = UiKit.Text(col, "Header", normal ? "Normal (tiles)" : "Other World (turrets)", 28f, TextAlignmentOptions.Top, color);
        header.rectTransform.offsetMax = Vector2.zero;

        int xp = ProgressionStore.GetXp(mode);
        int level = ProgressionStore.LevelForXp(xp, out int into, out int needed);
        string label = $"Lv {level} — {into} / {needed}";
        if (normal) NormalLabel = label; else OtherLabel = label;

        var levelText = UiKit.Text(col, "Level", label, 24f, TextAlignmentOptions.Top, Color.white);
        levelText.rectTransform.offsetMax = new Vector2(0f, -40f);

        var barHolder = UiKit.Rect(col, "BarHolder", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -110f), new Vector2(0f, -80f));
        UiKit.Bar(barHolder, "Bar", color, out var fill);
        UiKit.SetBar(fill, needed > 0 ? (float)into / needed : 0f);

        var routeTitle = UiKit.Text(col, "RouteTitle", "Next unlocks", 20f, TextAlignmentOptions.Top, new Color(1f, 1f, 1f, 0.7f));
        routeTitle.rectTransform.offsetMax = new Vector2(0f, -122f);

        var table = ProgressionStore.Table;
        int shown = 0;
        float y = -160f;
        int maxLevel = table != null ? (normal ? table.MaxNormalLevel : table.MaxOtherLevel) : 1;
        for (int l = level + 1; l <= maxLevel && shown < routeLevels; l++)
        {
            var unlock = table.GetLevel(normal, l);
            if (unlock == null) continue;
            BuildRouteRow(col, unlock, normal, y);
            y -= 120f;
            shown++;
        }
        if (shown == 0)
        {
            var done = UiKit.Text(col, "Done", "Everything unlocked", 20f, TextAlignmentOptions.Top, UiKit.Gold);
            done.rectTransform.offsetMax = new Vector2(0f, y);
        }
    }

    private void BuildRouteRow(RectTransform col, UnlockLevel unlock, bool normal, float y)
    {
        var row = UiKit.Rect(col, "Lv" + unlock.level, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y - 110f), new Vector2(0f, y));
        UiKit.Image(row, new Color(1f, 1f, 1f, 0.05f));
        var lv = UiKit.Text(row, "LevelTag", "Lv " + unlock.level, 22f, TextAlignmentOptions.Left, Color.white);
        lv.rectTransform.offsetMin = new Vector2(10f, 0f);

        var items = new System.Collections.Generic.List<UnlockedItem>();
        ProgressionStore.AppendUnlocks(items, unlock, normal);
        float x = 90f;
        foreach (var item in items)
        {
            var cell = UiKit.Rect(row, "Item", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(x, 6f), new Vector2(x + 96f, -6f));
            if (item.kind != UnlockedItem.Kind.Modifier)
            {
                var icon = UiKit.Rect(cell, "Icon", new Vector2(0.15f, 0.3f), new Vector2(0.85f, 1f), Vector2.zero, Vector2.zero);
                UiKit.Icon(icon, "Silhouette", item.tile, item.turret, true);
            }
            else
            {
                var badge = UiKit.Rect(cell, "Badge", new Vector2(0.15f, 0.3f), new Vector2(0.85f, 1f), Vector2.zero, Vector2.zero);
                UiKit.Image(badge, item.IsLegendary ? UiKit.Gold : new Color(0.3f, 0.3f, 0.45f, 1f));
                UiKit.Text(badge, "M", item.IsLegendary ? "L" : "M", 26f, TextAlignmentOptions.Center, Color.black);
            }
            var name = UiKit.Text(cell, "Name", item.DisplayName, 13f, TextAlignmentOptions.Bottom, item.IsLegendary ? UiKit.Gold : Color.white);
            name.rectTransform.anchorMax = new Vector2(1f, 0.3f);
            var tip = cell.gameObject.AddComponent<TooltipArea>();
            tip.text = $"{item.Caption}: {item.DisplayName}\nUnlocks at Lv {unlock.level}";
            UiKit.Image(cell, new Color(0f, 0f, 0f, 0.01f), true);
            x += 100f;
        }
    }
}
