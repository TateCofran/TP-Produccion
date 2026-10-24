using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldShiftPulse : MonoBehaviour
{
    [Header("Area")]
    [SerializeField] private float radius = 6f;
    [Tooltip("Optional origin. If empty, the active Core is used.")]
    [SerializeField] private Transform originOverride;

    [Header("Charge")]
    [Tooltip("Seconds spent in the previous world to reach full power.")]
    [SerializeField] private float timeForFullCharge = 20f;
    [SerializeField, Range(0f, 1f)] private float minPower = 0.25f;

    [Header("Effect at full power")]
    [SerializeField] private float damage = 30f;
    [SerializeField] private float stunSeconds = 1.5f;

    [Header("VFX")]
    [SerializeField] private GameObject pulseVfxPrefab;
    [SerializeField] private float vfxLifetime = 2f;
    [SerializeField] private bool scaleVfxWithRadius = false;

    public float Radius => radius;
    public float CurrentPower => ComputePower(WorldState.TimeInCurrentWorld);

    public event Action<float> OnPulse;

    private static readonly List<Enemy> _buffer = new List<Enemy>();

    private void OnEnable()
    {
        WorldState.OnWorldChanged += HandleWorldChanged;
    }

    private void OnDisable()
    {
        WorldState.OnWorldChanged -= HandleWorldChanged;
    }

    private void HandleWorldChanged(ShiftingWorldMechanic.World world)
    {
        Trigger(ComputePower(WorldState.TimeInPreviousWorld));
    }

    private float ComputePower(float secondsInWorld)
    {
        if (timeForFullCharge <= 0f) return 1f;
        return Mathf.Lerp(minPower, 1f, Mathf.Clamp01(secondsInWorld / timeForFullCharge));
    }

    private Vector3 GetOrigin()
    {
        if (originOverride != null) return originOverride.position;
        if (Core.Active != null) return Core.Active.transform.position;
        return transform.position;
    }

    private void Trigger(float power)
    {
        Vector3 origin = GetOrigin();
        float r2 = radius * radius;

        _buffer.Clear();
        var list = EnemyTracker.Enemies;
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e == null || e.Health == null || e.Health.IsDead()) continue;

            Vector3 d = e.transform.position - origin;
            d.y = 0f;
            if (d.sqrMagnitude <= r2) _buffer.Add(e);
        }

        float stun = stunSeconds * power;
        float dmg = damage * power;

        for (int i = 0; i < _buffer.Count; i++)
        {
            var e = _buffer[i];
            if (e == null || !e.isActiveAndEnabled || e.Health.IsDead()) continue;

            if (stun > 0f && e.TryGetComponent<EnemyEffects>(out var effects))
                effects.ApplyStun(stun);

            if (dmg > 0f)
                e.Health.TakeDamage(dmg);
        }

        if (pulseVfxPrefab != null)
        {
            var vfx = VfxPool.Spawn(pulseVfxPrefab, origin, Quaternion.identity, vfxLifetime);
            if (vfx != null && scaleVfxWithRadius)
                vfx.transform.localScale = Vector3.one * radius;
        }

        Debug.Log($"[WorldShiftPulse] Power {power:0.00}, enemies hit {_buffer.Count}");
        OnPulse?.Invoke(power);
        _buffer.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.3f, 1f, 0.6f);
        Gizmos.DrawWireSphere(GetOrigin(), radius);
    }
#endif
}
