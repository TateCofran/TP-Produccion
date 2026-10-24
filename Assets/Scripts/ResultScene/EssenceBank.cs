using UnityEngine;

public static class EssenceBank
{
    private const string BlueKey = "TotalBlueEssences";
    private const string RedKey = "TotalRedEssences";

    public static int TotalBlue => PlayerPrefs.GetInt(BlueKey, 0);
    public static int TotalRed => PlayerPrefs.GetInt(RedKey, 0);

    public static void Add(int blue, int red)
    {
        if (blue > 0) PlayerPrefs.SetInt(BlueKey, TotalBlue + blue);
        if (red > 0) PlayerPrefs.SetInt(RedKey, TotalRed + red);
        PlayerPrefs.Save();
    }

    public static bool TrySpend(int blue, int red)
    {
        if (blue < 0 || red < 0) return false;
        if (TotalBlue < blue || TotalRed < red) return false;

        PlayerPrefs.SetInt(BlueKey, TotalBlue - blue);
        PlayerPrefs.SetInt(RedKey, TotalRed - red);
        PlayerPrefs.Save();
        return true;
    }

    public static void Set(int blue, int red)
    {
        PlayerPrefs.SetInt(BlueKey, Mathf.Max(0, blue));
        PlayerPrefs.SetInt(RedKey, Mathf.Max(0, red));
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(BlueKey);
        PlayerPrefs.DeleteKey(RedKey);
        PlayerPrefs.Save();
    }
}
