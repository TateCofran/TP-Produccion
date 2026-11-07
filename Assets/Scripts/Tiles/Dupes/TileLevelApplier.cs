using UnityEngine;

public class TileLevelApplier : MonoBehaviour
{
    [SerializeField] private TileLayout layout;
    [SerializeField] private TileLevelData levelData;

    public TileLayout Layout => layout;

    private TileDupeSystem dupeSystem;
    private bool trackLevel;
    private bool subscribed;

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Initialize(TileLayout tileLayout, bool followLevel = true)
    {
        layout = tileLayout;
        trackLevel = followLevel;

        if (dupeSystem == null) dupeSystem = FindFirstObjectByType<TileDupeSystem>();

        levelData = (trackLevel && dupeSystem != null) ? dupeSystem.GetTileLevelData(layout) : null;

        if (isActiveAndEnabled) Subscribe();
        ApplyLevel();
    }

    private void Subscribe()
    {
        if (subscribed || !trackLevel || dupeSystem == null) return;
        dupeSystem.OnTileLevelUp += HandleTileLevelUp;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || dupeSystem == null) return;
        dupeSystem.OnTileLevelUp -= HandleTileLevelUp;
        subscribed = false;
    }

    private void HandleTileLevelUp(TileLayout leveledLayout, TileLevelData data)
    {
        if (leveledLayout == null || layout == null || leveledLayout != layout.DupeKey) return;
        levelData = data;
        ApplyLevel();
    }

    private void ApplyLevel()
    {
        float dmg = levelData != null ? levelData.damageMultiplier : 1f;
        float slow = levelData != null ? levelData.slowMultiplier : 1f;
        float stun = levelData != null ? levelData.stunMultiplier : 1f;

        var effects = GetComponentsInChildren<PathCellEffect>(true);
        foreach (var effect in effects)
            effect.SetLevelMultipliers(dmg, slow, stun);
    }

    public string GetLevelStatus()
    {
        return levelData?.GetStatusText() ?? "Lv 1";
    }
}
