using Unity.Cinemachine;

public interface ICameraActivator
{
    void SetActive(CinemachineCamera target, int activePriority, int inactivePriority);
}
