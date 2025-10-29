using UnityEngine;
using System;

[DisallowMultipleComponent]
public class PathEffectManager : MonoBehaviour
{
    public static PathEffectManager Instance { get; private set; }

    [Header("Master")]
    public bool enableAll = true;

    // ===== Daño fijo =====
    [Header("Daño fijo (golpe)")]
    public bool enableDamage = true;
    [Min(0)] public int damagePerHit = 1;   // escala global del daño por golpe
    [Min(0.05f)] public float damageCooldown = 10f;  // cooldown global POR ENEMIGO para el daño

    // ===== Slow =====
    [Header("Slow (reducción de velocidad)")]
    public bool enableSlow = true;
    [Tooltip("Multiplica la fuerza de slow definida por el tile (0..1). 1 = igual al tile.")]
    [Range(0, 1)] public float slowMultiplier = 1f;    // controla CUÁNTO se reduce la velocidad
    [Range(0, 1)] public float slowMax = 0.95f;        // no más de 95% de reducción
    [Tooltip("Intervalo opcional de refresco para slow (si 0 usa el interval del PathCellEffect).")]
    [Min(0)] public float globalApplyIntervalOverride = 0f; // NO es cooldown; solo refresco de estado

    // ===== Stun =====
    [Header("Stun (aturdimiento)")]
    public bool enableStun = true;
    [Min(0)] public float stunMultiplier = 1f;        // escala global de duración de stun
    [Min(0)] public float stunMax = 10f;              // tope de duración
    [Min(0.05f)] public float stunCooldown = 0.5f;    // cooldown global POR ENEMIGO para re-aplicar stun

    public event Action OnValuesChanged;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

#if UNITY_EDITOR
    private void OnValidate() { OnValuesChanged?.Invoke(); }
#endif

    /// <summary>
    /// Aplica switches, multiplicadores y caps.
    /// Devuelve además los cooldowns específicos de daño y stun.
    /// </summary>
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

        // Daño fijo
        if (enableDamage)
            outDamagePerHit = inDamagePerHit;
        else
            outDamagePerHit = 0;

        // Slow fijo
        if (enableSlow)
            outSlow = inSlow;
        else
            outSlow = 0;

        // Stun fijo
        if (enableStun)
            outStun = inStun;
        else
            outStun = 0;

        // Cooldowns fijos por efecto
        outDamageCd = Mathf.Max(0.05f, damageCooldown);
        outStunCd = Mathf.Max(0.05f, stunCooldown);
    }

    public float GetApplyIntervalOverride() => globalApplyIntervalOverride;

    // Helpers para UI (opcionales)
    public void ToggleAll(bool on) { enableAll = on; OnValuesChanged?.Invoke(); }
    public void ToggleDamage(bool on) { enableDamage = on; OnValuesChanged?.Invoke(); }
    public void ToggleSlow(bool on) { enableSlow = on; OnValuesChanged?.Invoke(); }
    public void ToggleStun(bool on) { enableStun = on; OnValuesChanged?.Invoke(); }
}
