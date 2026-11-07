using UnityEngine;

[CreateAssetMenu(menuName = "Grid/Tile Icon Palette", fileName = "TileIconPalette")]
public class TileIconPalette : ScriptableObject
{
    [Header("Cells")]
    public Color grass = new Color(0.22f, 0.27f, 0.22f, 1f);
    public Color pathBasic = new Color(0.78f, 0.75f, 0.68f, 1f);
    public Color pathDamage = new Color(0.85f, 0.2f, 0.2f, 1f);
    public Color pathSlow = new Color(0.45f, 0.8f, 1f, 1f);
    public Color pathStun = new Color(1f, 0.85f, 0.2f, 1f);
    public Color pathExposure = new Color(0.65f, 0.35f, 0.95f, 1f);
    public Color pathAntiHeal = new Color(0.12f, 0.38f, 0.16f, 1f);

    [Header("World Lock Marks")]
    public Color normalWorldMark = new Color(0.3f, 0.6f, 1f, 1f);
    public Color otherWorldMark = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Dead End")]
    public Color deadEndMark = new Color(1f, 0.25f, 0.25f, 1f);

    [Header("Markers")]
    public Color marker = Color.white;

    [Header("Ghost Tints")]
    public Color ghostBasic = new Color(0.8f, 0.9f, 1f, 1f);
    public Color ghostGrass = new Color(0.5f, 0.65f, 0.55f, 1f);

    public Color GetPathColor(TileLayout.PathModifiers mods)
    {
        if (mods.exposure) return pathExposure;
        if (mods.antiHeal) return pathAntiHeal;
        if (mods.stun > 0f) return pathStun;
        if (mods.slow > 0f) return pathSlow;
        if (mods.dps > 0f) return pathDamage;
        return pathBasic;
    }

    public Color GetGhostTint(bool isPath, TileLayout.PathModifiers mods)
    {
        if (!isPath) return ghostGrass;
        if (mods.HasAnyEffect) return GetPathColor(mods);
        return ghostBasic;
    }
}
