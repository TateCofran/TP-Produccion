using System;
using UnityEngine;

public static class GameTime
{
    private static float speed = 1f;
    private static int pauseRequests;

    public static float Speed => speed;
    public static bool IsPaused => pauseRequests > 0;

    public static event Action<float> OnSpeedChanged;
    public static event Action<bool> OnPauseChanged;

    public static void SetSpeed(float value)
    {
        speed = Mathf.Max(0f, value);
        Apply();
        OnSpeedChanged?.Invoke(speed);
    }

    public static void RequestPause()
    {
        pauseRequests++;
        Apply();
        if (pauseRequests == 1) OnPauseChanged?.Invoke(true);
    }

    public static void ReleasePause()
    {
        if (pauseRequests == 0) return;
        pauseRequests--;
        Apply();
        if (pauseRequests == 0) OnPauseChanged?.Invoke(false);
    }

    public static void ResetAll()
    {
        bool wasPaused = pauseRequests > 0;
        speed = 1f;
        pauseRequests = 0;
        Apply();
        OnSpeedChanged?.Invoke(speed);
        if (wasPaused) OnPauseChanged?.Invoke(false);
    }

    private static void Apply()
    {
        Time.timeScale = pauseRequests > 0 ? 0f : speed;
        AudioListener.pause = pauseRequests > 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        speed = 1f;
        pauseRequests = 0;
        OnSpeedChanged = null;
        OnPauseChanged = null;
    }
}
