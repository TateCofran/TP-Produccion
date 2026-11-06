using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class EssenceChime : MonoBehaviour
{
    public static EssenceChime Instance { get; private set; }

    [Header("Audio")]
    [Tooltip("Optional. A short chime is generated at runtime when empty.")]
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.35f;
    [SerializeField] private AudioMixerGroup outputGroup;
    [SerializeField] private string sfxGroupName = "SFX";

    private AudioSource source;
    private int chain;
    private float lastKillTime = -999f;

    public int Chain => chain;
    public float CurrentPitch => 1f + Mathf.Min(FeedbackSettings.Current.chimeMaxPitchBonus, FeedbackSettings.Current.chimePitchStep * Mathf.Max(0, chain - 1));

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        if (clip == null) clip = BuildChimeClip();
        if (outputGroup == null) outputGroup = FindSfxGroup();
        if (outputGroup != null) source.outputAudioMixerGroup = outputGroup;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void PlayKill()
    {
        if (Instance != null) Instance.Play();
    }

    public void Play()
    {
        var settings = FeedbackSettings.Current;
        float now = Time.unscaledTime;
        chain = now - lastKillTime <= settings.chimeChainWindow ? chain + 1 : 1;
        lastKillTime = now;

        if (source == null || clip == null) return;
        source.pitch = CurrentPitch;
        source.PlayOneShot(clip, volume);
    }

    private AudioMixerGroup FindSfxGroup()
    {
        var sm = SoundManager.instance;
        if (sm == null || sm.masterMixer == null) return null;
        var groups = sm.masterMixer.FindMatchingGroups(sfxGroupName);
        return groups != null && groups.Length > 0 ? groups[0] : null;
    }

    private static AudioClip BuildChimeClip()
    {
        const int rate = 44100;
        const float length = 0.18f;
        int samples = Mathf.CeilToInt(rate * length);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * 22f) * Mathf.Clamp01(t * 400f);
            float tone = Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 1975.5f * t) * 0.3f;
            data[i] = tone * env;
        }
        var c = AudioClip.Create("EssenceChime", samples, 1, rate, false);
        c.SetData(data, 0);
        return c;
    }
}
