using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyEffects : MonoBehaviour, IPathAffectable
{
    [SerializeField] private EnemyHealth health;

    public bool IsStunned => _stunUntil > Time.time;
    public float CurrentSpeedMultiplier
    {
        get
        {
            if (IsStunned) return 0f;

            // producto de multiplicadores de velocidad * (1 - slow)
            float mult = 1f;

            // purgar vencidos (si tienen duración)
            PurgeExpired(_speedMults);

            foreach (var kv in _speedMults)
                mult *= Mathf.Max(0f, kv.Value.value);

            // tu slow legacy (0..1) ya existente
            mult *= Mathf.Clamp01(1f - _currentSlow);

            return Mathf.Clamp(mult, 0f, 2f);
        }
    }


    // --- NUEVO: soporte para múltiples buffs de velocidad con expiración opcional ---
    private struct TimedValue<T> { public T value; public float until; } // until<=0 => infinito
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
    }

    public void AddSpeedMultiplier(object key, float multiplier)
    {
        if (key == null) key = this;
        _speedMults[key] = new TimedValue<float> { value = multiplier, until = 0f }; // infinito
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

    // Utilidad para purgar expirados
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


    // ----- NUEVO -----
    public void ApplyInstantDamage(int amount)
    {
        if (amount <= 0 || health == null) return;
        health.TakeDamage(amount);
    }

    // ----- Legacy (podés no usarlos) -----
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
