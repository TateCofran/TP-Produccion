using System.Collections.Generic;
using UnityEngine;

public interface IEnemyAbility
{
    void Initialize(AbilityContext ctx);          // Se llama una vez al spawnear
    void Tick(float deltaTime);                   // Se llama cada frame
    void OnDamaged(float damage, float currentHP, object source);
    void OnDeath();
    void OnArrivedToCore();
    void OnWaypoint(int index);
    void ResetRuntime();                          // Para limpiar antes de devolver al pool
}

public struct AbilityContext
{
    public Enemy Enemy;
    public EnemyHealth Health;
    public EnemyEffects Effects;
    public Transform Transform;
    public System.Func<IReadOnlyList<Vector3>> GetRoute;
    public System.Func<int> GetWaypointIndex;
}
