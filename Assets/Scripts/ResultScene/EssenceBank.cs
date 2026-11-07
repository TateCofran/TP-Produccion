using World = ShiftingWorldMechanic.World;

public static class EssenceBank
{
    public static int TotalBlue => ProgressionStore.GetXp(World.Normal);
    public static int TotalRed => ProgressionStore.GetXp(World.Other);

    public static void Add(int blue, int red)
    {
        ProgressionStore.AddXp(World.Normal, blue);
        ProgressionStore.AddXp(World.Other, red);
    }
}
