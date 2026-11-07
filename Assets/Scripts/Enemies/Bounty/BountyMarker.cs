using UnityEngine;

[DisallowMultipleComponent]
public class BountyMarker : MonoBehaviour
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [Header("Stats")]
    [SerializeField, Min(1f)] private float healthMultiplier = 2f;
    [SerializeField, Min(0.1f)] private float speedMultiplier = 1.4f;

    [Header("Reward")]
    [SerializeField, Range(0f, 1f)] private float essenceChance = 0.6f;
    [SerializeField, Min(1)] private int essenceAmount = 30;

    [Header("Look")]
    [SerializeField] private Color gold = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField, Min(0f)] private float glow = 1.5f;

    private Enemy enemy;
    private EnemyHealth health;
    private EnemyEffects effects;
    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private ParticleSystem sparkles;
    private bool emissionPrepared;

    public bool IsActive { get; private set; }
    public static event System.Action<BountyMarker> OnBountyStarted;
    public static event System.Action<BountyMarker> OnBountyEnded;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        health = GetComponent<EnemyHealth>();
        effects = GetComponent<EnemyEffects>();
        renderers = GetComponentsInChildren<Renderer>(true);
        block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        Enemy.OnAnyEnemyKilled += HandleKilled;
    }

    private void OnDisable()
    {
        Enemy.OnAnyEnemyKilled -= HandleKilled;
    }

    public void Activate()
    {
        if (enemy == null) Awake();
        var elite = GetComponent<EliteController>();
        if (elite != null) elite.Clear();

        IsActive = true;
        if (health != null)
        {
            float max = health.GetMaxHealth() * healthMultiplier;
            health.SetMaxHealth(max);
            health.SetCurrentHealth(max);
        }
        if (effects != null) effects.AddSpeedMultiplier(this, speedMultiplier);
        ApplyGold(true);
        OnBountyStarted?.Invoke(this);
    }

    public void Clear()
    {
        bool wasActive = IsActive;
        IsActive = false;
        if (effects != null) effects.RemoveSpeedMultiplier(this);
        ApplyGold(false);
        if (wasActive) OnBountyEnded?.Invoke(this);
    }

    public void HandleEscaped()
    {
        if (!IsActive) return;
        FeedbackFxPool.MuzzleFlash(transform.position + Vector3.up, gold, 2.5f, 0.4f);
        var rewards = FindFirstObjectByType<RewardZoneSystem>();
        if (rewards != null) rewards.Toast("Bounty escaped");
        Log.Info(nameof(BountyMarker), "Bounty escaped");
        Clear();
    }

    private void HandleKilled(Enemy killed)
    {
        if (!IsActive || killed != enemy || enemy.ReachedCore) return;

        var rewards = FindFirstObjectByType<RewardZoneSystem>();
        if (rewards != null)
        {
            bool blue = WorldState.Current == ShiftingWorldMechanic.World.Normal;
            if (Random.value < essenceChance)
                rewards.Grant(blue ? RewardType.BlueEssence : RewardType.RedEssence, essenceAmount, "Bounty: ");
            else
                rewards.Grant(blue ? RewardType.FreeTileDupe : RewardType.FreeTurretDupe, 1, "Bounty: ");
        }
        FeedbackFxPool.MuzzleFlash(transform.position + Vector3.up, gold, 3f, 0.5f);
        Log.Info(nameof(BountyMarker), "Bounty killed");
        Clear();
    }

    private void ApplyGold(bool on)
    {
        if (renderers == null) return;
        if (on && !emissionPrepared)
        {
            emissionPrepared = true;
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

        foreach (var r in renderers)
        {
            if (r == null || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || r is SpriteRenderer) continue;
            r.GetPropertyBlock(block);
            if (on)
            {
                block.SetColor(BaseColorId, gold);
                block.SetColor(EmissionColorId, gold * glow);
            }
            else
            {
                block.Clear();
            }
            r.SetPropertyBlock(block);
        }

        if (on)
        {
            EnsureSparkles();
            sparkles.Play();
        }
        else if (sparkles != null)
        {
            sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void EnsureSparkles()
    {
        if (sparkles != null) return;
        var go = new GameObject("BountySparkles");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        sparkles = go.AddComponent<ParticleSystem>();
        sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = sparkles.main;
        main.loop = true;
        main.startLifetime = 0.6f;
        main.startSpeed = 0.6f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = gold;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;
        var emission = sparkles.emission;
        emission.rateOverTime = 18f;
        var shape = sparkles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        r.sharedMaterial = new Material(shader != null ? shader : Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
    }
}
