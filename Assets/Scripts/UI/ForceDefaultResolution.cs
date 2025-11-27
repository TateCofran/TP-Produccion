using UnityEngine;

public class ForceDefaultResolution : MonoBehaviour
{
    private void Awake()
    {
        Screen.SetResolution(1920, 1080, Screen.fullScreenMode);
    }
}

