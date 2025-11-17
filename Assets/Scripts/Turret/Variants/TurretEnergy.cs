using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Turret))]
public sealed class TurretEnergy : MonoBehaviour, IWorldEnergySource
{
    [Header("Energy Settings")]
    [Tooltip("Base extra energy per second that this turret contributes at level 1.")]
    [SerializeField] private float baseEnergyPerSecond = 2f;

    [Tooltip("Additional multiplier per level above 1. For example 0.5 means +50% per extra level.")]
    [SerializeField] private float perLevelMultiplier = 0.5f;

    private Turret turret;
    private TurretStats stats;
    private bool isRegistered;

    private void Awake()
    {
        turret = GetComponent<Turret>();
        stats = GetComponent<TurretStats>();

        if (turret == null)
        {
            Debug.LogError("[TurretEnergy] Missing Turret component.", this);
        }

        if (stats == null)
        {
            Debug.LogError("[TurretEnergy] Missing TurretStats component.", this);
        }
    }

    private void OnEnable()
    {
        UpdateRegistration();
    }

    private void OnDisable()
    {
        if (isRegistered)
        {
            WorldEnergyRegistry.Unregister(this);
            isRegistered = false;
        }
    }

    private void Update()
    {
        UpdateRegistration();
    }

    private void UpdateRegistration()
    {
        if (turret == null)
            return;

        bool shouldBeRegistered = turret.IsPlaced && isActiveAndEnabled;

        if (shouldBeRegistered == isRegistered)
            return;

        if (shouldBeRegistered)
        {
            WorldEnergyRegistry.Register(this);
            isRegistered = true;
        }
        else
        {
            WorldEnergyRegistry.Unregister(this);
            isRegistered = false;
        }
    }


    public float GetChargePerSecond()
    {
        if (!isActiveAndEnabled || stats == null)
            return 0f;

        int level = Mathf.Max(1, stats.UpgradeLevel);

        float factor = 1f + perLevelMultiplier * (level - 1);
        return baseEnergyPerSecond * factor;
    }
}
