using UnityEngine;

public interface ITargetingBehavior
{
    Transform GetTarget(Vector3 origin, float range);
}
