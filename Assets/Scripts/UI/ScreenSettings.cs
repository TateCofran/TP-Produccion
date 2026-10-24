using UnityEngine;
using TMPro;

public class ScreenSettings : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown displayModeDropdown;

    private void Awake()
    {
        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
    }

    private void Start()
    {
        PlayerPrefs.DeleteKey("displayMode");

        displayModeDropdown.value = GetCurrentDisplayModeIndex();
        displayModeDropdown.onValueChanged.AddListener(SetDisplayMode);
    }

    public void SetDisplayMode(int index)
    {
        switch (index)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;

            case 1:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;

            case 2:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
        }

        StartCoroutine(ForceDisplayRefresh());
    }

    private System.Collections.IEnumerator ForceDisplayRefresh()
    {
        yield return null;
        Screen.SetResolution(Screen.width, Screen.height, Screen.fullScreenMode);
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
