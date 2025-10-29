using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager de pooling para tiles (Grass, Path, etc.).
/// Cumple con la interfaz ITilePoolManager utilizada por el LayoutInstantiator.
/// </summary>
[DisallowMultipleComponent]
public sealed class TilePoolManager : MonoBehaviour, ITilePoolManager
{
    [Header("Object Pooling")]
    [SerializeField] private int initialPoolSize = 100;
    [SerializeField] private bool useObjectPooling = true;

    private readonly Dictionary<GameObject, Queue<GameObject>> _objectPools = new();

    public void InitializePools()
    {
        if (!useObjectPooling) return;

        // Ya no dependemos directamente de GridGenerator (principio DIP)
        // => Prefabs se pueden registrar manualmente o a través de un inicializador externo.
    }

    /// <summary>
    /// Inicializa un pool manualmente (puede llamarse desde GridGenerator o desde otro bootstrap).
    /// </summary>
    public void InitializePool(GameObject prefab, int size)
    {
        if (prefab == null || _objectPools.ContainsKey(prefab)) return;

        var queue = new Queue<GameObject>();
        for (int i = 0; i < size; i++)
        {
            var obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            queue.Enqueue(obj);
        }

        _objectPools[prefab] = queue;
    }

    public GameObject GetPooledObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (!useObjectPooling || !_objectPools.TryGetValue(prefab, out var pool) || pool.Count == 0)
        {
            return Instantiate(prefab, position, rotation, parent);
        }

        var obj = pool.Dequeue();
        obj.transform.SetParent(parent);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    public void ReturnToPool(GameObject prefab, GameObject obj)
    {
        if (!useObjectPooling || !_objectPools.ContainsKey(prefab))
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        _objectPools[prefab].Enqueue(obj);
    }
}
