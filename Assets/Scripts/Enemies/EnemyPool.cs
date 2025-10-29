using UnityEngine;
using System.Collections.Generic;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance;

    [Header("Prefabs por tipo")]
    [SerializeField] private GameObject minionEnemyPrefab;
    [SerializeField] private GameObject sprintEnemyPrefab;
    [SerializeField] private GameObject bigEnemyPrefab;
    [SerializeField] private GameObject miniBossPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Prefabs por tipo")]
    [SerializeField] private GameObject minionEnemyPrefab2;
    [SerializeField] private GameObject sprintEnemyPrefab2;
    [SerializeField] private GameObject bigEnemyPrefab2;
    [SerializeField] private GameObject miniBossPrefab2;
    [SerializeField] private GameObject bossPrefab2;

    [Header("Pool")]
    [SerializeField] private int initialSizePerType = 10;
    [SerializeField] private Transform poolRoot;

    private readonly Dictionary<EnemyType, Queue<GameObject>> pools = new();
    private readonly Dictionary<EnemyType, int> totalInstantiated = new();
    private readonly Dictionary<EnemyType, GameObject> prefabMap = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!poolRoot)
        {
            var go = new GameObject("~EnemyPool");
            go.transform.SetParent(transform, false);
            poolRoot = go.transform;
        }

        prefabMap[EnemyType.Minion] = minionEnemyPrefab;
        prefabMap[EnemyType.Sprint] = sprintEnemyPrefab;
        prefabMap[EnemyType.Big] = bigEnemyPrefab;
        prefabMap[EnemyType.MiniBoss] = miniBossPrefab;
        prefabMap[EnemyType.Boss] = bossPrefab;

        prefabMap[EnemyType.Minion2] = minionEnemyPrefab2;
        prefabMap[EnemyType.Sprint2] = sprintEnemyPrefab2;
        prefabMap[EnemyType.Big2] = bigEnemyPrefab2;
        prefabMap[EnemyType.MiniBoss2] = miniBossPrefab2;
        prefabMap[EnemyType.Boss2] = bossPrefab2;
        foreach (var kv in prefabMap)
        {
            var type = kv.Key;
            var prefab = kv.Value;
            pools[type] = new Queue<GameObject>();
            totalInstantiated[type] = 0;

            if (!prefab) continue; // puede que no uses todos los tipos
            Prewarm(type, initialSizePerType);
        }
    }

    private void Prewarm(EnemyType type, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = CreateNew(type);
            ReturnEnemy(go); // lo deja inactivo en el pool
        }
    }

    private GameObject CreateNew(EnemyType type)
    {
        if (!prefabMap.TryGetValue(type, out var prefab) || !prefab)
        {
            Debug.LogError($"[EnemyPool] No hay prefab asignado para {type}");
            return null;
        }

        var go = Instantiate(prefab, poolRoot);
        go.name = $"{type}_{totalInstantiated[type]}";
        totalInstantiated[type]++;

        // Asegurar componentes mínimos
        if (!go.TryGetComponent<Enemy>(out _)) go.AddComponent<Enemy>();
        if (!go.TryGetComponent<EnemyHealth>(out _)) go.AddComponent<EnemyHealth>();
        if (!go.TryGetComponent<EnemyHealthBar>(out _)) go.AddComponent<EnemyHealthBar>();

        go.SetActive(false);
        return go;
    }

    private static bool IsDestroyed(GameObject go)
    {
        // En Unity, un objeto destruido compara == null aunque no sea null CLR
        return go == null;
    }

    /// <summary>
    /// Saca un enemigo del pool. Si el slot está destruido, lo reemplaza.
    /// Devuelve un GameObject activo y listo para configurar con Enemy.Init(route)
    /// </summary>
    public GameObject GetEnemy(EnemyType type)
    {
        if (!pools.TryGetValue(type, out var q))
        {
            q = new Queue<GameObject>();
            pools[type] = q;
            totalInstantiated[type] = 0;
        }

        GameObject go = null;

        // Consumir hasta encontrar uno válido
        while (q.Count > 0 && (go == null))
        {
            var candidate = q.Dequeue();
            if (IsDestroyed(candidate))
            {
                // slot inválido; seguimos
                continue;
            }
            go = candidate;
        }

        // Si no había, crear nuevo
        if (go == null)
        {
            go = CreateNew(type);
            if (go == null) return null;
        }

        // Activar y resetear estado
        if (IsDestroyed(go))
        {
            // si justo se destruyó entre chequeo y uso, creamos otro
            go = CreateNew(type);
            if (go == null) return null;
        }

        go.transform.SetParent(null, true);
        if (!go.activeSelf) go.SetActive(true);

        // Reset lógico seguro
        var enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            try { enemy.ResetEnemy(); }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[EnemyPool] ResetEnemy lanzó excepción: {ex.Message}");
                // Recuperación mínima, alineada a tu API actual
                var hb = go.GetComponent<EnemyHealthBar>();
                var h = go.GetComponent<EnemyHealth>();
                if (hb && h)
                {
                    float max = h.GetMaxHealth();
                    float cur = h.GetCurrentHealth();
                    hb.Initialize(go.transform, max);
                    hb.UpdateHealthBar(cur, max);
                }
            }

        }

        return go;
    }

    /// <summary> Devuelve el enemigo al pool (NO destruir). </summary>
    public void ReturnEnemy(GameObject go)
    {
        if (IsDestroyed(go)) return;

        // Volver a estado inactivo y colgarlo bajo el pool
        go.SetActive(false);
        go.transform.SetParent(poolRoot, false);

        var enemy = go.GetComponent<Enemy>();
        var type = enemy ? enemy.Type : EnemyType.Minion; // fallback

        if (!pools.ContainsKey(type))
        {
            pools[type] = new Queue<GameObject>();
            if (!totalInstantiated.ContainsKey(type)) totalInstantiated[type] = 0;
        }
        pools[type].Enqueue(go);
    }

    public void LogPoolStatus()
    {
        foreach (var type in pools.Keys)
        {
            int instantiated = totalInstantiated[type];
            int unused = pools[type].Count;
            int used = instantiated - unused;
            // Debug.Log($"[EnemyPool] {type} → Inst: {instantiated} | Usados: {used} | En pool: {unused}");
        }
    }
}
