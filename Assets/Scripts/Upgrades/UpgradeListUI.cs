using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeListUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private UpgradeRepository repository;
    [SerializeField] private UpgradeCategory category;

    [Header("UI")]
    [SerializeField] private Transform contentParent;       // contenedor de ítems
    [SerializeField] private Button itemButtonPrefab;       // botón con un TMP_Text hijo

    public event Action<UpgradeSO> OnItemSelected;

    private readonly List<GameObject> _spawned = new();

    private void OnEnable() => Build();

    public void Build()
    {
        Clear();

        foreach (var up in repository.GetByCategory(category))
        {
            var btn = Instantiate(itemButtonPrefab, contentParent);
            _spawned.Add(btn.gameObject);

            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            int level = UpgradeSystemBootstrap.Service.GetCurrentLevel(up);
            label.text = $"{up.DisplayName}  Lv.{level}/{up.MaxLevel}";

            btn.onClick.AddListener(() => OnItemSelected?.Invoke(up));
        }
    }

    public void RefreshLevels()
    {
        // Vuelve a construir los textos de nivel sin recrear la lista si preferís.
        Build();
    }

    private void Clear()
    {
        foreach (var go in _spawned) Destroy(go);
        _spawned.Clear();
    }
}
