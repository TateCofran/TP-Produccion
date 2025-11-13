using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour, IEnemyDeathHandler // <- implementamos el handler
{
    [SerializeField] private EnemyData data;
    public EnemyData Data => data;
    public EnemyType Type => data != null ? data.type : EnemyType.Minion;

    private bool hasHitCore = false;

    public static event System.Action<Enemy> OnAnyEnemyKilled;

    [Header("Settings")]
    [SerializeField] private float waypointTolerance = 0.03f;
    [SerializeField] private bool faceDirection = true;

    private readonly List<Vector3> _route = new List<Vector3>();
    private int _idx;

    public EnemyHealth Health { get; private set; }
    public EnemyHealthBar HealthBar { get; private set; }

    private EnemyEffects _effects;
    private List<IEnemyAbility> _abilities;


    // Evita notificar muerte más de una vez (pooling / Destroy / llegada al core)
    private bool _removedFromWave; // renombrado para contemplar ambas causas

    private void Awake()
    {
        Health = GetComponent<EnemyHealth>();
        HealthBar = GetComponent<EnemyHealthBar>();
        _effects = GetComponent<EnemyEffects>();

        if (data == null)
            Debug.LogError($"[Enemy] {name} no tiene asignado EnemyData");

        _abilities = GetComponents<IEnemyAbility>().ToList();
    }

    public void Init(IList<Vector3> worldRoute)
    {
        _route.Clear();
        if (worldRoute != null) _route.AddRange(worldRoute);
        _idx = 0;
        if (_route.Count > 0) transform.position = _route[0];

        _removedFromWave = false;
        hasHitCore = false;

        if (Health != null && data != null)
            Health.Initialize(data.maxHealth, data.defense);

        HealthBar?.Initialize(transform, Health != null ? Health.GetMaxHealth() : 1f);

        // ?? Inicializamos las habilidades
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
            speedMultiplier = Mathf.Clamp(_effects.CurrentSpeedMultiplier, 0f, 3f); // 0..3 (o el cap que quieras)

        float step = data.moveSpeed * speedMultiplier * Time.deltaTime;
        transform.position = Vector3.MoveTowards(current, target, step);

        if (faceDirection && dir.sqrMagnitude > 0.0001f)
        {
            // Invertimos la dirección porque el modelo mira "al revés"
            var forward = -new Vector3(dir.x, 0f, dir.z);
            var look = Quaternion.LookRotation(forward, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 0.25f);
        }


        // ?? Tick de habilidades
        float dt = Time.deltaTime;
        for (int i = 0; i < _abilities.Count; i++)
            _abilities[i].Tick(dt);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (hasHitCore) return;

        if (other.TryGetComponent<Core>(out var core))
        {
            hasHitCore = true;
            core.TakeDamage(data.damageToCore);
            ReturnToPoolOrDisable();
        }
    }

    private void OnEnable()
    {
        EnemyTracker.RegisterEnemy(this);
        _removedFromWave = false;
        hasHitCore = false;
        SuscribeHealthEvents();
    }

    private void OnDisable()
    {
        EnemyTracker.UnregisterEnemy(this);
        UnsuscribeHealthEvents();
    }


    private void SuscribeHealthEvents()
    {
        if (Health == null) Health = GetComponent<EnemyHealth>();
        if (Health == null) return;

        // Compat: si sólo tenés OnDamaged(current,max)
        Health.OnDamaged += HandleOnDamagedCompat;
    }

    private void UnsuscribeHealthEvents()
    {
        if (Health == null) return;
        Health.OnDamaged -= HandleOnDamagedCompat;
    }

    private void HandleOnDamagedCompat(float current, float max)
    {
        // Versión compatible: no sabemos "amount" ni "source", pero SprintOnLowHP sólo usa currentHP
        for (int i = 0; i < _abilities.Count; i++)
            _abilities[i].OnDamaged(0f, current, null);
    }

    private void OnArrived()
    {
        if (_removedFromWave) return;
        hasHitCore = true;

        foreach (var ab in _abilities)
            ab.OnArrivedToCore();

        RemoveFromWaveOnce();
        ReturnToPoolOrDisable();
    }


    public void ResetEnemy()
    {
        // Asegurar dependencias
        var health = GetComponent<EnemyHealth>();
        if (health == null) health = gameObject.AddComponent<EnemyHealth>();
        var healthBar = GetComponent<EnemyHealthBar>();
        if (healthBar == null) healthBar = gameObject.AddComponent<EnemyHealthBar>();

        // Reset de stats
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

        // Inicializar barra
        healthBar.Initialize(this.transform, health.GetMaxHealth());
        healthBar.UpdateHealthBar(health.GetCurrentHealth(), health.GetMaxHealth());

        // Reset de flags/movimiento
        hasHitCore = false;
        _removedFromWave = false;
        _idx = 0;
    }

    public void OnEnemyDeath(Enemy e)
    {
        if (e != this) return;

        //Notificar habilidades antes de pool
        foreach (var ab in _abilities)
            ab.OnDeath();

        NotifyDeath();
    }


    public void NotifyDeath()
    {
        if (_removedFromWave) return;

        OnAnyEnemyKilled?.Invoke(this);
        RemoveFromWaveOnce();
        ReturnToPoolOrDisable(); // NO Destroy
    }

    private void RemoveFromWaveOnce()
    {
        if (_removedFromWave) return;
        _removedFromWave = true;
        WaveManager.Instance?.NotifyEnemyKilled();
    }

    private void ReturnToPoolOrDisable()
    {
        // Devolver al pool si existe; si no, como mínimo desactivar para no romper refs
        if (EnemyPool.Instance != null)
            EnemyPool.Instance.ReturnEnemy(gameObject);
        else
            gameObject.SetActive(false);
    }

}
