using System.Collections;
using Unity.Cinemachine;

public interface IBlendController
{
    IEnumerator ApplyBlendOnce(CinemachineBrain brain, string blendStyleName, float blendTime, CinemachineBlenderSettings custom, bool restoreAfter);
    bool IsBlending(CinemachineBrain brain);
}