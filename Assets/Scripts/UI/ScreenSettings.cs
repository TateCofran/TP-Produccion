using UnityEngine;
using TMPro;

public class ScreenSettings : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown displayModeDropdown;

    private void Start()
    {
        PlayerPrefs.DeleteKey("displayMode");
        // Cargar el modo actual del sistema en el dropdown
        displayModeDropdown.value = GetCurrentDisplayModeIndex();
        displayModeDropdown.onValueChanged.AddListener(SetDisplayMode);     
    }

    public void SetDisplayMode(int index)
    {
        switch (index)
        {
            case 0: // Fullscreen
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;

            case 1: // Borderless
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;

            case 2: // Windowed
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
        }

        // Fuerza refresh incluso en pausa
        //Screen.fullScreen = !Screen.fullScreen;
        //Screen.fullScreen = !Screen.fullScreen;

        PlayerPrefs.SetInt("displayMode", index);
    }

    private int GetCurrentDisplayModeIndex()
    {
        if (PlayerPrefs.HasKey("displayMode"))
            return PlayerPrefs.GetInt("displayMode");

        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen: return 0;
            case FullScreenMode.FullScreenWindow: return 1;
            case FullScreenMode.Windowed: return 2;
            default: return 0;
        }
    }
}


