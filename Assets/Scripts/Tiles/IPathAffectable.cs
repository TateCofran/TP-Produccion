public interface IPathAffectable
{
    void ApplyDoT(float dps, float duration);
    void ApplySlow(float slowPercent, float duration);
    void ApplyStun(float stunSeconds);

    void ApplyInstantDamage(int amount);
}
