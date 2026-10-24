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

    private Turret _current;
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

        if (damageText) damageText.text = $"Damage: {dmg:0}";

        if (rangeText) rangeText.text = $"Range: {rng:0.##}";
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
