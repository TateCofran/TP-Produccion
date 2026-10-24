using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldProgressMaterialFiller : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShiftingWorldMechanic shiftingWorld;

    [Tooltip("Material used for the Normal world (has the FillPercent float).")]
    [SerializeField] private Material normalWorldMaterial;

    [Tooltip("Material used for the Other world (has the FillPercent float).")]
    [SerializeField] private Material otherWorldMaterial;

    [Header("Shader property")]
    [Tooltip("Name of the shader float that controls the fill (default _FillPercent).")]
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
                Debug.LogError("[WorldProgressMaterialFiller] ShiftingWorldMechanic not found in the scene");
            }
        }
    }

    private void Update()
    {
        if (shiftingWorld == null) return;

        float normalFill = shiftingWorld.GetNormalInt() / 100f;
        float otherFill = shiftingWorld.GetOtherInt() / 100f;

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
