using UnityEngine;

public static class TutorialProgress
{
    private const string KeyCompleted = "Tutorial_Completed";

    public static bool IsCompleted
    {
        get => PlayerPrefs.GetInt(KeyCompleted, 0) == 1;
    }

    public static void MarkCompleted()
    {
        PlayerPrefs.SetInt(KeyCompleted, 1);
        PlayerPrefs.Save();
    }
    public static bool ForceTutorialThisRun { get; set; } = false;
}
