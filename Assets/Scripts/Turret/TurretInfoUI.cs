using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurretInfoUI : MonoBehaviour
{
    public static TurretInfoUI Instance;

    [Header("UI References")]
    public TMP_Text turretNameText;
    public TMP_Text damageText;
    public TMP_Text rangeText;
    public TMP_Text fireRateText;
    //public TMP_Text specialInfoText;

    public Button sellButton;
    public TMP_Text sellButtonText;

    public Button changeTargetModeButton;
    public TMP_Text changeTargetModeButtonText;

    private Turret currentTurret;

    private TurretDataHolder dataHolder;
    private TurretStats stats;
    private TurretTargeting targeting;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        sellButton.onClick.AddListener(SellTurret);
        changeTargetModeButton.onClick.AddListener(ChangeTargetingMode);

    }

    public void Initialize(Turret turret)
    {
        currentTurret = turret;

        dataHolder = turret.GetComponent<TurretDataHolder>();
        stats = turret.GetComponent<TurretStats>();
        stats.OnStatsChanged += UpdateInfo;

        if (dataHolder == null || stats == null)
        {
            Debug.LogError("Faltan componentes requeridos en la torreta.");
            return;
        }

        var type = dataHolder.turretDataSO?.type;
        bool isSupport = type == "support";

        if (!isSupport)
        {
            targeting = turret.GetComponent<TurretTargeting>();
            if (targeting == null)
            {
                Debug.LogError("Falta TurretTargeting en torreta no-support.");
                return;
            }
        }
        UpdateInfo();

        if (!isSupport)
            UpdateTargetModeText(targeting.mode);
    }


    public void UpdateInfo()
    {
        if (currentTurret == null) return;

        var turretData = dataHolder.turretDataSO;
        turretNameText.text = $"{turretData.name}";

        if (turretData.type == "support")
        {
            rangeText.text = "-";
            fireRateText.text = "-";
            changeTargetModeButton.gameObject.SetActive(false);
        }
        else
        {
            damageText.text = $"{stats.Damage}";
            fireRateText.text = $"{stats.FireRate}";
            rangeText.text = $"{stats.Range}";

            //specialInfoText.gameObject.SetActive(false);
            changeTargetModeButton.gameObject.SetActive(true);
        }


        sellButtonText.text = $"Sell";
    }

    private void SellTurret()
    {

    }

    public void UpdateTargetModeText(TurretTargeting.TargetingMode mode)
    {
        switch (mode)
        {
            case TurretTargeting.TargetingMode.Closest:
                changeTargetModeButtonText.text = "Close Enemy"; break;
            case TurretTargeting.TargetingMode.Farthest:
                changeTargetModeButtonText.text = "Far Enemy"; break;
            case TurretTargeting.TargetingMode.HighestHealth:
                changeTargetModeButtonText.text = "More health"; break;
            case TurretTargeting.TargetingMode.LowestHealth:
                changeTargetModeButtonText.text = "Less Health"; break;
        }
    }

    void ChangeTargetingMode()
    {
        targeting.NextMode(); // Asegurate de tener este método en TurretTargeting
        UpdateTargetModeText(targeting.mode);
    }
}
