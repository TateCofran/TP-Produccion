public enum ModifierCategory { NormalWorld, OtherWorld, ShiftWorld, LegendaryNormal, LegendaryOther }

public interface IGameModifier
{
    string Name { get; }
    string Description { get; }
    ModifierCategory Category { get; }

    void Apply(GameModifiersManager manager);
    string GetStackDescription(int stacks);
}
