using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour, IEnemyDeathHandler, IWorldAware
{
    [SerializeField] private EnemyData data;
    public EnemyData Data => data;
    public EnemyType Type => data != null ? data.type : EnemyType.Minion;

    private bool hasHitCore = false;

    public bool ReachedCore => hasHitCore;

    private static int _nextSpawnId;
    public int SpawnId { get; private set; }
    public bool IsSameSpawn(int spawnId) => isActiveAndEnabled && !_removedFromWave && SpawnId == spawnId;

    public static event System.Action<Enemy> OnAnyEnemyKilled;

    public bool IsTargetable() => data == null || WorldState.Matches(data.vulnerableIn);

    private float _knockbackImmuneUntil;
    private int _knockbackCount;

    [Header("Settings")]
    [SerializeField] private float waypointTolerance = 0.03f;
    [SerializeField] private bool faceDirection = true;
    [SerializeField] private Vector3 facingOffsetEuler = Vector3.zero;

    private readonly List<Vector3> _route = new List<Vector3>();
    private int _idx;

    public EnemyHealth Health { get; private set; }
    public EnemyHealthBar HealthBar { get; private set; }

    private EnemyEffects _effects;
    private List<IEnemyAbility> _abilities;

    private bool _removedFromWave;

    private void Awake()
    {
        Health = GetComponent<EnemyHealth>();
        HealthBar = GetComponent<EnemyHealthBar>();
        _effects = GetComponent<EnemyEffects>();

        if (data == null)
            Debug.LogError($"[Enemy] {name} has no EnemyData assigned");

        _abilities = GetComponents<IEnemyAbility>().ToList();
    }

    public void Init(IList<Vector3> worldRoute)
    {
        _route.Clear();
        if (worldRoute != null) _route.AddRange(worldRoute);
        _idx = 0;
        if (_route.Count > 0) transform.position = _route[0];

        SpawnId = ++_nextSpawnId;

        _removedFromWave = false;
        hasHitCore = false;
        _knockbackImmuneUntil = 0f;
        _knockbackCount = 0;

        if (Health != null && data != null)
            Health.Initialize(data.maxHealth, data.defense);

        HealthBar?.Initialize(transform, Health != null ? Health.GetMaxHealth() : 1f);

        var ctx = new AbilityContext
        {
            Enemy = this,
            Health = Health,
            Effects = _effects,
            Transform = transform,
            GetRoute = () => _route,
            GetWaypointIndex = () => _idx
        };

        foreach (var ab in _abilities)
        {
            ab.ResetRuntime();
            ab.Initialize(ctx);
        }
    }

    private void Update()
    {
        if (_route.Count == 0 || _idx >= _route.Count) return;
        if (_effects != null && _effects.IsStunned) return;

        var current = transform.position;
        var target = _route[_idx];
        var to = target - current;

        if (to.sqrMagnitude <= waypointTolerance * waypointTolerance)
        {
            _idx++;
            if (_idx >= _route.Count)
            {
                OnArrived();
                return;
            }
            target = _route[_idx];
            to = target - current;
        }

        var dir = to.normalized;

        float speedMultiplier = 1f;
        if (_effects != null)
            speedMultiplier = Mathf.Clamp(_effects.CurrentSpeedMultiplier, 0f, 3f);

        float step = data.moveSpeed * speedMultiplier * Time.deltaTime;
        transform.position = Vector3.MoveTowards(current, target, step);

        if (faceDirection && dir.sqrMagnitude > 0.0001f)
        {
            var forward = new Vector3(dir.x, 0f, dir.z);

            var look = Quaternion.LookRotation(forward, Vector3.up);

            var finalRotation = look * Quaternion.Euler(facingOffsetEuler);

            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, 0.25f);
        }

        float dt = Time.deltaTime;
        for (int i = 0; i < _abilities.Count; i++)
            _abilities[i].Tick(dt);
    }

    public bool TryKnockBack(float distance, float immunitySeconds, int maxPerLife)
    {
        if (distance <= 0f || _removedFromWave || hasHitCore) return false;
        if (_route.Count == 0 || _idx <= 0 || _idx > _route.Count) return false;
        if (Time.time < _knockbackImmuneUntil || _knockbackCount >= maxPerLife) return false;

        Vector3 pos = transform.position;
        float remaining = distance;
        int idx = Mathf.Min(_idx, _route.Count - 1);

        while (remaining > 0f && idx > 0)
        {
            Vector3 prev = _route[idx - 1];
            float segment = Vector3.Distance(pos, prev);
            if (segment >= remaining)
            {
                pos = Vector3.MoveTowards(pos, prev, remaining);
                remaining = 0f;
            }
            else
            {
                pos = prev;
                remaining -= segment;
                idx--;
            }
        }

        transform.position = pos;
        _idx = idx;
        _knockbackImmuneUntil = Time.time + immunitySeconds;
        _knockbackCount++;
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHitCore) return;

        if (other.TryGetComponent<Core>(out var core))
            HitCore(core);
    }

    private void OnEnable()
    {
        EnemyTracker.RegisterEnemy(this);
        _removedFromWave = false;
        hasHitCore = false;
        SubscribeHealthEvents();
    }

    private void OnDisable()
    {
        EnemyTracker.UnregisterEnemy(this);
        UnsubscribeHealthEvents();
    }

    private void SubscribeHealthEvents()
    {
        if (Health == null) Health = GetComponent<EnemyHealth>();
        if (Health == null) return;

        Health.OnDamaged += HandleOnDamagedCompat;
    }

    private void UnsubscribeHealthEvents()
    {
        if (Health == null) return;
        Health.OnDamaged -= HandleOnDamagedCompat;
    }

    private void HandleOnDamagedCompat(float current, float max)
    {
        for (int i = 0; i < _abilities.Count; i++)
            _abilities[i].OnDamaged(0f, current, null);
    }

    private void OnArrived()
    {
        if (_removedFromWave) return;
        HitCore(Core.Active);
    }

    private void HitCore(Core core)
    {
        if (hasHitCore || _removedFromWave) return;
        hasHitCore = true;

        foreach (var ab in _abilities)
            ab.OnArrivedToCore();

        if (core != null && data != null)
            core.TakeDamage(data.damageToCore);

        NotifyDeath();
    }

    public void ResetEnemy()
    {
        var health = GetComponent<EnemyHealth>();
        if (health == null) health = gameObject.AddComponent<EnemyHealth>();
        var healthBar = GetComponent<EnemyHealthBar>();
        if (healthBar == null) healthBar = gameObject.AddComponent<EnemyHealthBar>();

        if (Data != null)
        {
            health.SetMaxHealth(Data.maxHealth);
            health.SetDefense(Data.defense);
            health.SetCurrentHealth(Data.maxHealth);
        }
        else
        {
            if (health.GetMaxHealth() <= 0f) health.SetMaxHealth(10f);
            health.SetCurrentHealth(health.GetMaxHealth());
        }

        healthBar.Initialize(this.transform, health.GetMaxHealth());
        healthBar.UpdateHealthBar(health.GetCurrentHealth(), health.GetMaxHealth());

        hasHitCore = false;
        _removedFromWave = false;
        _idx = 0;
    }

    public void OnEnemyDeath(Enemy e)
    {
        if (e != this) return;

        foreach (var ab in _abilities)
            ab.OnDeath();

        NotifyDeath();
    }

    public void NotifyDeath()
    {
        if (_removedFromWave) return;

        _removedFromWave = true;

        OnAnyEnemyKilled?.Invoke(this);

        ReturnToPoolOrDisable();
    }

    private void RemoveFromWaveOnce()
    {
        if (_removedFromWave) return;
        _removedFromWave = true;
    }

    private void ReturnToPoolOrDisable()
    {
        if (EnemyPool.Instance != null)
            EnemyPool.Instance.ReturnEnemy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
