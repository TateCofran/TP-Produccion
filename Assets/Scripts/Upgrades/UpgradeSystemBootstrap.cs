using UnityEngine;

public class UpgradeSystemBootstrap : MonoBehaviour
{
    public static IUpgradeService Service { get; private set; }

    private void Awake()
    {
        if (Service == null)
            Service = new UpgradeService(new PlayerPrefsUpgradeStateStore());
    }
}
