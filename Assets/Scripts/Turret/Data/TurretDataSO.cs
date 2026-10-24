using UnityEngine;

[CreateAssetMenu(menuName = "TD/Turret Data", fileName = "TurretDataSO_")]
public class TurretDataSO : ScriptableObject
{
    [Header("Identity")]
    public string id;

    [Header("Role / Type")]
    public string type;

    [Header("Stats")]
    public string displayName;

    public float damage;
    public float range;
    public float fireRate;
    public float projectileSpeed;

    [Header("World Affinity")]
    [Tooltip("World where this turret is stronger. Neutral turrets are not affected.")]
    public WorldAffinity affinity = WorldAffinity.Neutral;
    [Range(0f, 1f)] public float affinityBonus = 0.3f;
    [Range(0f, 1f)] public float offWorldPenalty = 0.2f;

    [Header("Prefab")]
    public GameObject prefab;
}
