using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class PathCellEffect : MonoBehaviour
{
    [Header("Valores del Tile (seteados por GridGenerator)")]
    [Min(0)] public int damagePerHit;   // daño por golpe (no dps)
    [Range(0, 1)] public float slow;     // intensidad del slow (0..1)
    [Min(0)] public float stun;         // duración de stun (s)

    [Header("Aplicación de slow/stun (sin cooldown para slow)")]
    [Min(0.05f)] public float applyInterval = 0.25f; // refresco de slow/stun (NO es cooldown)
    private float _nextStatusApplyTime;

    // Cooldown POR ENEMIGO (separados)
    private readonly Dictionary<int, float> _nextDamageAllowed = new Dictionary<int, float>();
    private readonly Dictionary<int, float> _nextStunAllowed = new Dictionary<int, float>();

    public void Setup(float damageOrPrevDps, float slow, float stun)
    {
        this.damagePerHit = Mathf.Max(0, Mathf.RoundToInt(damageOrPrevDps));
        this.slow = Mathf.Clamp01(slow);
        this.stun = Mathf.Max(0, stun);
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private float GetStatusInterval()
    {
        var man = PathEffectManager.Instance;
        float over = man ? man.GetApplyIntervalOverride() : 0f;
        return over > 0f ? over : applyInterval;
    }

    private bool TryGetAffectable(Collider other, out IPathAffectable aff, out int id)
    {
        aff = other.GetComponent<IPathAffectable>() ?? other.GetComponentInParent<IPathAffectable>();
        id = other.GetInstanceID();
        return aff != null;
    }

    private void ApplyDamageFixed(Collider other)
    {
        if (!TryGetAffectable(other, out var aff, out var id)) return;

        int effDmg; float effSlow, effStun, dmgCd, stunCd;
        var man = PathEffectManager.Instance;
        if (man) man.GetEffective(damagePerHit, slow, stun, out effDmg, out effSlow, out effStun, out dmgCd, out stunCd);
        else { effDmg = damagePerHit; effSlow = slow; effStun = stun; dmgCd = 0.5f; stunCd = 0.5f; }

        if (effDmg <= 0) return;

        float now = Time.time;
        if (_nextDamageAllowed.TryGetValue(id, out var tNext) && now < tNext) return;

        aff.ApplyInstantDamage(effDmg);
        _nextDamageAllowed[id] = now + dmgCd;
    }

    private void ApplySlowAndStun(Collider other)
    {
        // Slow: sin cooldown → se refresca por intervalo
        if (Time.time >= _nextStatusApplyTime)
        {
            if (TryGetAffectable(other, out var aff, out _))
            {
                int _; float effSlow, effStun, dmgCd, stunCd;
                var man = PathEffectManager.Instance;
                if (man) man.GetEffective(damagePerHit, slow, stun, out _, out effSlow, out effStun, out dmgCd, out stunCd);
                else { effSlow = slow; effStun = stun; }

                float dt = GetStatusInterval();
                if (effSlow > 0f) aff.ApplySlow(effSlow, dt); // refresco periódico
                _nextStatusApplyTime = Time.time + dt;
            }
        }

        // Stun: con cooldown POR ENEMIGO (independiente del intervalo de slow)
        if (TryGetAffectable(other, out var aff2, out var id))
        {
            int _; float __, effStun, dmgCd, stunCd;
            var man = PathEffectManager.Instance;
            if (man) man.GetEffective(damagePerHit, slow, stun, out _, out __, out effStun, out dmgCd, out stunCd);
            else { effStun = stun; stunCd = 0.5f; }

            if (effStun > 0f)
            {
                float now = Time.time;
                if (!_nextStunAllowed.TryGetValue(id, out var tNext) || now >= tNext)
                {
                    aff2.ApplyStun(effStun);
                    _nextStunAllowed[id] = now + stunCd;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ApplyDamageFixed(other);  // golpea al entrar (respeta cooldown por enemigo)
        ApplySlowAndStun(other);  // aplica slow (sin cd) + stun (con cd)
    }

    private void OnTriggerStay(Collider other)
    {
        ApplyDamageFixed(other);
        ApplySlowAndStun(other);
    }

    private void OnTriggerExit(Collider other)
    {
        int id = other.GetInstanceID();
        _nextDamageAllowed.Remove(id);
        _nextStunAllowed.Remove(id);
    }
}
