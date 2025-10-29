using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public class CinemachineCameraActivator : MonoBehaviour, ICameraActivator
{
    [SerializeField] private CinemachineCamera[] allCameras;

    public void SetActive(CinemachineCamera target, int activePriority, int inactivePriority)
    {
        if (allCameras == null) return;
        foreach (var cam in allCameras)
        {
            if (!cam) continue;
            cam.Priority = (cam == target) ? activePriority : inactivePriority;
        }
    }
}
