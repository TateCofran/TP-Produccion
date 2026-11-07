using UnityEngine;

[CreateAssetMenu(menuName = "Progression/Progression Settings")]
public class ProgressionSettingsSO : ScriptableObject
{
    public const string ResourcePath = "ProgressionSettings";

    public UnlockTableSO unlockTable;
    public XpCurveSO xpCurve;
    [Tooltip("Used by the menu tooltips, where there is no SynergyService.")]
    public SynergyTableSO synergyTable;

    [Header("Loadout")]
    [Min(1)] public int minPerColumn = 3;
    [Min(1)] public int maxPerColumn = 5;

    [Header("Fallback tiles")]
    [Tooltip("Tiles added to the options when no loadout tile fits any exit, so the run never gets stuck.")]
    public TileLayout[] fallbackTiles = new TileLayout[0];

    private static ProgressionSettingsSO cached;

    public static ProgressionSettingsSO Instance
    {
        get
        {
            if (cached == null) cached = Resources.Load<ProgressionSettingsSO>(ResourcePath);
            return cached;
        }
    }
}
