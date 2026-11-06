using System;
using UnityEngine;

public static class GameTime
{
    private static float speed = 1f;
    private static int pauseRequests;
    private static readonly System.Collections.Generic.Dictionary<int, float> slowRequests = new();
    private static int nextSlowHandle;

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
        slowRequests.Clear();
        Apply();
        OnSpeedChanged?.Invoke(speed);
        if (wasPaused) OnPauseChanged?.Invoke(false);
    }

    public static float SlowFactor
    {
        get
        {
            float factor = 1f;
            foreach (var v in slowRequests.Values) factor = Mathf.Min(factor, v);
            return factor;
        }
    }

    public static int RequestSlowMotion(float scale)
    {
        int handle = ++nextSlowHandle;
        slowRequests[handle] = Mathf.Clamp(scale, 0.01f, 1f);
        Apply();
        return handle;
    }

    public static void ReleaseSlowMotion(int handle)
    {
        if (!slowRequests.Remove(handle)) return;
        Apply();
    }

    private static void Apply()
    {
        Time.timeScale = pauseRequests > 0 ? 0f : speed * SlowFactor;
        AudioListener.pause = pauseRequests > 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        speed = 1f;
        pauseRequests = 0;
        slowRequests.Clear();
        nextSlowHandle = 0;
        OnSpeedChanged = null;
        OnPauseChanged = null;
    }
}
