using System.Collections;
using UnityEngine;

public static class SlowMotion
{
    public static void Play(float scale, float duration)
    {
        var settings = FeedbackSettings.Current;
        if (!settings.slowMotionEnabled || duration <= 0f || scale >= 1f) return;
        FeedbackRunner.Run(Routine(scale, duration));
    }

    private static IEnumerator Routine(float scale, float duration)
    {
        int handle = GameTime.RequestSlowMotion(scale);
        try
        {
            yield return new WaitForSecondsRealtime(duration);
        }
        finally
        {
            GameTime.ReleaseSlowMotion(handle);
        }
    }
}
