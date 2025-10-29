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
    [SerializeField] private TMP_Text dpsText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text targetModeText;

    [Header("Buttons")]
    [SerializeField] private Button cycleTargetModeButton;
    [SerializeField] private Button removeButton;

    [Header("Refs opcionales")]
    [Tooltip("Si lo dejás vacío, lo busca en la escena.")]
    [SerializeField] private TurretDupeSystem dupeSystem;

    private Turret _current;
    private TurretStats _stats;               // Ajustar a tu clase concreta si difiere
    private TurretDataHolder _holder;         // Para obtener el TurretDataSO
    private TurretTargeting _targeting;       // Tu targeting actual

    // cache para detectar si el evento de level up corresponde a la torreta mostrada
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

        // Botón: cambiar modo de target (usa tu NextMode)
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

        // Botón: eliminar torreta (libera la celda si existe)
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

        // Nombre (usa displayName de SO si existe)
        string display = (_holder && _holder.turretDataSO && !string.IsNullOrEmpty(_holder.turretDataSO.displayName))
            ? _holder.turretDataSO.displayName
            : _current.name.Replace("(Clone)", "").Trim();
        if (nameText) nameText.text = display;

        RefreshNumbers();
        RefreshTargetMode();
        RefreshLevel(); // nivel desde TurretDupeSystem
    }

    private void RefreshNumbers()
    {
        if (!_stats) { PutNumbersNA(); return; }

        float dmg = _stats.Damage;
        float rate = Mathf.Max(0f, _stats.FireRate);
        float dps = dmg * rate;
        float rng = _stats.Range;

        if (damageText) damageText.text = $"Daño: {dmg:0}";
        if (dpsText) dpsText.text = $"DPS: {dps:0.##}";
        if (rangeText) rangeText.text = $"Rango: {rng:0.##}";
    }

    private void PutNumbersNA()
    {
        if (damageText) damageText.text = "Daño: N/A";
        if (dpsText) dpsText.text = "DPS: N/A";
        if (rangeText) rangeText.text = "Rango: N/A";
    }

    private void RefreshTargetMode()
    {
        if (targetModeText)
            targetModeText.text = _targeting ? $"Target: {_targeting.mode}" : "Target: N/A";
    }

    private void RefreshLevel()
    {
        int lvl = 1;

        // Prioridad: si tenés dupeSystem y SO válido, usamos el nivel real
        if (dupeSystem != null && _currentSO != null)
        {
            var ld = dupeSystem.GetTurretLevelData(_currentSO);
            if (ld != null && ld.currentLevel > 0) lvl = ld.currentLevel;
        }

        if (levelText) levelText.text = $"Nivel: {lvl}";
    }

    // Cuando sube el nivel de alguna torreta, refrescamos si coincide con la mostrada
    private void HandleLevelUpEvent(TurretDataSO so, TurretLevelData data)
    {
        if (!_currentSO || so != _currentSO) return;
        RefreshLevel();
    }
}
