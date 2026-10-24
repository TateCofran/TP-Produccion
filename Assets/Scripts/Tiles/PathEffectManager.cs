using UnityEngine;
using System;

[DisallowMultipleComponent]
public class PathEffectManager : MonoBehaviour
{
    public static PathEffectManager Instance { get; private set; }

    [Header("Master")]
    public bool enableAll = true;

    [Header("Fixed Damage (hit)")]
    public bool enableDamage = true;
    [Min(0)] public int damagePerHit = 1;
    [Min(0.05f)] public float damageCooldown = 10f;

    [Header("Slow (speed reduction)")]
    public bool enableSlow = true;
    [Tooltip("Multiplies the slow strength defined by the tile (0..1). 1 = same as the tile.")]
    [Range(0, 1)] public float slowMultiplier = 1f;
    [Range(0, 1)] public float slowMax = 0.95f;
    [Tooltip("Optional slow refresh interval (0 uses the PathCellEffect interval).")]
    [Min(0)] public float globalApplyIntervalOverride = 0f;

    [Header("Stun")]
    public bool enableStun = true;
    [Min(0)] public float stunMultiplier = 1f;
    [Min(0)] public float stunMax = 10f;
    [Min(0.05f)] public float stunCooldown = 0.5f;

    [Header("Other World Flip")]
    [Tooltip("In the Other World, slow tiles deal damage and stun tiles push enemies back.")]
    public bool enableWorldFlip = true;
    [Tooltip("Damage per tick for a slow tile with strength 1 (scaled by the tile slow value).")]
    [Min(0)] public float otherWorldSlowDamage = 4f;
    [Min(0.05f)] public float otherWorldSlowDamageInterval = 1f;
    [Tooltip("Knockback distance per second of stun defined by the tile.")]
    [Min(0)] public float knockbackPerStunSecond = 1.5f;
    [Min(0)] public float maxKnockbackDistance = 3f;
    [Tooltip("Seconds an enemy cannot be pushed again after a knockback.")]
    [Min(0)] public float knockbackImmunity = 3f;
    [Min(0)] public int maxKnockbacksPerEnemy = 3;

    public bool IsWorldFlipped => enableAll && enableWorldFlip && WorldState.IsOther;

    public event Action OnValuesChanged;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

#if UNITY_EDITOR
    private void OnValidate() { OnValuesChanged?.Invoke(); }
#endif

    public void GetEffective(
        int inDamagePerHit, float inSlow, float inStun,
        out int outDamagePerHit, out float outSlow, out float outStun,
        out float outDamageCd, out float outStunCd)
    {
        if (!enableAll)
        {
            outDamagePerHit = 0;
            outSlow = 0;
            outStun = 0;
            outDamageCd = Mathf.Max(0.05f, damageCooldown);
            outStunCd = Mathf.Max(0.05f, stunCooldown);
            return;
        }

        if (enableDamage)
            outDamagePerHit = inDamagePerHit;
        else
            outDamagePerHit = 0;

        if (enableSlow)
            outSlow = inSlow;
        else
            outSlow = 0;

        if (enableStun)
            outStun = inStun;
        else
            outStun = 0;

        outDamageCd = Mathf.Max(0.05f, damageCooldown);
        outStunCd = Mathf.Max(0.05f, stunCooldown);
    }

    public float GetApplyIntervalOverride() => globalApplyIntervalOverride;

    public void ToggleAll(bool on) { enableAll = on; OnValuesChanged?.Invoke(); }
    public void ToggleDamage(bool on) { enableDamage = on; OnValuesChanged?.Invoke(); }
    public void ToggleSlow(bool on) { enableSlow = on; OnValuesChanged?.Invoke(); }
    public void ToggleStun(bool on) { enableStun = on; OnValuesChanged?.Invoke(); }
}
