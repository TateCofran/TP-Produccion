using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public sealed class FogWorldMaterialSwitcher : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material normalWorldMaterial;
    [SerializeField] private Material otherWorldMaterial;

    [Header("Initial State")]
    [SerializeField] private bool startsInOtherWorld = false;

    private Renderer _renderer;
    private bool _isInOtherWorld;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        _isInOtherWorld = startsInOtherWorld;
        ApplyCurrentMaterial();
    }

    /// <summary>
    /// isOtherWorld = true  -> usa otherWorldMaterial.
    /// isOtherWorld = false -> usa normalWorldMaterial.
    /// </summary>
    public void SetWorld(bool isOtherWorld)
    {
        _isInOtherWorld = isOtherWorld;
        ApplyCurrentMaterial();
    }

    public void ToggleWorld()
    {
        _isInOtherWorld = !_isInOtherWorld;
        ApplyCurrentMaterial();
    }

    private void ApplyCurrentMaterial()
    {
        if (_renderer == null)
            return;

        Material target = _isInOtherWorld ? otherWorldMaterial : normalWorldMaterial;
        if (target == null)
            return;

        _renderer.material = target;
    }
}
