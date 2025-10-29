using System.Collections;
using UnityEngine;

public class ShiftingWorldAudio : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip normalWorldClip;
    [SerializeField] private AudioClip otherWorldClip;
    private AudioSource audioSource;
   
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayWorldSound(bool isOtherWorld)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = isOtherWorld ? otherWorldClip : normalWorldClip;
        float volume = 1f;

        StopAllCoroutines(); 
        StartCoroutine(PlayWithFade(clipToPlay, volume));
    }

    private IEnumerator PlayWithFade(AudioClip clip, float targetVolume)
    {
        audioSource.clip = clip;
        audioSource.volume = targetVolume;
        audioSource.Play();

        yield return new WaitForSeconds(clip.length - 0.5f);

        float fadeTime = 0.5f;
        float startVolume = audioSource.volume;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = targetVolume; //reiniciar volumen - que se fue bajando antes
    }
}
