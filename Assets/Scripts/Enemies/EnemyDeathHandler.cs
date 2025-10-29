using UnityEngine;

public class EnemyDeathHandler : MonoBehaviour, IEnemyDeathHandler
{
    public void OnEnemyDeath(Enemy enemy)
    {
        if (!enemy) return;
        // Centralizamos la lógica en Enemy:
        enemy.NotifyDeath();
    }
}
