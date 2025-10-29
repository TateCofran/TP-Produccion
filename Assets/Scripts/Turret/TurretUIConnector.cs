using UnityEngine;

public class TurretUIConnector : MonoBehaviour
{
    private Turret turret;
    private TurretDataHolder dataHolder;


    void Awake()
    {
        turret = GetComponent<Turret>();


        dataHolder = GetComponent<TurretDataHolder>();
        if (dataHolder == null)
            Debug.LogError("Falta el componente TurretDataHolder en " + gameObject.name);
    }

    public void ShowUI()
    {
        TurretInfoUI.Instance.Initialize(turret);
    }

}
