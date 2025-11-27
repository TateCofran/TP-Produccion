using System.Collections;
using System.Collections.Generic;
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

    // Para no afectar dos veces la misma torre
    private readonly HashSet<Turret> _affected = new HashSet<Turret>();

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _timer = empInterval;
        _affected.Clear();
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

        Debug.Log("[EMP] Cast desde " + _ctx.Transform.name + " colliders: " + hits.Length);

        for (int i = 0; i < hits.Length; i++)
        {
            Turret turret = hits[i].GetComponentInParent<Turret>();
            if (turret == null)
                continue;

            // Para debug
            Debug.Log("[EMP] Torre detectada: " + turret.name);

            if (_affected.Contains(turret))
                continue;

            _affected.Add(turret);
            DisableTower(turret);
        }

        if (empVfxPrefab != null)
        {
            var vfx = Object.Instantiate(empVfxPrefab, center, Quaternion.identity);
            Object.Destroy(vfx, 3f);
        }
    }

    private void DisableTower(Turret turret)
    {
        if (turret == null) return;

        // Desactiva comportamientos de combate
        turret.SetCombatEnabled(false);

        // Reactivar después de X segundos
        _ctx.Enemy.StartCoroutine(ReenableAfter(turret));
    }

    private IEnumerator ReenableAfter(Turret turret)
    {
        yield return new WaitForSeconds(disableDuration);

        if (turret != null)
        {
            turret.SetCombatEnabled(true);
            _affected.Remove(turret);
        }
    }

    public void OnDamaged(float dmg, float currentHP, object source) { }
    public void OnDeath() { }
    public void OnArrivedToCore() { }
    public void OnWaypoint(int index) { }
    public void ResetRuntime()
    {
        _affected.Clear();
        _timer = empInterval;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
