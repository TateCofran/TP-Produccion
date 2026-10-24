using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float defense;
    [SerializeField] private float currentHealth;

    [SerializeField] private bool shieldAuraApplied = false;

    [SerializeField] private bool invulnerable = false;

    private bool isDead = false;

    private Enemy enemyReference;
    private IHealthDisplay healthBarDisplay;
    private IEnemyDeathHandler deathHandler;

    private EnemyHealthBar healthBarConcrete;

    public event System.Action<float, float> OnDamaged;
    public event System.Action OnDied;

    public event System.Func<bool> OnPreDeath;

    public void SetInvulnerable(bool value) => invulnerable = value;
    public bool IsInvulnerable() => invulnerable;

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

        shieldAuraApplied = false;
        invulnerable = false;

        CacheDependenciesIfMissing();
        healthBarDisplay?.UpdateHealthBar(currentHealth, this.maxHealth);
    }

    public void InitializeFromData(EnemyData data)
    {
        if (data == null)
        {
            Initialize(10f, 0f);
            return;
        }
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

        shieldAuraApplied = false;

        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        if (invulnerable)
        {
            OnDamaged?.Invoke(currentHealth, maxHealth);
            return;
        }

        if (enemyReference != null && !enemyReference.IsTargetable()) return;

        float modAmount = amount;
        if (GameModifiersManager.Instance != null)
            modAmount *= GameModifiersManager.Instance.enemyDamageTakenMultiplier;

        float realDamage = Mathf.Max(0f, modAmount - defense);
        if (realDamage <= 0f) return;

        currentHealth -= realDamage;
        if (currentHealth < 0f) currentHealth = 0f;

        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);

        OnDamaged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    public bool TryApplyShieldAuraBoost(float multiplier)
    {
        if (shieldAuraApplied) return false;
        shieldAuraApplied = true;

        multiplier = Mathf.Max(0.01f, multiplier);

        float oldMax = maxHealth;
        float newMax = Mathf.Round(oldMax * multiplier);
        if (newMax < 1f) newMax = 1f;

        float extra = newMax - oldMax;

        maxHealth = newMax;
        currentHealth = Mathf.Min(maxHealth, currentHealth + extra);

        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
        return true;
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

    public void Die()
    {
        if (OnPreDeath != null)
        {
            foreach (System.Func<bool> handler in OnPreDeath.GetInvocationList())
            {
                try
                {
                    if (handler())
                        return;
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        if (isDead) return;
        isDead = true;

        healthBarDisplay?.DestroyBar();
        deathHandler?.OnEnemyDeath(enemyReference);
        OnDied?.Invoke();
    }

    public bool IsDead() => isDead;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetDefense() => defense;
}
