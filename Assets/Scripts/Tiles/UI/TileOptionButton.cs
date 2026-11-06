using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TileOptionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private Button button;
    [SerializeField] private RawImage preview;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private TileDupeBarUI dupeBar;
    [SerializeField] private GameObject levelUpBadge;

    private TileLayout layout;
    private TileLevelData level;

    public TileLayout Layout => layout;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (dupeBar == null) dupeBar = GetComponentInChildren<TileDupeBarUI>(true);
    }

    public void Bind(TileLayout newLayout, TileLevelData levelData, Action onClick)
    {
        if (button == null) button = GetComponent<Button>();
        if (dupeBar == null) dupeBar = GetComponentInChildren<TileDupeBarUI>(true);

        layout = newLayout;
        level = levelData;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick());
            button.interactable = true;
        }
        gameObject.SetActive(true);

        if (preview != null)
        {
            preview.texture = TileIconGenerator.GetIcon(layout);
            preview.uvRect = new Rect(0f, 0f, 1f, 1f);
            var fitter = preview.GetComponent<AspectRatioFitter>();
            if (fitter != null && layout != null)
                fitter.aspectRatio = (float)layout.gridWidth / Mathf.Max(1, layout.gridHeight);
        }

        if (nameLabel != null) nameLabel.text = layout != null ? layout.name : string.Empty;
        if (dupeBar != null && layout != null) dupeBar.SetLayout(layout);
        if (levelLabel != null) levelLabel.text = level != null ? $"Lv {level.currentLevel}" : "Lv 1";
        if (levelUpBadge != null) levelUpBadge.SetActive(WillLevelUp(level));
    }

    public static bool WillLevelUp(TileLevelData data)
    {
        return data != null && data.dupesRequiredForNextLevel - data.currentDupes == 1;
    }

    public static string BuildTooltip(TileLayout layout, TileLevelData data)
    {
        if (layout == null) return string.Empty;

        float dps = 0f, slow = 0f, stun = 0f;
        for (int y = 0; y < layout.gridHeight; y++)
            for (int x = 0; x < layout.gridWidth; x++)
            {
                var cell = new Vector2Int(x, y);
                if (!layout.IsPath(cell)) continue;
                var mods = layout.GetPathModifiers(cell);
                dps = Mathf.Max(dps, mods.dps);
                slow = Mathf.Max(slow, mods.slow);
                stun = Mathf.Max(stun, mods.stun);
            }

        float dmgMult = data != null ? data.damageMultiplier : 1f;
        float slowMult = data != null ? data.slowMultiplier : 1f;
        float stunMult = data != null ? data.stunMultiplier : 1f;

        var sb = new StringBuilder();
        if (dps > 0f) sb.Append($"Damage {dps * dmgMult:0.#}/s\n");
        if (slow > 0f) sb.Append($"Slow {Mathf.Min(slow * slowMult, 0.95f) * 100f:0}%\n");
        if (stun > 0f) sb.Append($"Stun {stun * stunMult:0.##}s\n");
        if (sb.Length == 0) sb.Append("Basic path\n");
        if (data != null) sb.Append($"Lv {data.currentLevel} ({data.currentDupes}/{data.dupesRequiredForNextLevel})");
        return sb.ToString().TrimEnd();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (layout == null) return;
        OptionTooltip.Show(BuildTooltip(layout, level), transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OptionTooltip.HideTooltip();
    }

    private void OnDisable()
    {
        OptionTooltip.HideTooltip();
    }
}
