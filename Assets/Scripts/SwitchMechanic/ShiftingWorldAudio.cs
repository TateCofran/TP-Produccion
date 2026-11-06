using System.Collections;
using UnityEngine;

public class ShiftingWorldAudio : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip normalWorldClip;
    [SerializeField] private AudioClip otherWorldClip;

    [Header("Crossfade")]
    [Tooltip("Unscaled seconds of the crossfade between the two world sounds.")]
    [SerializeField, Min(0.01f)] private float crossfadeDuration = 0.5f;
    [SerializeField, Range(0f, 1f)] private float targetVolume = 1f;
    [SerializeField, Min(0.05f)] private float endFadeDuration = 0.5f;

    [Header("Low Pass Sweep")]
    [SerializeField] private bool useLowPass = true;
    [SerializeField, Min(10f)] private float lowPassStartCutoff = 900f;
    [SerializeField, Min(10f)] private float lowPassEndCutoff = 22000f;

    [Header("Unavailable")]
    [Tooltip("Played when the switch key is pressed during the cooldown. A short buzz is generated when empty.")]
    [SerializeField] private AudioClip unavailableClip;
    [SerializeField, Range(0f, 1f)] private float unavailableVolume = 0.5f;

    private AudioSource audioSource;
    private AudioSource secondarySource;
    private AudioSource oneShotSource;
    private AudioLowPassFilter lowPass;
    private Coroutine crossfadeRoutine;
    private bool primaryIsCurrent = true;

    public float LowPassCutoff => lowPass != null ? lowPass.cutoffFrequency : lowPassEndCutoff;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) return;

        secondarySource = gameObject.AddComponent<AudioSource>();
        CopySettings(audioSource, secondarySource);

        oneShotSource = gameObject.AddComponent<AudioSource>();
        CopySettings(audioSource, oneShotSource);
        oneShotSource.loop = false;

        if (useLowPass)
        {
            lowPass = GetComponent<AudioLowPassFilter>();
            if (lowPass == null) lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            lowPass.cutoffFrequency = lowPassEndCutoff;
        }

        if (unavailableClip == null) unavailableClip = BuildBuzz();
    }

    private static void CopySettings(AudioSource from, AudioSource to)
    {
        to.playOnAwake = false;
        to.outputAudioMixerGroup = from.outputAudioMixerGroup;
        to.spatialBlend = from.spatialBlend;
        to.loop = from.loop;
        to.priority = from.priority;
        to.volume = 0f;
    }

    public void PlayWorldSound(bool isOtherWorld)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = isOtherWorld ? otherWorldClip : normalWorldClip;
        if (clipToPlay == null) return;

        if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
        crossfadeRoutine = StartCoroutine(Crossfade(clipToPlay));
    }

    public void PlayUnavailable()
    {
        var src = oneShotSource != null ? oneShotSource : audioSource;
        if (src == null || unavailableClip == null) return;
        src.PlayOneShot(unavailableClip, unavailableVolume);
    }

    private IEnumerator Crossfade(AudioClip clip)
    {
        var outgoing = primaryIsCurrent ? audioSource : secondarySource;
        var incoming = primaryIsCurrent ? secondarySource : audioSource;
        if (incoming == null) incoming = audioSource;
        primaryIsCurrent = incoming == audioSource;

        float outgoingStart = outgoing != null && outgoing != incoming ? outgoing.volume : 0f;
        incoming.clip = clip;
        incoming.volume = 0f;
        incoming.Play();

        float t = 0f;
        while (t < crossfadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / crossfadeDuration);
            incoming.volume = Mathf.Lerp(0f, targetVolume, k);
            if (outgoing != null && outgoing != incoming) outgoing.volume = Mathf.Lerp(outgoingStart, 0f, k);
            if (lowPass != null) lowPass.cutoffFrequency = Mathf.Lerp(lowPassStartCutoff, lowPassEndCutoff, k * k);
            yield return null;
        }

        incoming.volume = targetVolume;
        if (outgoing != null && outgoing != incoming)
        {
            outgoing.Stop();
            outgoing.volume = 0f;
        }
        if (lowPass != null) lowPass.cutoffFrequency = lowPassEndCutoff;

        float remaining = clip.length - crossfadeDuration - endFadeDuration;
        while (remaining > 0f && incoming.isPlaying)
        {
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        float fade = 0f;
        float start = incoming.volume;
        while (fade < endFadeDuration && incoming.isPlaying)
        {
            fade += Time.unscaledDeltaTime;
            incoming.volume = Mathf.Lerp(start, 0f, fade / endFadeDuration);
            yield return null;
        }

        incoming.Stop();
        incoming.volume = 0f;
        crossfadeRoutine = null;
    }

    private static AudioClip BuildBuzz()
    {
        const int rate = 44100;
        const float length = 0.16f;
        int samples = Mathf.CeilToInt(rate * length);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Clamp01(t * 300f) * Mathf.Clamp01((length - t) * 40f);
            float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 140f * t));
            data[i] = square * 0.25f * env;
        }
        var clip = AudioClip.Create("WorldSwitchUnavailable", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
