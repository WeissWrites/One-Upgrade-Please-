using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    // ---------------------------------------------------------------

    [System.Serializable]
    public class WaveConfig
    {
        public string label = "Wave";
        [Tooltip("How many normal enemies spawn this wave")]
        public int enemyCount = 10;
        [Tooltip("Seconds between each enemy spawn")]
        public float spawnInterval = 1f;
        [Tooltip("Stats multiplied on top of the PREVIOUS wave's stats. Ignored for wave 1.")]
        public float statMultiplier = 1.5f;

        [Tooltip("One or more zombie prefabs — each spawn picks one at random")]
        public GameObject[] enemyPrefabs;

        [Tooltip("One or more boss prefabs — each spawn picks one at random")]
        public GameObject[] bossPrefabs;
        [Tooltip("How many bosses to spawn this wave (0 = none)")]
        public int bossCount = 0;
    }

    // ---------------------------------------------------------------

    [Header("Waves (configure all 5 here)")]
    public WaveConfig[] waves = new WaveConfig[5];

    [Header("Spawn Points")]
    [Tooltip("Enemies pick a random point from this list each spawn")]
    public Transform[] spawnPoints;
    [Tooltip("Boss always spawns here (assign an empty GameObject in the scene)")]
    public Transform bossSpawnPoint;

    [Header("Base Stats — used exactly for Wave 1")]
    public float baseHealth = 100f;
    public float baseDamage = 10f;
    public float baseSpeed  = 3.5f;

    [Header("Flow")]
    [Tooltip("Start wave 1 automatically on Play")]
    public bool autoStart = true;
    [Tooltip("Seconds between the last enemy dying and the next wave starting")]
    public float timeBetweenWaves = 8f;

    // ---------------------------------------------------------------

    private int   currentWave    = 0;
    private int   enemiesAlive   = 0;
    private float currentHealth;
    private float currentDamage;
    private float currentSpeed;
    private bool  waveInProgress;

    void Awake() { Instance = this; }

    void Start()
    {
        currentHealth = baseHealth;
        currentDamage = baseDamage;
        currentSpeed  = baseSpeed;

        if (autoStart)
            Invoke(nameof(StartNextWave), 2f);
    }

    // ---------------------------------------------------------------
    // Public API

    public void StartNextWave()
    {
        if (currentWave >= waves.Length)
        {
            Debug.Log("All waves complete.");
            return;
        }

        currentWave++;
        WaveConfig config = waves[currentWave - 1];

        // Wave 1 uses base stats; every subsequent wave multiplies on top
        if (currentWave > 1)
        {
            currentHealth *= config.statMultiplier;
            currentDamage *= config.statMultiplier;
            currentSpeed  *= config.statMultiplier;
        }

        Debug.Log($"Starting {config.label} — HP:{currentHealth:F0}  DMG:{currentDamage:F1}  SPD:{currentSpeed:F2}");

        waveInProgress = true;
        enemiesAlive   = 0;
        StartCoroutine(SpawnWave(config));
    }

    // Called by Enemy.Die()
    public void OnEnemyDied()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);

        if (enemiesAlive == 0 && waveInProgress)
        {
            waveInProgress = false;
            if (currentWave < waves.Length)
                Invoke(nameof(StartNextWave), timeBetweenWaves);
            else
                Debug.Log("All waves cleared!");
        }
    }

    // ---------------------------------------------------------------

    private IEnumerator SpawnWave(WaveConfig config)
    {
        for (int i = 0; i < config.bossCount; i++)
            SpawnBoss(PickRandom(config.bossPrefabs));

        for (int i = 0; i < config.enemyCount; i++)
        {
            SpawnOne(PickRandom(config.enemyPrefabs));
            yield return new WaitForSeconds(config.spawnInterval);
        }
    }

    private void SpawnOne(GameObject prefab)
    {
        if (prefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject go = Instantiate(prefab, sp.position, sp.rotation);

        Enemy e = go.GetComponent<Enemy>();
        if (e != null)
        {
            e.SetRuntimeStats(currentHealth, currentDamage, currentSpeed);
            enemiesAlive++;
        }
    }

    private void SpawnBoss(GameObject prefab)
    {
        if (prefab == null) return;

        Transform sp = bossSpawnPoint != null ? bossSpawnPoint : (spawnPoints?.Length > 0 ? spawnPoints[0] : transform);
        Instantiate(prefab, sp.position, sp.rotation);
    }

    private static GameObject PickRandom(GameObject[] arr) =>
        (arr != null && arr.Length > 0) ? arr[Random.Range(0, arr.Length)] : null;
}
