using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public float maxHealth;
    public float defense;
    public float moveSpeed;
    public int damageToCore;
    public string abilityDescription;

    public EnemyType type;

    [Header("World Phase")]
    [Tooltip("World where this enemy can be targeted and damaged. Neutral means both worlds.")]
    public WorldAffinity vulnerableIn = WorldAffinity.Neutral;
}
