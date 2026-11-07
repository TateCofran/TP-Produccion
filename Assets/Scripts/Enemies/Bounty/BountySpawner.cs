using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BountySpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GridGenerator grid;
    [SerializeField] private RewardZoneSystem rewards;

    [Header("Rules")]
    [SerializeField, Min(1)] private int firstWave = 4;
    [SerializeField] private List<int> bossWaves = new List<int> { 15, 30, 45 };
    [SerializeField, Range(0f, 1f)] private float chancePerWave = 0.15f;
    [Tooltip("Seconds after the wave starts before the bounty appears.")]
    [SerializeField, Min(0f)] private float spawnDelay = 8f;
    [SerializeField, Min(1)] private int wavesPerZone = 15;

    [Header("Audio")]
    [SerializeField] private AudioClip spottedClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;

    private WaveManager waves;
    private AudioSource source;
    private Coroutine pending;

    public Enemy LastBounty { get; private set; }

    private void Awake()
    {
        if (grid == null) grid = FindFirstObjectByType<GridGenerator>();
        if (rewards == null) rewards = FindFirstObjectByType<RewardZoneSystem>();
        source = GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }
        if (spottedClip == null) spottedClip = RewardClaimFX.BuildTone(880f);
    }

    private void Start()
    {
        waves = WaveManager.Instance;
        if (waves != null) waves.OnWaveStarted += HandleWaveStarted;
    }

    private void OnDestroy()
    {
        if (waves != null) waves.OnWaveStarted -= HandleWaveStarted;
    }

    private void HandleWaveStarted(int wave, int enemies)
    {
        if (wave < firstWave || bossWaves.Contains(wave)) return;
        if (Random.value >= chancePerWave) return;
        if (pending != null) StopCoroutine(pending);
        pending = StartCoroutine(SpawnLater(wave));
    }

    private IEnumerator SpawnLater(int wave)
    {
        if (spawnDelay > 0f) yield return new WaitForSeconds(spawnDelay);
        pending = null;
        if (waves != null && !waves.WaveInProgress) yield break;
        SpawnBounty(wave);
    }

    public Enemy SpawnBounty(int wave)
    {
        if (EnemyPool.Instance == null || grid == null) return null;

        EnemyType type = SprintForZone(wave);
        Vector3 exit = grid.GetNextSpawnRoundRobin();
        if (exit == Vector3.zero) return null;
        if (!grid.TryGetRouteExitToCore(exit, out var route) || route == null || route.Count == 0) return null;

        var go = EnemyPool.Instance.GetEnemy(type);
        if (go == null) return null;
        go.transform.position = route[0];
        var enemy = go.GetComponent<Enemy>();
        if (enemy == null) return null;

        enemy.Init(route);
        waves?.RegisterExtraEnemies(1);

        var marker = go.GetComponent<BountyMarker>();
        if (marker == null) marker = go.AddComponent<BountyMarker>();
        marker.Activate();
        LastBounty = enemy;

        if (rewards != null) rewards.Toast("Bounty spotted!");
        if (source != null && spottedClip != null) source.PlayOneShot(spottedClip, volume);
        Log.Info(nameof(BountySpawner), $"Bounty spawned on wave {wave} ({type})");
        return enemy;
    }

    private EnemyType SprintForZone(int wave)
    {
        int zone = Mathf.Max(1, (Mathf.Max(1, wave) - 1) / wavesPerZone + 1);
        return zone >= 3 ? EnemyType.Sprint3 : zone == 2 ? EnemyType.Sprint2 : EnemyType.Sprint;
    }
}
