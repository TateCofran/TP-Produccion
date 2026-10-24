using UnityEngine;
using System.Collections.Generic;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int initialSize = 30;

    private Queue<GameObject> projectilePool = new Queue<GameObject>();
    private int totalInstantiated = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;

        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < initialSize; i++)
        {
            GameObject projectile = Instantiate(projectilePrefab);
            projectile.transform.SetParent(this.transform);
            projectile.SetActive(false);
            projectilePool.Enqueue(projectile);
            totalInstantiated++;
        }
    }

    public GameObject GetProjectile()
    {
        if (projectilePool.Count == 0)
        {
            GameObject projectile = Instantiate(projectilePrefab);
            projectile.transform.SetParent(this.transform);
            projectile.SetActive(false);
            projectilePool.Enqueue(projectile);
            totalInstantiated++;
        }

        GameObject proj = projectilePool.Dequeue();
        proj.SetActive(true);

        return proj;
    }

    public void ReturnProjectile(GameObject projectile)
    {
        projectile.SetActive(false);
        projectile.transform.SetParent(this.transform);
        projectilePool.Enqueue(projectile);
    }

    public void LogPoolStatus()
    {
        int used = totalInstantiated - projectilePool.Count;
    }
}
