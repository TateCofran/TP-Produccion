public interface IDamageMultiplierProvider
{
    float GetDamageMultiplier(Enemy target);
}

public interface ITurretInfoProvider
{
    string GetInfoLine();
}
