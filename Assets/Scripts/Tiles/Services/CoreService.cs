using UnityEngine;

public sealed class CoreService : ICoreService
{
    private readonly GameObject _corePrefab;
    private readonly Transform _root;
    private GameObject _coreInstance;

    public bool HasCore => _coreInstance != null;
    public Vector3 Position => _coreInstance ? _coreInstance.transform.position : Vector3.zero;

    public CoreService(GameObject corePrefab, Transform root)
    {
        _corePrefab = corePrefab;
        _root = root;
    }

    public void SpawnCore(Vector3 pos)
    {
        if (_coreInstance != null)
            Object.Destroy(_coreInstance);

        if (_corePrefab == null)
        {
            Debug.LogWarning("[CoreService] corePrefab no asignado.");
            return;
        }

        _coreInstance = Object.Instantiate(_corePrefab, pos, Quaternion.identity, _root);
        _coreInstance.name = "Core";

        // Asegurar setup básico
        if (_coreInstance.GetComponent<Core>() == null)
            _coreInstance.AddComponent<Core>();

        if (_coreInstance.GetComponent<Collider>() == null)
        {
            var col = _coreInstance.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }

        _coreInstance.tag = "Core";
    }

    public void ClearCore()
    {
        if (_coreInstance != null)
            Object.Destroy(_coreInstance);
        _coreInstance = null;
    }
}
