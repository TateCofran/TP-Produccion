using UnityEngine;
using UnityEngine.Events;

public class WorldSwitchPoints : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("ShiftingWorldMechanic from the scene.")]
    [SerializeField] private ShiftingWorldMechanic mechanic;
    [SerializeField] private EssencePopupUI essenceUI;

    [Header("Base Essences Per World Switch")]
    [Tooltip("Blue essences gained when switching to the Normal world.")]
    [SerializeField] private int essencesOnNormal = 1;
    [Tooltip("Red essences gained when switching to the Other world.")]
    [SerializeField] private int essencesOnOther = 1;

    [Header("Permanent Upgrade Bonuses")]
    [Tooltip("ID of the Lab upgrade that increases blue essences per world switch.")]
    [SerializeField] private string labBlueUpgradeId = "upgrade_moreEssencePerSwitchTile";
    [Tooltip("Bonus per level (0.1 = +10% per level).")]
    [Range(0f, 5f)][SerializeField] private float blueBonusPerLevel = 0.1f;

    [Tooltip("ID of the Workshop upgrade that increases red essences per world switch.")]
    [SerializeField] private string workshopRedUpgradeId = "upgrade_moreEssencePerSwitchTurret";
    [Range(0f, 5f)][SerializeField] private float redBonusPerLevel = 0.1f;

    [Header("Totals")]
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
            Debug.LogWarning("[WorldSwitchPoints] ShiftingWorldMechanic not found in the scene");
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
        essenceUI.SetWorld(current == ShiftingWorldMechanic.World.Normal);

        if (current == ShiftingWorldMechanic.World.Normal)
        {
            int amount = CalculateBlueEssence();
            totalBlueEssences += amount;
            essenceUI.HandleEssence(amount, totalBlueEssences);
        }
        else
        {
            int amount = CalculateRedEssence();
            totalRedEssences += amount;
            essenceUI.HandleEssence(amount, totalRedEssences);
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
