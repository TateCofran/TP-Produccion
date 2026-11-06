using System.Collections;
using UnityEngine;

public class FeedbackRunner : MonoBehaviour
{
    private static FeedbackRunner instance;

    public static FeedbackRunner Instance
    {
        get
        {
            if (instance != null) return instance;
            if (!Application.isPlaying) return null;
            var go = new GameObject("~FeedbackRunner");
            instance = go.AddComponent<FeedbackRunner>();
            return instance;
        }
    }

    public static Coroutine Run(IEnumerator routine)
    {
        var runner = Instance;
        return runner != null ? runner.StartCoroutine(routine) : null;
    }

    public static void Stop(Coroutine routine)
    {
        if (routine != null && instance != null) instance.StopCoroutine(routine);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
