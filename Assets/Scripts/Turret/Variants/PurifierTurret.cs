using UnityEngine;

[RequireComponent(typeof(TurretDataHolder))]
public class PurifierTurret : MonoBehaviour, IShootingBehavior
{
    [Header("Damage")]
    [Tooltip("Damage multiplier against enemies with a health shield (ShieldAura, ShieldOnLowHP).")]
    [SerializeField, Min(1f)] private float shieldMultiplier = 3f;

    [Header("Beam")]
    [SerializeField] private Color beamColor = new Color(1f, 0.95f, 0.6f, 0.9f);
    [SerializeField, Min(0.005f)] private float beamWidth = 0.08f;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.6f, 0f);

    private LineRenderer beam;
    private Transform beamTarget;
    private Transform beamOrigin;
    private float lastTick = -999f;
    private float tickInterval = 0.2f;

    public int ShieldedHits { get; private set; }
    public int InvulnerableHits { get; private set; }

    public void Shoot(Transform firePoint, Transform target, ITurretStats stats)
    {
        if (firePoint == null || target == null || stats == null) return;
        var enemy = target.GetComponentInParent<Enemy>();
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead()) return;

        tickInterval = 1f / Mathf.Max(0.01f, stats.FireRate);
        float tickDamage = stats.Damage * tickInterval;

        var health = enemy.Health;
        DamageFlags flags = DamageFlags.None;
        float multiplier = 1f;
        if (health.IsInvulnerable())
        {
            flags = DamageFlags.IgnoreInvulnerable;
            InvulnerableHits++;
        }
        else if (health.HasHealthShield)
        {
            multiplier = shieldMultiplier;
            ShieldedHits++;
        }

        health.TakeDamage(TurretDamageCalculator.Compute(tickDamage * multiplier, enemy), flags);

        beamOrigin = firePoint;
        beamTarget = target;
        lastTick = Time.time;
        EnsureBeam();
        beam.enabled = true;
        UpdateBeam();
    }

    private void Update()
    {
        if (beam == null || !beam.enabled) return;
        bool stale = Time.time - lastTick > tickInterval * 1.6f;
        if (stale || beamTarget == null || !beamTarget.gameObject.activeInHierarchy || beamOrigin == null)
        {
            beam.enabled = false;
            return;
        }
        UpdateBeam();
    }

    private void UpdateBeam()
    {
        beam.SetPosition(0, beamOrigin.position);
        beam.SetPosition(1, beamTarget.position + targetOffset);
        float flicker = 0.85f + Mathf.PerlinNoise(Time.time * 12f, 0f) * 0.3f;
        beam.widthMultiplier = beamWidth * flicker;
    }

    private void EnsureBeam()
    {
        if (beam != null) return;
        var go = new GameObject("PurifierBeam");
        go.transform.SetParent(transform, false);
        beam = go.AddComponent<LineRenderer>();
        beam.useWorldSpace = true;
        beam.positionCount = 2;
        beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        beam.receiveShadows = false;
        beam.numCapVertices = 3;
        beam.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
        beam.startColor = beamColor;
        beam.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, beamColor.a * 0.6f);
    }

    private void OnDisable()
    {
        if (beam != null) beam.enabled = false;
    }
}
