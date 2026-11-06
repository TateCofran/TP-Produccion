using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class WorldShiftPostFX : MonoBehaviour
{
    public static WorldShiftPostFX Instance { get; private set; }

    [Header("Volume")]
    [Tooltip("Optional. A global volume with Chromatic Aberration and Lens Distortion is created at runtime when empty.")]
    [SerializeField] private Volume volume;
    [SerializeField] private float volumePriority = 50f;

    [Header("Pulse")]
    [SerializeField, Min(0.01f)] private float duration = 0.2f;
    [SerializeField, Range(0f, 1f)] private float chromaticIntensity = 1f;
    [SerializeField, Range(-1f, 1f)] private float distortionIntensity = -0.25f;

    private Coroutine routine;
    private VolumeProfile runtimeProfile;

    public float CurrentWeight => volume != null ? volume.weight : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        EnsureVolume();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (runtimeProfile != null) Destroy(runtimeProfile);
    }

    public static void Pulse()
    {
        if (Instance != null) Instance.Play();
    }

    public void Play()
    {
        if (volume == null) return;
        float mult = FeedbackSettings.Current.FlashMultiplier;
        if (mult <= 0f) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PulseRoutine(mult));
    }

    private IEnumerator PulseRoutine(float mult)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            volume.weight = Mathf.Sin(k * Mathf.PI) * mult;
            yield return null;
        }
        volume.weight = 0f;
        routine = null;
    }

    private void EnsureVolume()
    {
        if (volume == null)
        {
            var go = new GameObject("WorldShiftVolume");
            go.transform.SetParent(transform, false);
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = volumePriority;
        }

        if (volume.sharedProfile == null)
        {
            runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            runtimeProfile.name = "WorldShiftProfile";
            var ca = runtimeProfile.Add<ChromaticAberration>(true);
            ca.intensity.Override(chromaticIntensity);
            var ld = runtimeProfile.Add<LensDistortion>(true);
            ld.intensity.Override(distortionIntensity);
            volume.sharedProfile = runtimeProfile;
        }

        volume.weight = 0f;
    }
}
