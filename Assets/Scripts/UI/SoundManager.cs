using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{

    public static SoundManager instance;


    [Header("Mixer")]
    public AudioMixer masterMixer;

    private float currentMusic = 1f;
    private float currentSFX = 1f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject); // opcional
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        masterMixer.SetFloat("MusicVolume", Mathf.Log10(currentMusic) * 20f);
        masterMixer.SetFloat("SFXVolume", Mathf.Log10(currentSFX) * 20f);
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

    //lets the UI read the current values
    public float GetMusic() => currentMusic;
    public float GetSFX() => currentSFX;
}
