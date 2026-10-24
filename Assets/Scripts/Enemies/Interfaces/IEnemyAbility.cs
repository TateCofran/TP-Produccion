using System.Collections.Generic;
using UnityEngine;

public interface IEnemyAbility
{
    void Initialize(AbilityContext ctx);
    void Tick(float deltaTime);
    void OnDamaged(float damage, float currentHP, object source);
    void OnDeath();
    void OnArrivedToCore();
    void OnWaypoint(int index);
    void ResetRuntime();
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
