using UnityEngine;
using UnityEngine.Events;

public class WorldSwitchPoints : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Arrastra el ShiftingWorldMechanic de la escena.")]
    [SerializeField] private ShiftingWorldMechanic mechanic;

    [Header("Esencias base al cambiar de mundo")]
    [Tooltip("Al quedar en el mundo Normal - esencias AZULES ganadas.")]
    [SerializeField] private int essencesOnNormal = 1;
    [Tooltip("Al quedar en el Otro Mundo - esencias ROJAS ganadas.")]
    [SerializeField] private int essencesOnOther = 1;

    [Header("Bonos de mejoras permanentes")]
    [Tooltip("ID de la mejora del Lab que aumenta las esencias AZULES al cambiar de mundo.")]
    [SerializeField] private string labBlueUpgradeId = "upgrade_moreEssencePerSwitchTile";
    [Tooltip("Bonus plano o multiplicador por nivel (ej: 0.1 = +10% por nivel).")]
    [Range(0f, 5f)][SerializeField] private float blueBonusPerLevel = 0.1f;

    [Tooltip("ID de la mejora del Workshop que aumenta las esencias ROJAS al cambiar de mundo.")]
    [SerializeField] private string workshopRedUpgradeId = "upgrade_moreEssencePerSwitchTurret";
    [Range(0f, 5f)][SerializeField] private float redBonusPerLevel = 0.1f;

    [Header("Totales")]
    [SerializeField] private int totalBlueEssences = 0;
    [SerializeField] private int totalRedEssences = 0;

    [System.Serializable] public class EssenceEvent : UnityEvent<int, int> { }
    public EssenceEvent OnBlueEssenceGained;
    public EssenceEvent OnRedEssenceGained;

    private ShiftingWorldMechanic.World _lastWorld;
    private bool _init;

    public int TotalBlue => totalBlueEssences;
    public int TotalRed => totalRedEssences;

    private void Reset()
    {
        if (mechanic == null) mechanic = FindFirstObjectByType<ShiftingWorldMechanic>();
    }

    private void Awake()
    {
        if (mechanic == null) mechanic = FindFirstObjectByType<ShiftingWorldMechanic>();
    }

    private void Start()
    {
        if (mechanic == null)
        {
            Debug.LogWarning("[WorldSwitchPoints] No se encontró ShiftingWorldMechanic en la escena.");
            enabled = false;
            return;
        }

        _lastWorld = mechanic.GetCurrentWorld();
        _init = true;
    }

    private void Update()
    {
        if (!_init || mechanic == null) return;

        var current = mechanic.GetCurrentWorld();
        if (current != _lastWorld)
        {
            if (current == ShiftingWorldMechanic.World.Normal)
            {
                int amount = CalculateBlueEssence();
                AddBlueEssence(amount);
            }
            else
            {
                int amount = CalculateRedEssence();
                AddRedEssence(amount);
            }

            _lastWorld = current;
        }
    }

    private int CalculateBlueEssence()
    {
        int level = UpgradeLevels.Get(labBlueUpgradeId);
        float multiplier = 1f + (blueBonusPerLevel * level);
        int total = Mathf.RoundToInt(essencesOnNormal * multiplier);
        return Mathf.Max(1, total);
    }

    private int CalculateRedEssence()
    {
        int level = UpgradeLevels.Get(workshopRedUpgradeId);
        float multiplier = 1f + (redBonusPerLevel * level);
        int total = Mathf.RoundToInt(essencesOnOther * multiplier);
        return Mathf.Max(1, total);
    }

    public void AddBlueEssence(int amount)
    {
        if (amount <= 0) return;
        totalBlueEssences += amount;
        OnBlueEssenceGained?.Invoke(amount, totalBlueEssences);
    }

    public void AddRedEssence(int amount)
    {
        if (amount <= 0) return;
        totalRedEssences += amount;
        OnRedEssenceGained?.Invoke(amount, totalRedEssences);
    }
}
