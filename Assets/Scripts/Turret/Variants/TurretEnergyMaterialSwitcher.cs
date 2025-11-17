using UnityEngine;

[DisallowMultipleComponent]
public sealed class TurretEnergyMaterialSwitcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShiftingWorldMechanic worldMechanic;

    [Tooltip("Renderers donde se cambiará solo UN material.")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Material to switch")]
    [Tooltip("Índice del material que debe cambiar (0 = primer material, 1 = segundo, etc.)")]
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
                Debug.LogError("[TurretEnergyMaterialSwitcher] No se encontró ShiftingWorldMechanic en la escena.");
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
            Debug.LogWarning("[TurretEnergyMaterialSwitcher] Falta asignar materiales en el switcher.");
            return;
        }

        foreach (Renderer rend in targetRenderers)
        {
            if (rend == null) continue;

            // Clonamos el array de materiales, ya que rend.materials instancia
            // y rend.sharedMaterials los afecta a todos los hijos.
            Material[] mats = rend.materials;

            if (materialIndexToChange < 0 || materialIndexToChange >= mats.Length)
            {
                Debug.LogWarning($"[TurretEnergyMaterialSwitcher] El índice {materialIndexToChange} no existe en el renderer {rend.name}");
                continue;
            }

            // Cambiamos SOLO ese material
            mats[materialIndexToChange] = targetMaterial;

            rend.materials = mats;
        }
    }
}
