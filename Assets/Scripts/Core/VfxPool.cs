using System.Collections.Generic;
using UnityEngine;

public static class VfxPool
{
    private static readonly Dictionary<GameObject, Stack<PooledVfx>> pools = new();
    private static Transform root;

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime, Transform follow = null)
    {
        if (prefab == null) return null;
        var item = Get(prefab);
        item.Activate(position, rotation, lifetime, follow);
        return item.gameObject;
    }

    public static void Release(GameObject instance)
    {
        if (instance == null) return;

        if (instance.TryGetComponent<PooledVfx>(out var item))
            item.Release();
        else
            Object.Destroy(instance);
    }

    internal static void Return(PooledVfx item)
    {
        if (item == null || item.Prefab == null) return;

        if (!pools.TryGetValue(item.Prefab, out var stack))
        {
            stack = new Stack<PooledVfx>();
            pools[item.Prefab] = stack;
        }
        stack.Push(item);
    }

    private static PooledVfx Get(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out var stack))
        {
            while (stack.Count > 0)
            {
                var pooled = stack.Pop();
                if (pooled != null) return pooled;
            }
        }

        var go = Object.Instantiate(prefab, Root);
        go.SetActive(false);
        var item = go.AddComponent<PooledVfx>();
        item.Init(prefab);
        return item;
    }

    private static Transform Root
    {
        get
        {
            if (root == null)
                root = new GameObject("~VfxPool").transform;
            return root;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        pools.Clear();
        root = null;
    }
}
