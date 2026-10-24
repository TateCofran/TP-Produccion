using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Mixer")]
    public AudioMixer masterMixer;

    private float currentMusic = 1f;
    private float currentSFX = 1f;
    public AudioSource musicSource;
    public AudioClip menuMusic;
    public AudioClip gameMusic;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            if (musicSource != null) musicSource.Stop();
            Destroy(gameObject);
            return;
        }

        SceneManager.activeSceneChanged += OnSceneChanged;
        ApplyVolumes();
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        if (musicSource == null) return;

        if (newScene.name == "Menu")
            PlayMusic(menuMusic);
        else
            PlayMusic(gameMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    private void LoadVolumes()
    {
        currentMusic = PlayerPrefs.GetFloat("MusicVolume", 1f);
        currentSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    private void SaveVolumes()
    {
        PlayerPrefs.SetFloat("MusicVolume", currentMusic);
        PlayerPrefs.SetFloat("SFXVolume", currentSFX);
        PlayerPrefs.Save();
    }

    private void ApplyVolumes()
    {
        float musicVol = currentMusic > 0 ? Mathf.Log10(currentMusic) * 20f : -80f;
        float sfxVol = currentSFX > 0 ? Mathf.Log10(currentSFX) * 20f : -80f;

        masterMixer.SetFloat("MusicVolume", musicVol);
        masterMixer.SetFloat("SFXVolume", sfxVol);
    }

    public void SetMusic(float value)
    {
        currentMusic = value;
        ApplyVolumes();
    }

    public void SetSFX(float value)
    {
        currentSFX = value;
        ApplyVolumes();
    }

    public float GetMusic() => currentMusic;
    public float GetSFX() => currentSFX;
}
