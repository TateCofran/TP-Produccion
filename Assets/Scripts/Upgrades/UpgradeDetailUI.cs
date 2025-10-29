using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UpgradeDetailUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button buyButton;

    [Header("Cross UI")]
    [SerializeField] private UpgradeListUI listUI;                 // para refrescar niveles a la izquierda
    [SerializeField] private EssenceDisplayUI essenceDisplayUI;    // para refrescar totales mostrados

    private UpgradeSO _current;

    private void Awake()
    {
        if (listUI != null) listUI.OnItemSelected += Show;
        buyButton.onClick.AddListener(Buy);
    }

    public void Show(UpgradeSO up)
    {
        _current = up;
        if (up == null)
        {
            titleText.text = "Select an upgrade";
            descText.text = "";
            levelText.text = "";
            costText.text = "";
            buyButton.interactable = false;
            return;
        }

        titleText.text = up.DisplayName;
        descText.text = up.Description;

        int level = UpgradeSystemBootstrap.Service.GetCurrentLevel(up);
        levelText.text = $"Level: {level}/{up.MaxLevel}";

        if (level >= up.MaxLevel)
        {
            costText.text = "Max level reached";
            buyButton.interactable = false;
        }
        else
        {
            var (blue, red) = up.GetCostForLevel(level + 1);
            costText.text = $"Cost: {blue} Blue | {red} Red";
            bool can = UpgradeSystemBootstrap.Service.CanPurchase(up, out _, out _, out _);
            buyButton.interactable = can;
        }
    }

    private void Buy()
    {
        if (_current == null) return;

        if (UpgradeSystemBootstrap.Service.TryPurchase(_current))
        {
            // Refrescar panel derecho
            Show(_current);

            // Refrescar panel izquierdo (niveles)
            if (listUI != null) listUI.RefreshLevels();

            // Refrescar contador de esencias del panel
            if (essenceDisplayUI != null) essenceDisplayUI.UpdateEssenceDisplay();
        }
        else
        {
            // Podés mostrar feedback de error aquí
        }
    }
}
