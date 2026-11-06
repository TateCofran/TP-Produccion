using UnityEngine;

[DisallowMultipleComponent]
public class EnemyDeathFeedback : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyHealth health;

    [Header("VFX")]
    [Tooltip("Death VFX for this enemy's zone (goblin, skeleton or vampire).")]
    [SerializeField] private GameObject deathVfxPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.6f, 0f);
    [SerializeField, Min(0.1f)] private float lifetime = 1.5f;
    [Tooltip("Scale multiplier for big enemies and bosses.")]
    [SerializeField, Min(0.1f)] private float scale = 1f;

    [Header("Shake")]
    [SerializeField, Range(0f, 1f)] private float deathTrauma = 0f;

    public GameObject DeathVfxPrefab => deathVfxPrefab;

    private void Awake()
    {
        if (health == null) health = GetComponent<EnemyHealth>();
        if (health != null) health.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (deathVfxPrefab != null)
        {
            var vfx = VfxPool.Spawn(deathVfxPrefab, transform.position + offset, Quaternion.identity, lifetime);
            if (vfx != null) vfx.transform.localScale = Vector3.one * scale;
        }

        if (deathTrauma > 0f) CameraShake.AddTrauma(deathTrauma);
    }
}
