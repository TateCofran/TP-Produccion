using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Start()
    {
        musicSlider.value = SoundManager.instance.GetMusic();
        sfxSlider.value = SoundManager.instance.GetSFX();

        // Limpiar listeners para evitar duplicados
        musicSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();


        musicSlider.onValueChanged.AddListener(volume => SoundManager.instance.SetMusic(volume));
        sfxSlider.onValueChanged.AddListener(volume => SoundManager.instance.SetSFX(volume));
    }
}

