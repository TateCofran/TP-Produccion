using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyEffects : MonoBehaviour, IPathAffectable, ISlowable
{
    [SerializeField] private EnemyHealth health;

    public bool IsStunned => _stunUntil > Time.time;
    public bool IsExposed => _exposedUntil > Time.time;
    public bool IsHealBlocked => _healBlockedUntil > Time.time;

    private float _exposedUntil;
    private float _healBlockedUntil;
    private ParticleSystem _healBlockFx;
    private static Material _healBlockMaterial;

    public void ApplyExposure(float duration)
    {
        if (duration <= 0f) return;
        _exposedUntil = Mathf.Max(_exposedUntil, Time.time + duration);
    }

    public void ApplyHealBlock(float duration)
    {
        if (duration <= 0f) return;
        _healBlockedUntil = Mathf.Max(_healBlockedUntil, Time.time + duration);
        EnsureHealBlockFx();
        if (_healBlockFx != null && !_healBlockFx.isPlaying) _healBlockFx.Play();
    }

    public void ClearStatus()
    {
        _exposedUntil = 0f;
        _healBlockedUntil = 0f;
        _stunUntil = 0f;
        _slowUntil = 0f;
        _currentSlow = 0f;
        if (_healBlockFx != null) _healBlockFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void EnsureHealBlockFx()
    {
        if (_healBlockFx != null) return;
        var go = new GameObject("HealBlockFx");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        _healBlockFx = go.AddComponent<ParticleSystem>();
        _healBlockFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = _healBlockFx.main;
        main.loop = true;
        main.startLifetime = 0.8f;
        main.startSpeed = 0.4f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        main.startColor = new Color(0.12f, 0.3f, 0.12f, 0.85f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;
        var emission = _healBlockFx.emission;
        emission.rateOverTime = 10f;
        var shape = _healBlockFx.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        if (_healBlockMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            _healBlockMaterial = new Material(shader != null ? shader : Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
        }
        r.sharedMaterial = _healBlockMaterial;
    }
    public float CurrentSpeedMultiplier
    {
        get
        {
            if (IsStunned) return 0f;

            float mult = 1f;

            PurgeExpired(_speedMults);

            foreach (var kv in _speedMults)
                mult *= Mathf.Max(0f, kv.Value.value);

            mult *= Mathf.Clamp01(1f - _currentSlow);

            return Mathf.Clamp(mult, 0f, 2f);
        }
    }

    private struct TimedValue<T> { public T value; public float until; }
    private readonly Dictionary<object, TimedValue<float>> _speedMults = new();
    private static readonly List<object> _toRemove = new();

    private float _currentSlow, _slowUntil, _stunUntil;
    private Coroutine _dotCo;

    private void Awake()
    {
        if (!health) health = GetComponent<EnemyHealth>();
    }

    private void Update()
    {
        if (Time.time > _slowUntil) _currentSlow = 0f;
        if (_healBlockFx != null && _healBlockFx.isPlaying && !IsHealBlocked) _healBlockFx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    public void AddSpeedMultiplier(object key, float multiplier)
    {
        if (key == null) key = this;
        _speedMults[key] = new TimedValue<float> { value = multiplier, until = 0f };
    }

    public void AddSpeedMultiplier(object key, float multiplier, float duration)
    {
        if (key == null) key = this;
        float until = duration > 0f ? Time.time + duration : 0f;
        _speedMults[key] = new TimedValue<float> { value = multiplier, until = until };
    }

    public void RemoveSpeedMultiplier(object key)
    {
        if (key == null) key = this;
        _speedMults.Remove(key);
    }

    private void PurgeExpired(Dictionary<object, TimedValue<float>> dict)
    {
        if (dict.Count == 0) return;
        float now = Time.time;
        _toRemove.Clear();
        foreach (var kv in dict)
        {
            if (kv.Value.until > 0f && now > kv.Value.until)
                _toRemove.Add(kv.Key);
        }
        for (int i = 0; i < _toRemove.Count; i++)
            dict.Remove(_toRemove[i]);
        _toRemove.Clear();
    }

    public void ApplyInstantDamage(int amount)
    {
        if (amount <= 0 || health == null) return;
        health.TakeDamage(amount);
    }

    public void ApplyDoT(float dps, float duration)
    {
        if (dps <= 0f || duration <= 0f || health == null) return;
        if (_dotCo != null) StopCoroutine(_dotCo);
        _dotCo = StartCoroutine(DotRoutine(dps, duration));
    }
    public void ApplySlow(float slowPercent, float duration)
    {
        if (slowPercent <= 0f || duration <= 0f) return;
        _currentSlow = Mathf.Max(_currentSlow, Mathf.Clamp01(slowPercent));
        _slowUntil = Mathf.Max(_slowUntil, Time.time + duration);
    }
    public void ApplyStun(float stunSeconds)
    {
        if (stunSeconds <= 0f) return;
        _stunUntil = Mathf.Max(_stunUntil, Time.time + stunSeconds);
    }

    private IEnumerator DotRoutine(float dps, float duration)
    {
        float end = Time.time + duration, last = Time.time;
        while (Time.time < end)
        {
            float now = Time.time, dt = now - last; last = now;
            int dmg = Mathf.CeilToInt(dps * dt);
            if (dmg > 0 && health != null) health.TakeDamage(dmg);
            yield return null;
        }
        _dotCo = null;
    }
}
