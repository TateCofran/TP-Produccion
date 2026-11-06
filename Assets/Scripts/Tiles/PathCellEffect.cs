using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class PathCellEffect : MonoBehaviour
{
    [Header("Tile Values (set by GridGenerator)")]
    [Min(0)] public int damagePerHit;
    [Range(0, 1)] public float slow;
    [Min(0)] public float stun;

    [Header("Slow/Stun Application (slow has no cooldown)")]
    [Min(0.05f)] public float applyInterval = 0.25f;

    private readonly Dictionary<int, float> _nextDamageAllowed = new Dictionary<int, float>();
    private readonly Dictionary<int, float> _nextStunAllowed = new Dictionary<int, float>();

    private readonly Dictionary<int, float> _nextSlowRefresh = new Dictionary<int, float>();

    private float _baseDamage;
    private float _baseSlow;
    private float _baseStun;
    private float _damageMultiplier = 1f;
    private float _slowMultiplier = 1f;
    private float _stunMultiplier = 1f;

    public void Setup(float damageOrPrevDps, float slow, float stun)
    {
        _baseDamage = Mathf.Max(0f, damageOrPrevDps);
        _baseSlow = Mathf.Clamp01(slow);
        _baseStun = Mathf.Max(0f, stun);
        _damageMultiplier = 1f;
        _slowMultiplier = 1f;
        _stunMultiplier = 1f;
        RecalculateEffective();
    }

    public void SetLevelMultipliers(float dmg, float slowMult, float stunMult)
    {
        _damageMultiplier = Mathf.Max(0f, dmg);
        _slowMultiplier = Mathf.Max(0f, slowMult);
        _stunMultiplier = Mathf.Max(0f, stunMult);
        RecalculateEffective();
    }

    private void RecalculateEffective()
    {
        damagePerHit = Mathf.Max(0, Mathf.RoundToInt(_baseDamage * _damageMultiplier));
        slow = Mathf.Clamp01(_baseSlow * _slowMultiplier);
        stun = Mathf.Max(0f, _baseStun * _stunMultiplier);
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private bool IsBasicCell => _baseDamage <= 0f && _baseSlow <= 0f && _baseStun <= 0f;

    private static float GetLingerSeconds()
    {
        return Mathf.Max(0f, GameModifiersManager.Stat(ModifierStat.EffectLingerSeconds));
    }

    private float GetStatusInterval()
    {
        var man = PathEffectManager.Instance;
        float over = man ? man.GetApplyIntervalOverride() : 0f;
        return over > 0f ? over : applyInterval;
    }

    private bool TryGetAffectable(Collider other, out IPathAffectable aff, out int id)
    {
        aff = other.GetComponentInParent<IPathAffectable>();
        if (aff != null)
        {
            var c = (Component)aff;
            id = c.gameObject.GetInstanceID();
            return true;
        }

        id = 0;
        return false;
    }

    private void ApplyDamageFixed(Collider other)
    {
        if (!TryGetAffectable(other, out var aff, out var id)) return;

        int baseDamage = damagePerHit;
        if (IsBasicCell)
            baseDamage = Mathf.Max(0, Mathf.RoundToInt(GameModifiersManager.Stat(ModifierStat.BasicTileDamage)));

        int effDmg; float effSlow, effStun, dmgCd, stunCd;
        var man = PathEffectManager.Instance;
        if (man) man.GetEffective(baseDamage, slow, stun, out effDmg, out effSlow, out effStun, out dmgCd, out stunCd);
        else { effDmg = baseDamage; effSlow = slow; effStun = stun; dmgCd = 0.5f; stunCd = 0.5f; }

        if (effDmg <= 0) return;

        float now = Time.time;
        if (_nextDamageAllowed.TryGetValue(id, out var tNext) && now < tNext) return;

        aff.ApplyInstantDamage(effDmg);
        _nextDamageAllowed[id] = now + dmgCd;
    }

    private void ApplySlowAndStun(Collider other)
    {
        if (!TryGetAffectable(other, out var aff, out var id)) return;

        int _; float effSlow, effStun, dmgCd, stunCd;
        var man = PathEffectManager.Instance;
        if (man) man.GetEffective(damagePerHit, slow, stun, out _, out effSlow, out effStun, out dmgCd, out stunCd);
        else { effSlow = slow; effStun = stun; stunCd = 0.5f; }

        bool flipped = man != null && man.IsWorldFlipped;
        float now = Time.time;

        if (effSlow > 0f && (!_nextSlowRefresh.TryGetValue(id, out var tNextSlow) || now >= tNextSlow))
        {
            if (flipped)
            {
                int dmg = Mathf.Max(1, Mathf.CeilToInt(effSlow * man.otherWorldSlowDamage));
                aff.ApplyInstantDamage(dmg);
                _nextSlowRefresh[id] = now + man.otherWorldSlowDamageInterval;
            }
            else
            {
                float dt = GetStatusInterval();
                aff.ApplySlow(effSlow, dt + 0.05f + GetLingerSeconds());
                _nextSlowRefresh[id] = now + dt;
            }
        }

        if (effStun > 0f && (!_nextStunAllowed.TryGetValue(id, out var tNextStun) || now >= tNextStun))
        {
            if (flipped)
            {
                var enemy = ((Component)aff).GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    float distance = Mathf.Min(man.maxKnockbackDistance, effStun * man.knockbackPerStunSecond);
                    enemy.TryKnockBack(distance, man.knockbackImmunity, man.maxKnockbacksPerEnemy);
                }
            }
            else
            {
                aff.ApplyStun(effStun + GetLingerSeconds());
            }
            _nextStunAllowed[id] = now + stunCd;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ApplyDamageFixed(other);
        ApplySlowAndStun(other);
    }

    private void OnTriggerStay(Collider other)
    {
        ApplyDamageFixed(other);
        ApplySlowAndStun(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!TryGetAffectable(other, out _, out var id)) return;
        _nextDamageAllowed.Remove(id);
        _nextStunAllowed.Remove(id);
        _nextSlowRefresh.Remove(id);
    }
}
