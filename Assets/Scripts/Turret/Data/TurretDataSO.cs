using UnityEngine;


[CreateAssetMenu(menuName = "TD/Turret Data", fileName = "TurretDataSO_")]
public class TurretDataSO : ScriptableObject
{
    [Header("Identidad")]
    public string id; // opcional

    [Header("Rol / Tipo")]            
    public string type;               // "attack", "aoe", "slow", etc.

    [Header("Stats")]
    public string displayName;
    //public Sprite icon;

    public float damage;
    public float range;
    public float fireRate;
    public float projectileSpeed;

    [Header("Prefab")]
    public GameObject prefab;
}
