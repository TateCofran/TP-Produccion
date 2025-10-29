public interface IPathAffectable
{
    // EXISTENTES (podés dejarlos aunque ya no uses DoT)
    void ApplyDoT(float dps, float duration);
    void ApplySlow(float slowPercent, float duration);
    void ApplyStun(float stunSeconds);

    // NUEVO: daño instantáneo
    void ApplyInstantDamage(int amount);
}
