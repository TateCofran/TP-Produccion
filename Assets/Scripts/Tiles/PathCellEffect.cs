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

    [Header("Special Effects")]
    public bool exposure;
    public bool antiHeal;
    [Min(0.05f)] public float exposureSeconds = 0.5f;
    [Min(0.05f)] public float healBlockSeconds = 4f;

    [Header("World Lock")]
    public WorldAffinity activeIn = WorldAffinity.Neutral;
    [Min(0f)] public float lockedStrengthMultiplier = 2f;
    [Range(0f, 1f)] public float inactiveDesaturation = 0.75f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private Renderer[] cellRenderers;
    private Color[] baseColors;
    private MaterialPropertyBlock visualBlock;
    private bool subscribedWorld;
    private bool shownInactive;

    private TileEffectKind[] kinds = new TileEffectKind[0];
    public TileEffectKind[] Kinds => kinds;
    public WorldAffinity ActiveIn => activeIn;

    public bool IsWorldLocked => activeIn != WorldAffinity.Neutral;
    public bool IsActiveNow => !IsWorldLocked || WorldState.Matches(activeIn);
    private float WorldStrength => IsWorldLocked ? (WorldState.Matches(activeIn) ? lockedStrengthMultiplier : 0f) : 1f;

    private readonly Dictionary<int, float> _nextDamageAllowed = new Dictionary<int, float>();
    private readonly Dictionary<int, float> _nextStunAllowed = new Dictionary<int, float>();

    private readonly Dictionary<int, float> _nextSlowRefresh = new Dictionary<int, float>();
    private readonly Dictionary<int, float> _nextFlipSlow = new Dictionary<int, float>();

    private float _baseDamage;
    private float _baseSlow;
    private float _baseStun;
    private float _damageMultiplier = 1f;
    private float _slowMultiplier = 1f;
    private float _stunMultiplier = 1f;

    public void Setup(float damageOrPrevDps, float slow, float stun)
    {
        var mods = new TileLayout.PathModifiers { dps = damageOrPrevDps, slow = slow, stun = stun };
        Setup(mods, WorldAffinity.Neutral, lockedStrengthMultiplier);
    }

    public void Setup(TileLayout.PathModifiers mods, WorldAffinity worldLock, float lockedMultiplier)
    {
        _baseDamage = Mathf.Max(0f, mods.dps);
        _baseSlow = Mathf.Clamp01(mods.slow);
        _baseStun = Mathf.Max(0f, mods.stun);
        exposure = mods.exposure;
        antiHeal = mods.antiHeal;
        activeIn = worldLock;
        lockedStrengthMultiplier = Mathf.Max(0f, lockedMultiplier);
        _damageMultiplier = 1f;
        _slowMultiplier = 1f;
        _stunMultiplier = 1f;
        RecalculateEffective();
        RebuildKinds();
        RefreshWorldVisual();
    }

    private void RebuildKinds()
    {
        var list = new System.Collections.Generic.List<TileEffectKind>(4);
        if (_baseDamage > 0f) list.Add(TileEffectKind.Dps);
        if (_baseSlow > 0f) list.Add(TileEffectKind.Slow);
        if (_baseStun > 0f) list.Add(TileEffectKind.Stun);
        if (exposure) list.Add(TileEffectKind.Exposure);
        if (antiHeal) list.Add(TileEffectKind.AntiHeal);
        if (activeIn != WorldAffinity.Neutral) list.Add(TileEffectKind.WorldLockedSameWorld);
        kinds = list.ToArray();
    }

    private void OnEnable()
    {
        if (!subscribedWorld)
        {
            WorldState.OnWorldChanged += HandleWorldChanged;
            subscribedWorld = true;
        }
        RefreshWorldVisual();
    }

    private void OnDisable()
    {
        if (subscribedWorld)
        {
            WorldState.OnWorldChanged -= HandleWorldChanged;
            subscribedWorld = false;
        }
    }

    private void HandleWorldChanged(ShiftingWorldMechanic.World world)
    {
        RefreshWorldVisual();
    }

    private void RefreshWorldVisual()
    {
        bool inactive = IsWorldLocked && !WorldState.Matches(activeIn);
        if (!inactive && !shownInactive) return;

        if (cellRenderers == null)
        {
            cellRenderers = GetComponentsInChildren<Renderer>(true);
            baseColors = new Color[cellRenderers.Length];
            for (int i = 0; i < cellRenderers.Length; i++)
            {
                var m = cellRenderers[i] != null ? cellRenderers[i].sharedMaterial : null;
                baseColors[i] = m != null && m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : Color.white;
            }
        }
        visualBlock ??= new MaterialPropertyBlock();

        for (int i = 0; i < cellRenderers.Length; i++)
        {
            var r = cellRenderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(visualBlock);
            if (inactive)
            {
                Color c = baseColors[i];
                float g = c.grayscale * 0.7f;
                visualBlock.SetColor(BaseColorId, Color.Lerp(c, new Color(g, g, g, c.a), inactiveDesaturation));
            }
            else
            {
                visualBlock.SetColor(BaseColorId, baseColors[i]);
            }
            r.SetPropertyBlock(visualBlock);
        }
        shownInactive = inactive;
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

    private bool IsBasicCell => _baseDamage <= 0f && _baseSlow <= 0f && _baseStun <= 0f && !exposure && !antiHeal;

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

        if (!IsActiveNow) return;

        int baseDamage = Mathf.RoundToInt(damagePerHit * WorldStrength);
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
        if (!IsActiveNow) return;

        ApplySpecialEffects(other);

        float strength = WorldStrength;
        int _; float effSlow, effStun, dmgCd, stunCd;
        var man = PathEffectManager.Instance;
        if (man) man.GetEffective(damagePerHit, Mathf.Clamp01(slow * strength), stun * strength, out _, out effSlow, out effStun, out dmgCd, out stunCd);
        else { effSlow = Mathf.Clamp01(slow * strength); effStun = stun * strength; stunCd = 0.5f; }

        bool flipped = man != null && man.IsWorldFlipped;
        bool primal = man != null && man.enableAll && man.enableWorldFlip && GameModifiersManager.Stat(ModifierStat.PrimalGround) > 0f;
        bool doFlip = flipped || primal;
        bool doNormal = !flipped || primal;
        float now = Time.time;

        if (effSlow > 0f)
        {
            if (doFlip && (!_nextFlipSlow.TryGetValue(id, out var tNextFlip) || now >= tNextFlip))
            {
                int dmg = Mathf.Max(1, Mathf.CeilToInt(effSlow * man.otherWorldSlowDamage));
                aff.ApplyInstantDamage(dmg);
                _nextFlipSlow[id] = now + man.otherWorldSlowDamageInterval;
            }
            if (doNormal && (!_nextSlowRefresh.TryGetValue(id, out var tNextSlow) || now >= tNextSlow))
            {
                float dt = GetStatusInterval();
                aff.ApplySlow(effSlow, dt + 0.05f + GetLingerSeconds());
                _nextSlowRefresh[id] = now + dt;
            }
        }

        if (effStun > 0f && (!_nextStunAllowed.TryGetValue(id, out var tNextStun) || now >= tNextStun))
        {
            if (doFlip)
            {
                var enemy = ((Component)aff).GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    float distance = Mathf.Min(man.maxKnockbackDistance, effStun * man.knockbackPerStunSecond);
                    enemy.TryKnockBack(distance, man.knockbackImmunity, man.maxKnockbacksPerEnemy);
                }
            }
            if (doNormal)
            {
                aff.ApplyStun(effStun + GetLingerSeconds());
            }
            _nextStunAllowed[id] = now + stunCd;
        }
    }

    private void ApplySpecialEffects(Collider other)
    {
        if (!exposure && !antiHeal) return;
        var effects = other.GetComponentInParent<EnemyEffects>();
        if (effects == null) return;
        float linger = GetLingerSeconds();
        if (exposure) effects.ApplyExposure(exposureSeconds + linger);
        if (antiHeal) effects.ApplyHealBlock(healBlockSeconds + linger);
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
        _nextFlipSlow.Remove(id);
    }
}
