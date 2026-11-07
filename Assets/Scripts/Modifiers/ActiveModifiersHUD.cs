using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActiveModifiersHUD : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform iconContainer;
    [SerializeField] private Image iconPrefab;

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private TMP_Text tooltipTitle;
    [SerializeField] private TMP_Text tooltipDescription;

    private readonly List<GameObject> spawnedIcons = new List<GameObject>();
    private int legendaryIconCount;
    private GameModifiersManager subscribedManager;

    private void Start()
    {
        HideTooltip();
        TrySubscribe();
        Rebuild();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnModifierApplied -= HandleModifierApplied;
            subscribedManager = null;
        }
    }

    private void TrySubscribe()
    {
        if (subscribedManager != null || GameModifiersManager.Instance == null) return;
        subscribedManager = GameModifiersManager.Instance;
        subscribedManager.OnModifierApplied += HandleModifierApplied;
    }

    private void HandleModifierApplied(ModifierSO mod)
    {
        AddIcon(mod);
    }

    private void Rebuild()
    {
        foreach (var icon in spawnedIcons)
            if (icon != null) Destroy(icon);
        spawnedIcons.Clear();
        legendaryIconCount = 0;

        if (GameModifiersManager.Instance == null) return;
        foreach (var mod in GameModifiersManager.Instance.GetModifiers())
            if (mod is ModifierSO so) AddIcon(so);
    }

    private void AddIcon(ModifierSO mod)
    {
        if (mod == null || iconPrefab == null || iconContainer == null) return;

        var image = Instantiate(iconPrefab, iconContainer);
        image.sprite = mod.icon;
        image.gameObject.SetActive(true);

        var hover = image.GetComponent<ModifierIconHover>();
        if (hover == null) hover = image.gameObject.AddComponent<ModifierIconHover>();
        hover.Bind(this, mod);

        if (GameModifiersManager.IsLegendary(mod.category))
        {
            var frame = image.GetComponent<Outline>();
            if (frame == null) frame = image.gameObject.AddComponent<Outline>();
            frame.effectColor = LegendaryCardFX.Gold;
            frame.effectDistance = new Vector2(3f, 3f);
            image.transform.SetSiblingIndex(legendaryIconCount);
            legendaryIconCount++;
        }

        spawnedIcons.Add(image.gameObject);
    }

    public void ShowTooltip(ModifierSO mod)
    {
        if (mod == null || tooltipRoot == null) return;
        if (tooltipTitle) tooltipTitle.text = GameModifiersManager.IsLegendary(mod.category) ? "<color=#FFC733>" + mod.displayName + "</color>" : mod.displayName;
        if (tooltipDescription) tooltipDescription.text = mod.description;
        tooltipRoot.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipRoot) tooltipRoot.SetActive(false);
    }
}
