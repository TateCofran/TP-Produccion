using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HitFlash : MonoBehaviour
{
    public static event Action<EnemyHealth, float, Vector3> OnEnemyHit;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [Header("Refs")]
    [SerializeField] private EnemyHealth health;
    [Tooltip("Optional. Scaled for the hit punch. Uses this transform when empty.")]
    [SerializeField] private Transform punchTarget;

    [Header("Look")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Min(0f)] private float flashIntensity = 1.5f;

    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private float lastHealth = -1f;
    private float lastFlashTime = -999f;
    private float flashUntil;
    private float punchStart = -999f;
    private Vector3 baseScale;
    private bool flashing;
    private bool punching;
    private bool emissionPrepared;

    private void Awake()
    {
        if (health == null) health = GetComponent<EnemyHealth>();
        if (punchTarget == null) punchTarget = transform;
        baseScale = punchTarget.localScale;
        renderers = GetComponentsInChildren<Renderer>(true);
        block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDamaged += HandleDamaged;
            health.OnHealthReset += HandleHealthReset;
            lastHealth = health.GetCurrentHealth();
        }
        flashing = false;
        punching = false;
        if (punchTarget != null) punchTarget.localScale = baseScale;
        ApplyEmission(Color.black);
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnHealthReset -= HandleHealthReset;
        }
        if (punchTarget != null) punchTarget.localScale = baseScale;
    }

    private void HandleHealthReset()
    {
        lastHealth = health != null ? health.GetCurrentHealth() : -1f;
    }

    private void HandleDamaged(float current, float max)
    {
        float previous = lastHealth < 0f || lastHealth > max ? max : lastHealth;
        lastHealth = current;
        float damage = previous - current;
        if (damage <= 0f) return;

        OnEnemyHit?.Invoke(health, damage, transform.position);

        var settings = FeedbackSettings.Current;
        if (Time.time - lastFlashTime < settings.hitMinInterval) return;
        lastFlashTime = Time.time;

        float flash = settings.FlashMultiplier;
        if (flash > 0f)
        {
            PrepareEmission();
            ApplyEmission(flashColor * flashIntensity * flash);
            flashUntil = Time.time + settings.hitFlashDuration;
            flashing = true;
        }

        if (settings.hitPunchDuration > 0f)
        {
            punchStart = Time.time;
            punching = true;
        }
    }

    private void Update()
    {
        if (flashing && Time.time >= flashUntil)
        {
            flashing = false;
            ApplyEmission(Color.black);
        }

        if (punching)
        {
            var settings = FeedbackSettings.Current;
            float k = (Time.time - punchStart) / settings.hitPunchDuration;
            if (k >= 1f)
            {
                punching = false;
                punchTarget.localScale = baseScale;
            }
            else
            {
                float s = k < 0.5f ? Mathf.Lerp(1f, settings.hitPunchScale, k * 2f) : Mathf.Lerp(settings.hitPunchScale, 1f, (k - 0.5f) * 2f);
                punchTarget.localScale = baseScale * s;
            }
        }
    }

    private void PrepareEmission()
    {
        if (emissionPrepared) return;
        emissionPrepared = true;

        foreach (var r in renderers)
        {
            if (r == null || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            var mats = r.materials;
            foreach (var m in mats)
            {
                if (m == null || !m.HasProperty(EmissionColorId)) continue;
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                m.SetColor(EmissionColorId, Color.black);
            }
        }
    }

    private void ApplyEmission(Color color)
    {
        if (renderers == null || block == null) return;
        foreach (var r in renderers)
        {
            if (r == null || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            r.GetPropertyBlock(block);
            block.SetColor(EmissionColorId, color);
            r.SetPropertyBlock(block);
        }
    }
}
