using UnityEngine;

public class PriorityTargeting : TurretTargeting
{
    public enum Priority { None, MostAdvanced, Shielded, HighestMaxHealth }

    [Header("Priority")]
    [SerializeField] private Priority priority = Priority.None;

    public Priority CurrentPriority => priority;

    protected override float PriorityScore(Enemy enemy)
    {
        switch (priority)
        {
            case Priority.MostAdvanced:
                return enemy.RouteProgress / 1000f;
            case Priority.Shielded:
                return enemy.Health != null && enemy.Health.HasShield ? 1f : 0f;
            case Priority.HighestMaxHealth:
                return enemy.Health != null ? enemy.Health.GetMaxHealth() / 10000f : 0f;
            default:
                return 0f;
        }
    }
}
