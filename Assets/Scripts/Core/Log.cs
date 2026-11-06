using System.Diagnostics;

public static class Log
{
    [Conditional("UNITY_EDITOR")]
    public static void Info(string source, string message)
    {
        UnityEngine.Debug.Log($"[{source}] {message}");
    }

    [Conditional("UNITY_EDITOR")]
    public static void Warn(string source, string message)
    {
        UnityEngine.Debug.LogWarning($"[{source}] {message}");
    }
}
