using UnityEngine;
using static Unity.VisualScripting.Member;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float defense;
    [SerializeField] private float currentHealth;
    [SerializeField] private float shieldCurrent; // cantidad de escudo actual

    private bool isDead = false;

    // Refs
    private Enemy enemyReference;
    private IHealthDisplay healthBarDisplay;
    private IEnemyDeathHandler deathHandler;

    // NUEVO: acceso concreto para popup
    private EnemyHealthBar healthBarConcrete;

    public event System.Action<float, float> OnDamaged; // (current, max)
    public event System.Action OnDied;
    public float GetShield() => shieldCurrent;


    private void Awake()
    {
        CacheDependencies();
        if (healthBarConcrete == null)
            healthBarConcrete = GetComponent<EnemyHealthBar>();

    }

    private void OnEnable()
    {
        CacheDependenciesIfMissing();
        if (isDead) isDead = false;
        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
    }

    private void CacheDependencies()
    {
        enemyReference = GetComponent<Enemy>();
        if (healthBarDisplay == null) healthBarDisplay = GetComponent<IHealthDisplay>();
        if (deathHandler == null) deathHandler = GetComponent<IEnemyDeathHandler>();
        if (healthBarConcrete == null) healthBarConcrete = GetComponent<EnemyHealthBar>();
    }

    private void CacheDependenciesIfMissing()
    {
        if (enemyReference == null || ReferenceEquals(enemyReference, null))
            enemyReference = GetComponent<Enemy>();
        if (healthBarDisplay == null)
            healthBarDisplay = GetComponent<IHealthDisplay>();
        if (deathHandler == null)
            deathHandler = GetComponent<IEnemyDeathHandler>();
        if (healthBarConcrete == null)
            healthBarConcrete = GetComponent<EnemyHealthBar>();
    }

    public void Initialize(float maxHealth, float defense)
    {
        this.maxHealth = Mathf.Max(1f, maxHealth);
        this.defense = Mathf.Max(0f, defense);
        currentHealth = this.maxHealth;
        isDead = false;

        shieldCurrent = 0f; // 🔹 reset escudo

        CacheDependenciesIfMissing();
        healthBarDisplay?.UpdateHealthBar(currentHealth, this.maxHealth);
        // si tu barra soporta overlay de escudo:
        (healthBarDisplay as EnemyHealthBar)?.SetShieldOverlay(0f);
    }


    public void InitializeFromData(EnemyData data)
    {
        if (data == null) { Initialize(10f, 0f); return; }
        Initialize(data.maxHealth, data.defense);
    }

    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(1f, value);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
    }

    public void SetCurrentHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        isDead = (currentHealth <= 0f);
        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
    }

    public void SetDefense(float value) => defense = Mathf.Max(0f, value);

    public void ReviveFull()
    {
        isDead = false;
        currentHealth = Mathf.Max(1f, maxHealth);
        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
        shieldCurrent = 0f;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        float modAmount = amount;
        if (GameModifiersManager.Instance != null)
            modAmount *= GameModifiersManager.Instance.enemyDamageTakenMultiplier;

        float realDamage = Mathf.Max(0f, modAmount - defense);
        if (realDamage <= 0f) return;

        currentHealth -= realDamage;
        if (currentHealth < 0f) currentHealth = 0f;

        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);

        // --- NUEVO: popup de daño (usa el realDamage) ---
        healthBarConcrete?.ShowDamageText(realDamage);

        OnDamaged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f) Die();

        // ... después de calcular realDamage ...
        float damageLeft = realDamage;

        // 🔹 Escudo absorbe primero
        if (shieldCurrent > 0f)
        {
            float absorbed = Mathf.Min(shieldCurrent, damageLeft);
            shieldCurrent -= absorbed;
            damageLeft -= absorbed;

            // actualizar overlay
            var hb = healthBarDisplay as EnemyHealthBar;
            if (hb != null) hb.SetShieldOverlay(Mathf.Clamp01(shieldCurrent / Mathf.Max(1f, maxHealth)));

            // si absorbió todo, no pasa a la vida
            if (damageLeft <= 0f)
            {
                OnDamaged?.Invoke(currentHealth, maxHealth);
                return;
            }
        }

        // 🔹 Lo que queda sí daña la vida
        currentHealth -= damageLeft;
        if (currentHealth < 0f) currentHealth = 0f;

        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
        // popup: si preferís mostrar solo lo que dañó vida, usá damageLeft en vez de realDamage
        //healthBarConcrete?.ShowDamageText(damageLeft, transform);

        OnDamaged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f) Die();

    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
        OnDamaged?.Invoke(currentHealth, maxHealth);
    }

    public void MultiplyMaxHealth(float multiplier)
    {
        multiplier = Mathf.Max(0.01f, multiplier);
        maxHealth = Mathf.Round(maxHealth * multiplier);
        if (maxHealth < 1f) maxHealth = 1f;

        currentHealth = Mathf.Min(currentHealth, maxHealth);
        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
    }

    public void AddShield(float amount)
    {
        if (amount <= 0f) return;
        shieldCurrent += amount;
        var hb = healthBarDisplay as EnemyHealthBar;
        if (hb != null) hb.SetShieldOverlay(Mathf.Clamp01(shieldCurrent / Mathf.Max(1f, maxHealth)));
    }

    public void SetShield(float amount)
    {
        shieldCurrent = Mathf.Max(0f, amount);
        var hb = healthBarDisplay as EnemyHealthBar;
        if (hb != null) hb.SetShieldOverlay(Mathf.Clamp01(shieldCurrent / Mathf.Max(1f, maxHealth)));
    }

    public void ClearShield()
    {
        if (shieldCurrent <= 0f) return;
        shieldCurrent = 0f;
        var hb = healthBarDisplay as EnemyHealthBar;
        if (hb != null) hb.SetShieldOverlay(0f);
    }


    public void Die()
    {
        if (isDead) return;
        isDead = true;

        healthBarDisplay?.DestroyBar();
        deathHandler?.OnEnemyDeath(enemyReference);
        OnDied?.Invoke();
        shieldCurrent = 0f;
    }

    public bool IsDead() => isDead;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetDefense() => defense;
}
