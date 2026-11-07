using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurretInfoPanel : MonoBehaviour
{
    public static TurretInfoPanel Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Texts")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text damageText;

    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text targetModeText;

    [Header("Buttons")]
    [SerializeField] private Button cycleTargetModeButton;
    [SerializeField] private Button removeButton;

    [Header("Optional Refs")]
    [Tooltip("If empty, it is searched in the scene.")]
    [SerializeField] private TurretDupeSystem dupeSystem;

    [Header("Extra Info")]
    [Tooltip("Optional. Buffs, charge and special effects. When empty, the lines are added under the damage text.")]
    [SerializeField] private TMP_Text extraInfoText;
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;

    private float nextRefresh;
    private readonly System.Text.StringBuilder infoBuilder = new System.Text.StringBuilder();

    private Turret _current;
    public Turret Current => _current;
    private TurretStats _stats;
    private TurretDataHolder _holder;
    private TurretTargeting _targeting;

    private TurretDataSO _currentSO;

    void Awake()
    {
        Instance = this;
        if (!dupeSystem) dupeSystem = FindFirstObjectByType<TurretDupeSystem>();
        Hide();
    }

    void OnEnable()
    {
        if (dupeSystem != null) dupeSystem.OnTurretLevelUp += HandleLevelUpEvent;
    }

    void OnDisable()
    {
        if (dupeSystem != null) dupeSystem.OnTurretLevelUp -= HandleLevelUpEvent;
    }

    public void Show(Turret t)
    {
        if (!t) { Hide(); return; }

        _current = t;
        _stats = t.GetComponent<TurretStats>();
        _holder = t.GetComponent<TurretDataHolder>();
        _targeting = t.GetComponent<TurretTargeting>();
        _currentSO = (_holder != null) ? _holder.turretDataSO : null;

        if (panelRoot) panelRoot.SetActive(true);
        RefreshAll();

        if (cycleTargetModeButton)
        {
            cycleTargetModeButton.onClick.RemoveAllListeners();
            cycleTargetModeButton.onClick.AddListener(() =>
            {
                if (_targeting)
                {
                    _targeting.NextMode();
                    if (targetModeText) targetModeText.text = $"Target: {_targeting.mode}";
                }
            });
            cycleTargetModeButton.interactable = (_targeting != null);
        }

        if (removeButton)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(() =>
            {
                if (!_current) return;

                var slot = _current.GetComponentInParent<CellSlot>();
                bool removed = false;

                if (slot && slot.IsOccupied && slot.GetCurrentTurret() == _current.gameObject)
                    removed = slot.TryRemove();
                else
                {
                    Destroy(_current.gameObject);
                    removed = true;
                }

                if (removed) Hide();
            });
        }
    }

    public void Hide()
    {
        if (panelRoot) panelRoot.SetActive(false);
        if (extraInfoText)
        {
            var holderGo = extraInfoText.transform.parent != null ? extraInfoText.transform.parent.gameObject : null;
            if (holderGo != null && holderGo != panelRoot) holderGo.SetActive(false);
        }

        _current = null;
        _stats = null;
        _holder = null;
        _targeting = null;
        _currentSO = null;
    }

    private void RefreshAll()
    {
        if (!_current) { Hide(); return; }

        string display = (_holder && _holder.turretDataSO && !string.IsNullOrEmpty(_holder.turretDataSO.displayName))
            ? _holder.turretDataSO.displayName
            : _current.name.Replace("(Clone)", "").Trim();
        if (nameText) nameText.text = display;

        RefreshNumbers();
        RefreshTargetMode();
        RefreshLevel();
    }

    private void RefreshNumbers()
    {
        if (!_stats) { PutNumbersNA(); return; }

        float dmg = _stats.Damage;
        float rate = Mathf.Max(0f, _stats.FireRate);
        float dps = dmg * rate;
        float rng = _stats.Range;

        string extra = BuildExtraInfo();
        if (extraInfoText)
        {
            extraInfoText.text = extra;
            var holderGo = extraInfoText.transform.parent != null ? extraInfoText.transform.parent.gameObject : null;
            if (holderGo != null && holderGo != panelRoot && holderGo.activeSelf != extra.Length > 0) holderGo.SetActive(extra.Length > 0);
        }
        if (damageText) damageText.text = !extraInfoText && extra.Length > 0 ? $"Damage: {dmg:0.#}\n<size=75%>{extra}</size>" : $"Damage: {dmg:0.#}";

        if (rangeText) rangeText.text = $"Range: {rng:0.##}";
    }

    public string BuildExtraInfo()
    {
        infoBuilder.Clear();
        if (_current == null) return string.Empty;

        if (_stats != null && _stats.HasExternalBuff)
            infoBuilder.Append($"Banner: +{(_stats.ExternalDamageMultiplier - 1f) * 100f:0}% damage, +{(_stats.ExternalFireRateMultiplier - 1f) * 100f:0}% fire rate\n");

        if (WardenRegistry.IsProtected(_current))
            infoBuilder.Append("Protected by a Warden\n");

        foreach (var provider in _current.GetComponents<ITurretInfoProvider>())
        {
            string line = provider.GetInfoLine();
            if (!string.IsNullOrEmpty(line)) infoBuilder.Append(line).Append('\n');
        }

        return infoBuilder.ToString().TrimEnd();
    }

    private void Update()
    {
        if (_current == null || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + refreshInterval;
        RefreshNumbers();
    }

    private void PutNumbersNA()
    {
        if (damageText) damageText.text = "Damage: N/A";

        if (rangeText) rangeText.text = "Range: N/A";
    }

    private void RefreshTargetMode()
    {
        if (targetModeText)
            targetModeText.text = _targeting ? $"Target: {_targeting.mode}" : "Target: N/A";
    }

    private void RefreshLevel()
    {
        int lvl = 1;

        if (dupeSystem != null && _currentSO != null)
        {
            var ld = dupeSystem.GetTurretLevelData(_currentSO);
            if (ld != null && ld.currentLevel > 0) lvl = ld.currentLevel;
        }

        if (levelText) levelText.text = $"Level: {lvl}";
    }

    private void HandleLevelUpEvent(TurretDataSO so, TurretLevelData data)
    {
        if (!_currentSO || so != _currentSO) return;
        RefreshLevel();
    }
}
