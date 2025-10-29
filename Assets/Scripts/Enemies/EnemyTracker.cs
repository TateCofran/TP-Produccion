using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EnemyTracker
{
    private static List<Enemy> enemies = new();

    public static IReadOnlyList<Enemy> Enemies => enemies;

    public static event System.Action<EnemyType> OnEnemyKilled;

    public static void RegisterEnemy(Enemy enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public static void UnregisterEnemy(Enemy enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
            //Debug.Log($"[EnemyTracker] Enemy removido: {enemy.name} - Restantes: {enemies.Count}");
        }
        else
        {
            Debug.LogWarning($"[EnemyTracker] Intento de remover enemigo no registrado: {enemy?.name}");
        }
    }

    public static int GetEnemiesInRangeNonAlloc(Vector3 center, float radius, List<Enemy> buffer)
    {
        if (buffer == null) return 0;
        buffer.Clear();

        float r2 = radius * radius;

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null) continue;
            if (!e.gameObject.activeInHierarchy) continue;
            if (e.Health == null || e.Health.IsDead()) continue;

            if ((e.transform.position - center).sqrMagnitude <= r2)
                buffer.Add(e);
        }

        return buffer.Count;
    }

    public static int CountEnemiesOfType(EnemyType type)
    {
        return enemies.Count(e =>
            e != null &&
            e.gameObject.activeInHierarchy &&
            !e.Health.IsDead() &&
            e.Type == type);
    }

    public static int CountEnemies()
    {
        return enemies.Count(e =>
            e != null &&
            e.gameObject.activeInHierarchy &&
            !e.Health.IsDead());
    }
    public static int CountAllEnemies() => enemies.Count;

    public static List<Enemy> GetActiveEnemies()
    {
        return enemies
            .Where(e =>
                e != null &&
                e.gameObject.activeInHierarchy &&
                !e.Health.IsDead())
            .ToList();
    }

    public static void NotifyEnemyKilledGlobal(Enemy enemy)
    {
        if (enemy != null)
        {
            OnEnemyKilled?.Invoke(enemy.Type);
        }
    }
}
