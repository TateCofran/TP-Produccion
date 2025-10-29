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
            projectile.transform.SetParent(this.transform); // agrupado en Projectile Pool
            projectile.SetActive(false);
            projectilePool.Enqueue(projectile);
            totalInstantiated++;
        }

        //Debug.Log($"Inicializado pool de proyectiles con {initialSize} proyectiles.");
    }

    public GameObject GetProjectile()
    {
        if (projectilePool.Count == 0)
        {
            GameObject projectile = Instantiate(projectilePrefab);
            projectile.transform.SetParent(this.transform);
            projectile.SetActive(false);
            projectilePool.Enqueue(projectile); // importante: agregar al pool
            totalInstantiated++;

            //Debug.Log($"Pool vacío. Instanciando nuevo proyectil. Total instanciados: {totalInstantiated}");
        }

        GameObject proj = projectilePool.Dequeue();
        proj.SetActive(true);
        //Debug.Log($"Proyectil solicitado. Usados: {totalInstantiated - projectilePool.Count}, Sin usar: {projectilePool.Count}");
        return proj;
    }

    public void ReturnProjectile(GameObject projectile)
    {
        projectile.SetActive(false);
        projectile.transform.SetParent(this.transform);
        projectilePool.Enqueue(projectile);
        //Debug.Log($"Proyectil regresado al pool. Sin usar ahora: {projectilePool.Count}");
    }

    public void LogPoolStatus()
    {
        int used = totalInstantiated - projectilePool.Count;
        //Debug.Log($"Estado Pool de Proyectiles: Total instanciados: {totalInstantiated}, Usados: {used}, Sin usar: {projectilePool.Count}");
    }
}
