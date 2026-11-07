using UnityEngine;

[DisallowMultipleComponent]
public class RiftCannonTurret : MonoBehaviour, IDamageMultiplierProvider, ITurretInfoProvider
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [Header("Charge")]
    [Tooltip("Damage bonus gained per second spent in the same world (0.08 = +8%).")]
    [SerializeField, Min(0f)] private float bonusPerSecond = 0.08f;
    [Tooltip("Maximum damage bonus (2 = +200%).")]
    [SerializeField, Min(0f)] private float maxBonus = 2f;

    [Header("Visual")]
    [SerializeField] private Color chargeColor = new Color(0.7f, 0.3f, 1f, 1f);
    [SerializeField, Min(0f)] private float maxEmission = 3f;
    [SerializeField] private Color dischargeColor = new Color(0.8f, 0.5f, 1f, 1f);

    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private float nextVisualUpdate;
    private float chargeRateMultiplier = 1f;

    public float ChargeRateMultiplier
    {
        get => chargeRateMultiplier;
        set => chargeRateMultiplier = Mathf.Max(0f, value);
    }

    public float CurrentBonus => Mathf.Min(maxBonus, WorldState.TimeInCurrentWorld * bonusPerSecond * chargeRateMultiplier);

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        block = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            if (r == null || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || r is SpriteRenderer) continue;
            foreach (var m in r.materials)
            {
                if (m == null || !m.HasProperty(EmissionColorId)) continue;
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
        }
    }

    private void OnEnable()
    {
        WorldState.OnWorldChanged += HandleWorldChanged;
    }

    private void OnDisable()
    {
        WorldState.OnWorldChanged -= HandleWorldChanged;
    }

    public float GetDamageMultiplier(Enemy target)
    {
        return 1f + CurrentBonus;
    }

    public string GetInfoLine()
    {
        return $"Rift charge +{CurrentBonus * 100f:0}%";
    }

    private void HandleWorldChanged(ShiftingWorldMechanic.World world)
    {
        FeedbackFxPool.MuzzleFlash(transform.position + Vector3.up * 1f, dischargeColor, 2.2f, 0.35f);
        FeedbackFxPool.Beam(transform.position, transform.position + Vector3.up * 4f, dischargeColor, 0.5f, 0.3f);
        ApplyEmission(0f);
    }

    private void Update()
    {
        if (Time.time < nextVisualUpdate) return;
        nextVisualUpdate = Time.time + 0.2f;
        ApplyEmission(maxBonus > 0f ? CurrentBonus / maxBonus : 0f);
    }

    private void ApplyEmission(float normalized)
    {
        if (renderers == null) return;
        Color c = chargeColor * (maxEmission * normalized);
        foreach (var r in renderers)
        {
            if (r == null || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || r is SpriteRenderer) continue;
            r.GetPropertyBlock(block);
            block.SetColor(EmissionColorId, c);
            r.SetPropertyBlock(block);
        }
    }
}
