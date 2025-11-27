using UnityEngine;

public class ForceDefaultResolution : MonoBehaviour
{
    private void Awake()
    {
        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        Screen.SetResolution(1920, 1080, Screen.fullScreenMode);
    }
}

