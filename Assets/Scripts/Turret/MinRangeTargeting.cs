using UnityEngine;

public class MinRangeTargeting : TurretTargeting
{
    [Header("Minimum Range")]
    [Tooltip("Enemies closer than this distance are ignored.")]
    [SerializeField, Min(0f)] private float minRange = 2.5f;

    public float MinRange => minRange;

    protected override bool IsValidTarget(Enemy enemy, float sqrDistance)
    {
        return sqrDistance >= minRange * minRange;
    }
}
