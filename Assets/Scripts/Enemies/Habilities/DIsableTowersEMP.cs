using System.Collections.Generic;
using UnityEngine;

public class Ability_DisableTowersEMP : MonoBehaviour, IEnemyAbility
{
    [Header("EMP Settings")]
    [SerializeField, Min(0f)] private float radius = 6f;
    [SerializeField, Min(0.1f)] private float empDuration = 5f;      // tiempo que las torres quedan deshabilitadas
    [SerializeField, Min(0.1f)] private float cooldownSeconds = 15f; // tiempo entre EMPs

    [Header("Deteccion de torres")]
    [SerializeField] private bool useLayerMask = true;
    [SerializeField] private LayerMask towerLayers;      // capa(s) de torres
    [SerializeField] private string towerTag = "Tower";  // tag de torres (puede quedar vacio si solo usas layer)

    [Header("FX (opcional)")]
    [SerializeField] private GameObject empCastVfxPrefab;
    [SerializeField, Min(0f)] private float empCastVfxLifetime = 2f;

    private AbilityContext _ctx;

    private float _cooldownTimer;
    private bool _empActive;
    private float _empTimer;

    // Torres afectadas SOLO en el EMP actual
    private readonly List<GameObject> _disabledThisPulse = new List<GameObject>();

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _cooldownTimer = 0f;
        _empActive = false;
        _empTimer = 0f;
        _disabledThisPulse.Clear();
    }

    public void Tick(float dt)
    {
        if (_empActive)
        {
            // EMP activo, contamos hacia atras
            _empTimer -= dt;
            if (_empTimer <= 0f)
            {
                ReenableAll();
                _empActive = false;
            }
            return;
        }

        // EMP no activo, contamos cooldown
        _cooldownTimer += dt;
        if (_cooldownTimer >= cooldownSeconds)
        {
            _cooldownTimer = 0f;
            CastEMP();
        }
    }

    public void OnDamaged(float dmg, float currentHP, object source)
    {
        // no-op, el EMP se maneja solo con cooldown
    }

    public void OnDeath()
    {
        // Seguridad: si muere con torres apagadas, las reactivamos
        ReenableAll();
    }

    public void OnArrivedToCore()
    {
        ReenableAll();
    }

    public void OnWaypoint(int index)
    {
        // no-op
    }

    public void ResetRuntime()
    {
        ReenableAll();
        _cooldownTimer = 0f;
        _empActive = false;
        _empTimer = 0f;
    }

    // ============================
    // Logica EMP
    // ============================

    private void CastEMP()
    {
        if (_empActive) return;  // por las dudas

        if (_ctx.Transform == null) return;

        _empActive = true;
        _empTimer = empDuration;
        _disabledThisPulse.Clear();

        // Buscar torres en el radio
        Collider[] hits;
        if (useLayerMask)
            hits = Physics.OverlapSphere(_ctx.Transform.position, radius, towerLayers);
        else
            hits = Physics.OverlapSphere(_ctx.Transform.position, radius);

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null) continue;

            GameObject go = col.transform.root.gameObject;

            if (!string.IsNullOrEmpty(towerTag) && !go.CompareTag(towerTag))
                continue;

            // Solo deshabilitamos si esta activa ahora
            if (!go.activeSelf) continue;

            go.SetActive(false);
            _disabledThisPulse.Add(go);
        }

        // FX opcional
        if (empCastVfxPrefab != null)
        {
            var vfx = Object.Instantiate(
                empCastVfxPrefab,
                _ctx.Transform.position,
                Quaternion.identity,
                _ctx.Transform
            );
            Object.Destroy(vfx, empCastVfxLifetime);
        }
    }

    private void ReenableAll()
    {
        if (_disabledThisPulse.Count > 0)
        {
            for (int i = 0; i < _disabledThisPulse.Count; i++)
            {
                var go = _disabledThisPulse[i];
                if (go != null)
                    go.SetActive(true);
            }
        }

        _disabledThisPulse.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
