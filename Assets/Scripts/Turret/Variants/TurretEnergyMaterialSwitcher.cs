using UnityEngine;

[DisallowMultipleComponent]
public sealed class TurretEnergyMaterialSwitcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShiftingWorldMechanic worldMechanic;

    [Tooltip("Renderers where a single material slot is swapped.")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Material to switch")]
    [Tooltip("Index of the material slot to swap (0 = first, 1 = second, and so on)")]
    [SerializeField] private int materialIndexToChange = 1;

    [Header("Materials")]
    [SerializeField] private Material normalWorldMaterial;
    [SerializeField] private Material otherWorldMaterial;

    private ShiftingWorldMechanic.World currentAppliedWorld;

    private void Awake()
    {
        if (worldMechanic == null)
        {
            worldMechanic = FindFirstObjectByType<ShiftingWorldMechanic>();
            if (worldMechanic == null)
            {
                Debug.LogError("[TurretEnergyMaterialSwitcher] ShiftingWorldMechanic not found in the scene");
            }
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>();
        }
    }

    private void Start()
    {
        ApplyMaterial(worldMechanic.GetCurrentWorld());
    }

    private void Update()
    {
        if (worldMechanic == null) return;

        var world = worldMechanic.GetCurrentWorld();

        if (world != currentAppliedWorld)
        {
            ApplyMaterial(world);
        }
    }

    private void ApplyMaterial(ShiftingWorldMechanic.World world)
    {
        currentAppliedWorld = world;

        Material targetMaterial =
            (world == ShiftingWorldMechanic.World.Normal)
            ? normalWorldMaterial
            : otherWorldMaterial;

        if (targetMaterial == null)
        {
            Debug.LogWarning("[TurretEnergyMaterialSwitcher] Materials are not assigned");
            return;
        }

        foreach (Renderer rend in targetRenderers)
        {
            if (rend == null) continue;

            Material[] mats = rend.materials;

            if (materialIndexToChange < 0 || materialIndexToChange >= mats.Length)
            {
                Debug.LogWarning($"[TurretEnergyMaterialSwitcher] Material index {materialIndexToChange} does not exist on renderer {rend.name}");
                continue;
            }

            mats[materialIndexToChange] = targetMaterial;

            rend.materials = mats;
        }
    }
}
