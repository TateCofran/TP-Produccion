using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TurretOptionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private Button button;
    [SerializeField] private Image preview;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text levelLabel;
    [Tooltip("Optional. Its type text must be the nameLabel so it can find the turret data.")]
    [SerializeField] private DupeBarUI dupeBar;
    [SerializeField] private GameObject levelUpBadge;

    private TurretDataSO turret;
    private TurretLevelData level;

    public TurretDataSO Turret => turret;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (dupeBar == null) dupeBar = GetComponentInChildren<DupeBarUI>(true);
    }

    public void Bind(TurretDataSO so, TurretLevelData levelData, Action onClick)
    {
        if (button == null) button = GetComponent<Button>();
        if (dupeBar == null) dupeBar = GetComponentInChildren<DupeBarUI>(true);

        turret = so;
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
            preview.sprite = so != null ? so.icon : null;
            preview.enabled = preview.sprite != null;
            preview.preserveAspect = true;
        }

        if (nameLabel != null) nameLabel.text = GetDisplayName(so);
        if (dupeBar != null && dupeBar.isActiveAndEnabled && TurretDupeUI.Instance != null) dupeBar.UpdateDupeBar();
        if (levelLabel != null) levelLabel.text = level != null ? $"Lv {level.currentLevel}" : "Lv 1";
        if (levelUpBadge != null) levelUpBadge.SetActive(WillLevelUp(level));
    }

    public static string GetDisplayName(TurretDataSO so)
    {
        if (so == null) return string.Empty;
        return !string.IsNullOrEmpty(so.displayName) ? so.displayName : so.name;
    }

    public static bool WillLevelUp(TurretLevelData data)
    {
        return data != null && data.dupesRequiredForNextLevel - data.currentDupes == 1;
    }

    public static void ComputeStats(TurretDataSO so, TurretLevelData data, out float damage, out float range, out float fireRate)
    {
        damage = range = fireRate = 0f;
        if (so == null) return;

        damage = so.damage;
        range = so.range;
        fireRate = so.fireRate;
        TurretPermanentUpgrades.ApplyPermanentUpgrades(ref damage, ref range, ref fireRate);

        if (data != null)
        {
            damage *= data.damageMultiplier;
            range *= data.rangeMultiplier;
            fireRate *= data.fireRateMultiplier;
        }

        var mods = GameModifiersManager.Instance;
        if (mods != null)
        {
            damage *= mods.turretDamageMultiplier;
            range *= mods.turretRangeMultiplier;
            fireRate *= mods.turretFireRateMultiplier;
        }
    }

    public static string BuildTooltip(TurretDataSO so, TurretLevelData data)
    {
        if (so == null) return string.Empty;
        ComputeStats(so, data, out float damage, out float range, out float fireRate);

        string affinity = so.affinity switch
        {
            WorldAffinity.Normal => $"Stronger in Normal (+{so.affinityBonus * 100f:0}%)",
            WorldAffinity.Other => $"Stronger in Other World (+{so.affinityBonus * 100f:0}%)",
            _ => "Neutral"
        };

        string levelText = data != null ? $"Lv {data.currentLevel} ({data.currentDupes}/{data.dupesRequiredForNextLevel})" : "Lv 1";
        return $"{GetDisplayName(so)}  {levelText}\nDamage {damage:0.#}\nRange {range:0.#}\nFire rate {fireRate:0.##}\n{affinity}";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (turret == null) return;
        OptionTooltip.Show(BuildTooltip(turret, level), transform as RectTransform);
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
