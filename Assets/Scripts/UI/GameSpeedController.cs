using UnityEngine;

public class GameSpeedController : MonoBehaviour
{
    public void SetSpeedTo1()
    {
        Time.timeScale = 1f;
    }

    public void SetSpeedTo2()
    {
        Time.timeScale = 2f;
    }

    public void SetSpeedTo3()
    {
        Time.timeScale = 3f;
    }
}
