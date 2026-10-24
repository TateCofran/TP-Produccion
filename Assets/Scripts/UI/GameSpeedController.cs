using UnityEngine;

public class GameSpeedController : MonoBehaviour
{
    public void SetSpeedTo1() => GameTime.SetSpeed(1f);
    public void SetSpeedTo2() => GameTime.SetSpeed(2f);
    public void SetSpeedTo3() => GameTime.SetSpeed(3f);
}
