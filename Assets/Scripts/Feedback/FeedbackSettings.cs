using UnityEngine;

[CreateAssetMenu(menuName = "Shadow Realms/Feedback Settings", fileName = "FeedbackSettings")]
public class FeedbackSettings : ScriptableObject
{
    public const string ResourcePath = "FeedbackSettings";

    [Header("Camera Shake")]
    [Tooltip("Global multiplier for every camera shake.")]
    [Range(0f, 2f)] public float shakeIntensity = 1f;
    [Tooltip("Maximum position offset of the camera at trauma 1.")]
    [Min(0f)] public float shakeMaxOffset = 0.35f;
    [Tooltip("Maximum rotation offset in degrees at trauma 1.")]
    [Min(0f)] public float shakeMaxAngle = 1.5f;
    [Tooltip("Trauma lost per second.")]
    [Min(0.01f)] public float shakeDecay = 1.6f;
    [Min(0.1f)] public float shakeFrequency = 22f;

    [Header("Slow Motion")]
    public bool slowMotionEnabled = true;
    [Range(0.05f, 1f)] public float bossDeathSlowScale = 0.3f;
    [Min(0f)] public float bossDeathSlowDuration = 0.3f;
    [Range(0.05f, 1f)] public float worldShiftSlowScale = 0.3f;
    [Min(0f)] public float worldShiftSlowDuration = 0.15f;

    [Header("Flash")]
    public bool flashEnabled = true;
    [Tooltip("Reduces flash intensity for players sensitive to flashing lights.")]
    public bool reduceFlashes = false;
    [Range(0f, 1f)] public float reducedFlashMultiplier = 0.35f;

    [Header("Essence Orbs")]
    [Min(1)] public int maxOrbs = 20;
    [Min(0.05f)] public float orbMinDuration = 0.5f;
    [Min(0.05f)] public float orbMaxDuration = 0.7f;
    [Min(0f)] public float orbArcHeight = 140f;
    [Min(1f)] public float counterPunchScale = 1.15f;
    [Min(0.01f)] public float counterPunchDuration = 0.1f;

    [Header("Kill Chime")]
    [Min(0f)] public float chimePitchStep = 0.05f;
    [Min(0f)] public float chimeMaxPitchBonus = 0.5f;
    [Min(0.05f)] public float chimeChainWindow = 1f;

    [Header("Hits")]
    [Min(0f)] public float hitFlashDuration = 0.06f;
    [Min(1f)] public float hitPunchScale = 1.08f;
    [Min(0f)] public float hitPunchDuration = 0.08f;
    [Min(0f)] public float hitMinInterval = 0.05f;

    private static FeedbackSettings current;
    private static FeedbackSettings fallback;

    public static FeedbackSettings Current
    {
        get
        {
            if (current != null) return current;
            current = Resources.Load<FeedbackSettings>(ResourcePath);
            if (current != null) return current;
            if (fallback == null)
            {
                fallback = CreateInstance<FeedbackSettings>();
                fallback.name = "FeedbackSettingsDefault";
                fallback.hideFlags = HideFlags.DontSave;
            }
            return fallback;
        }
    }

    public float FlashMultiplier => !flashEnabled ? 0f : (reduceFlashes ? reducedFlashMultiplier : 1f);
}
