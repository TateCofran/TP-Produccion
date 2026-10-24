using UnityEngine;

public class Ability_DisableTowersEMP : MonoBehaviour, IEnemyAbility
{
    [Header("EMP Settings")]
    [SerializeField] private float radius = 6f;
    [SerializeField] private float disableDuration = 5f;
    [SerializeField] private float empInterval = 15f;

    [Header("Detection")]
    [SerializeField] private LayerMask towerLayers;

    [Header("FX")]
    [SerializeField] private GameObject empVfxPrefab;

    private AbilityContext _ctx;
    private float _timer;

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _timer = empInterval;
    }

    public void Tick(float dt)
    {
        _timer -= dt;
        if (_timer <= 0f)
        {
            CastEMP();
            _timer = empInterval;
        }
    }

    private void CastEMP()
    {
        if ( _ctx.Transform == null)
            return;

        Vector3 center = _ctx.Transform.position;

        Collider[] hits = Physics.OverlapSphere(center, radius, towerLayers);

        for (int i = 0; i < hits.Length; i++)
        {
            Turret turret = hits[i].GetComponentInParent<Turret>();
            if (turret == null)
                continue;

            turret.DisableFor(disableDuration);
        }

        if (empVfxPrefab != null)
        {
            VfxPool.Spawn(empVfxPrefab, center, Quaternion.identity, 3f);
        }
    }

    public void OnDamaged(float dmg, float currentHP, object source) { }
    public void OnDeath() { }
    public void OnArrivedToCore() { }
    public void OnWaypoint(int index) { }
    public void ResetRuntime()
    {
        _timer = empInterval;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
