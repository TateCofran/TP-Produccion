public enum TutorialEventType
{
    Manual,
    TilePlaced,
    TurretPlaced,
    WaveStarted,
    CoreDamaged,
    WorldSwitched,
    WaveCompleted
}

[System.Serializable]
public class TutorialStep
{
    public string title;
    public string description;
    public TutorialEventType eventType;

    public bool allowTiles;
    public bool allowTurrets;
    public bool allowWave;
    public bool allowWorld;

    public TutorialStep(string t, string d, TutorialEventType evt,
        bool allowTiles = true, bool allowTurrets = true,
        bool allowWave = true, bool allowWorld = true)
    {
        title = t;
        description = d;
        eventType = evt;
        this.allowTiles = allowTiles;
        this.allowTurrets = allowTurrets;
        this.allowWave = allowWave;
        this.allowWorld = allowWorld;
    }
}