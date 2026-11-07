using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LoadoutPanelUI : MonoBehaviour
{
    private const string HintSeenKey = "Loadout_HintSeen";

    [Header("Layout")]
    [SerializeField] private Vector2 boxSize = new Vector2(1500f, 860f);
    [SerializeField] private Vector2 cardSize = new Vector2(92f, 104f);
    [SerializeField, Min(1)] private int cardsPerRow = 7;
    [SerializeField] private bool dimBackground;

    private readonly List<TileLayout> selectedTiles = new List<TileLayout>();
    private readonly List<TurretDataSO> selectedTurrets = new List<TurretDataSO>();

    private RectTransform root;
    private Button startButton;
    private TextMeshProUGUI statusText;
    private Action onStart;
    private Action onBack;
    private bool built;

    public bool DimBackground { get => dimBackground; set => dimBackground = value; }
    public bool IsOpen => root != null && gameObject.activeInHierarchy;
    public IReadOnlyList<TileLayout> SelectedTiles => selectedTiles;
    public IReadOnlyList<TurretDataSO> SelectedTurrets => selectedTurrets;
    public bool CanStart => InRange(selectedTiles.Count) && InRange(selectedTurrets.Count);

    private void OnEnable()
    {
        if (!built) Open(null, null);
    }

    public void Open(Action startAction, Action backAction)
    {
        onStart = startAction;
        onBack = backAction;
        LoadoutStore.Load(out var tiles, out var turrets);
        selectedTiles.Clear();
        selectedTiles.AddRange(tiles);
        selectedTurrets.Clear();
        selectedTurrets.AddRange(turrets);
        gameObject.SetActive(true);
        Rebuild();
    }

    private static bool InRange(int count) => count >= LoadoutStore.MinPerColumn && count <= LoadoutStore.MaxPerColumn;

    public void Rebuild()
    {
        built = true;
        if (root != null) Destroy(root.gameObject);

        if (dimBackground)
        {
            root = UiKit.Fill(transform, "LoadoutRoot");
            UiKit.Image(root, new Color(0f, 0f, 0f, 0.7f), true);
        }
        else
        {
            root = UiKit.Fill(transform, "LoadoutRoot");
        }

        var box = UiKit.Sized(root, "Box", boxSize);
        UiKit.Image(box, UiKit.PanelColor, true);

        var title = UiKit.Text(box, "Title", "Loadout", 40f, TextAlignmentOptions.Top, Color.white);
        title.rectTransform.offsetMax = new Vector2(0f, -12f);

        if (onStart != null && PlayerPrefs.GetInt(HintSeenKey, 0) == 0)
        {
            var hint = UiKit.Text(box, "Hint", "Pick 3 to 5 tiles and 3 to 5 turrets. Only these show up during the run. Modifiers are not picked: every unlocked one can appear.", 20f, TextAlignmentOptions.Top, UiKit.Gold);
            hint.rectTransform.offsetMin = new Vector2(40f, 0f);
            hint.rectTransform.offsetMax = new Vector2(-40f, -60f);
        }

        var table = ProgressionStore.Table;
        var tiles = new List<TileLayout>();
        var turrets = new List<TurretDataSO>();
        if (table != null)
        {
            foreach (var t in table.AllTiles()) if (!tiles.Contains(t)) tiles.Add(t);
            foreach (var t in table.AllTurrets()) if (!turrets.Contains(t)) turrets.Add(t);
        }

        BuildColumn(box, "Tiles", new Vector2(0.02f, 0.12f), new Vector2(0.49f, 0.86f), tiles.Count, i => tiles[i], null, UiKit.NormalColor);
        BuildColumn(box, "Turrets", new Vector2(0.51f, 0.12f), new Vector2(0.98f, 0.86f), turrets.Count, null, i => turrets[i], UiKit.OtherColor);

        var footer = UiKit.Rect(box, "Footer", new Vector2(0f, 0f), new Vector2(1f, 0.11f), Vector2.zero, Vector2.zero);
        statusText = UiKit.Text(footer, "Status", string.Empty, 22f, TextAlignmentOptions.Center, Color.white);
        statusText.rectTransform.anchorMax = new Vector2(0.5f, 1f);

        startButton = UiKit.Button(footer, "StartButton", onStart != null ? "Start" : "Save", new Vector2(200f, 60f), Confirm);
        var srt = (RectTransform)startButton.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.85f, 0.5f);

        if (onStart != null)
        {
            var back = UiKit.Button(footer, "BackButton", "Back", new Vector2(160f, 60f), Back);
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.67f, 0.5f);
        }

        RefreshStatus();
    }

    private void BuildColumn(RectTransform box, string label, Vector2 min, Vector2 max, int count, Func<int, TileLayout> tileAt, Func<int, TurretDataSO> turretAt, Color color)
    {
        var col = UiKit.Rect(box, label + "Column", min, max, Vector2.zero, Vector2.zero);
        var header = UiKit.Text(col, "Header", label, 28f, TextAlignmentOptions.Top, color);
        header.rectTransform.offsetMax = Vector2.zero;

        bool isTiles = tileAt != null;
        int selectedCount = isTiles ? selectedTiles.Count : selectedTurrets.Count;
        for (int s = 0; s < LoadoutStore.MaxPerColumn; s++)
        {
            float x = (s - (LoadoutStore.MaxPerColumn - 1) * 0.5f) * (cardSize.x + 8f);
            var slot = UiKit.Rect(col, "Slot" + s, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            slot.sizeDelta = cardSize;
            slot.anchoredPosition = new Vector2(x, -44f - cardSize.y * 0.5f);
            UiKit.Image(slot, s < LoadoutStore.MinPerColumn ? new Color(color.r, color.g, color.b, 0.25f) : new Color(1f, 1f, 1f, 0.08f));
            if (s < selectedCount)
            {
                var tile = isTiles ? selectedTiles[s] : null;
                var turret = isTiles ? null : selectedTurrets[s];
                FillCard(slot, tile, turret, false, 0, true);
            }
        }

        float top = -44f - cardSize.y - 20f;
        for (int i = 0; i < count; i++)
        {
            var tile = isTiles ? tileAt(i) : null;
            var turret = isTiles ? null : turretAt(i);
            int row = i / cardsPerRow;
            int c = i % cardsPerRow;
            float x = (c - (cardsPerRow - 1) * 0.5f) * (cardSize.x + 6f);
            var card = UiKit.Rect(col, "Card", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            card.sizeDelta = cardSize;
            card.anchoredPosition = new Vector2(x, top - row * (cardSize.y + 6f) - cardSize.y * 0.5f);

            bool unlocked = isTiles ? ProgressionStore.IsUnlocked(tile) : ProgressionStore.IsUnlocked(turret);
            int req = isTiles ? ProgressionStore.RequiredLevel(tile) : ProgressionStore.RequiredLevel(turret);
            bool selected = isTiles ? selectedTiles.Contains(tile) : selectedTurrets.Contains(turret);

            var bg = UiKit.Image(card, selected ? new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, 1f) : UiKit.CardColor, true);
            FillCard(card, tile, turret, !unlocked, req, false);

            var tip = card.gameObject.AddComponent<TooltipArea>();
            tip.text = unlocked
                ? (isTiles ? TileOptionButton.BuildTooltip(tile, null) : TurretOptionButton.BuildTooltip(turret, null))
                : $"Locked. Unlocks at {(isTiles ? "Normal" : "Other World")} Lv {req}";

            if (!unlocked) continue;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            if (isTiles) button.onClick.AddListener(() => ToggleTile(tile));
            else button.onClick.AddListener(() => ToggleTurret(turret));
        }
    }

    private void FillCard(RectTransform card, TileLayout tile, TurretDataSO turret, bool locked, int req, bool slot)
    {
        var iconArea = UiKit.Rect(card, "IconArea", new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.95f), Vector2.zero, Vector2.zero);
        UiKit.Icon(iconArea, "Icon", tile, turret, locked);
        string name = tile != null ? tile.name.Replace("Tile-", string.Empty) : TurretOptionButton.GetDisplayName(turret);
        var label = UiKit.Text(card, "Name", locked ? "Lv " + req : name, 12f, TextAlignmentOptions.Bottom, locked ? new Color(1f, 1f, 1f, 0.6f) : Color.white);
        label.rectTransform.anchorMax = new Vector2(1f, 0.28f);
        if (locked)
        {
            var lockText = UiKit.Text(card, "Lock", "LOCK", 14f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.8f));
            lockText.fontStyle = FontStyles.Bold;
        }
    }

    public void ToggleTile(TileLayout tile)
    {
        if (tile == null || !ProgressionStore.IsUnlocked(tile)) return;
        if (selectedTiles.Contains(tile)) selectedTiles.Remove(tile);
        else if (selectedTiles.Count < LoadoutStore.MaxPerColumn) selectedTiles.Add(tile);
        Rebuild();
    }

    public void ToggleTurret(TurretDataSO turret)
    {
        if (turret == null || !ProgressionStore.IsUnlocked(turret)) return;
        if (selectedTurrets.Contains(turret)) selectedTurrets.Remove(turret);
        else if (selectedTurrets.Count < LoadoutStore.MaxPerColumn) selectedTurrets.Add(turret);
        Rebuild();
    }

    private void RefreshStatus()
    {
        if (statusText != null)
            statusText.text = $"Tiles {selectedTiles.Count}/{LoadoutStore.MaxPerColumn}    Turrets {selectedTurrets.Count}/{LoadoutStore.MaxPerColumn}    (min {LoadoutStore.MinPerColumn})";
        if (startButton != null) startButton.interactable = CanStart;
    }

    public void Confirm()
    {
        if (!CanStart) return;
        LoadoutStore.Save(selectedTiles, selectedTurrets);
        RunLoadout.Set(selectedTiles, selectedTurrets);
        Log.Info(nameof(LoadoutPanelUI), $"Loadout saved: {selectedTiles.Count} tiles, {selectedTurrets.Count} turrets");
        OptionTooltip.HideTooltip();
        if (onStart == null) return;
        PlayerPrefs.SetInt(HintSeenKey, 1);
        PlayerPrefs.Save();
        var action = onStart;
        onStart = null;
        gameObject.SetActive(false);
        action();
    }

    public void Back()
    {
        OptionTooltip.HideTooltip();
        gameObject.SetActive(false);
        onBack?.Invoke();
    }
}
