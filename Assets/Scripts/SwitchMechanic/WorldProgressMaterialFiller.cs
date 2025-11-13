using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldProgressMaterialFiller : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShiftingWorldMechanic shiftingWorld;

    [Tooltip("Material usado para el mundo Normal (tiene el float FillPercent).")]
    [SerializeField] private Material normalWorldMaterial;

    [Tooltip("Material usado para el mundo Otro (tiene el float FillPercent).")]
    [SerializeField] private Material otherWorldMaterial;

    [Header("Shader property")]
    [Tooltip("Nombre del parámetro float en el shader que controla el fill (por defecto _FillPercent).")]
    [SerializeField] private string fillPercentProperty = "_FillPercent";

    private int _fillPercentId;

    private void Awake()
    {
        _fillPercentId = Shader.PropertyToID(fillPercentProperty);

        if (shiftingWorld == null)
        {
            shiftingWorld = FindFirstObjectByType<ShiftingWorldMechanic>();
            if (shiftingWorld == null)
            {
                Debug.LogError("[WorldProgressMaterialFiller] No se encontró ShiftingWorldMechanic en la escena.");
            }
        }
    }

    private void Update()
    {
        if (shiftingWorld == null) return;

        // Traemos los porcentajes 0..100 del mechanic y los normalizamos 0..1
        float normalFill = shiftingWorld.GetNormalInt() / 100f;
        float otherFill = shiftingWorld.GetOtherInt() / 100f;

        // Actualizamos los materiales si existen
        if (normalWorldMaterial != null)
        {
            normalWorldMaterial.SetFloat(_fillPercentId, normalFill);
        }

        if (otherWorldMaterial != null)
        {
            otherWorldMaterial.SetFloat(_fillPercentId, otherFill);
        }
    }
}
