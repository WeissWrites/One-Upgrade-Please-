using System.Collections;
using UnityEngine;

// Place on a persistent scene GameObject.
// Drives the full inter-wave shopkeeper flow:
//   triangle sinks → stall rises → player confined → shop → shopkeeper killed + map done → next wave
public class ShopkeeperSequence : MonoBehaviour
{
    public static ShopkeeperSequence Instance { get; private set; }

    // ── Scene refs ────────────────────────────────────────────────────
    [Header("Triangle")]
    [Tooltip("The triangle object floating above the boss spawn point")]
    public Transform triangleObject;
    [Tooltip("World position the triangle sinks to before disappearing (usually ground level)")]
    public Vector3 triangleSinkTarget = new Vector3(0, -1f, 0);
    public float triangleSinkSpeed = 2f;

    [Header("Stall")]
    [Tooltip("The shopkeeper stall prefab — spawned at (0, 0, 0)")]
    public GameObject stallPrefab;
    public float stallRiseSpeed = 2f;

    [Header("Confinement")]
    [Tooltip("Horizontal distance from world origin at which the player gets locked in")]
    public float confinementRadius = 15f;

    // ── State (read by MapManager and other systems) ──────────────────
    [HideInInspector] public bool isMapFinished   = false;
    [HideInInspector] public bool isShopkeeperAlive = true;

    // ── Private ───────────────────────────────────────────────────────
    private GameObject spawnedStall;
    private PlayerMovement playerMovement;

    void Awake() { Instance = this; }

    void Start()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();
    }

    // ── Entry point (called by WaveManager) ───────────────────────────

    public void BeginShopPhase()
    {
        isMapFinished    = false;
        isShopkeeperAlive = true;
        StartCoroutine(ShopSequence());
    }

    // ── Called by ShopkeeperNPC when the shopkeeper dies ─────────────

    public void OnShopkeeperKilled()
    {
        isShopkeeperAlive = false;
        TryEndPhase();
    }

    // Called by MapManager.MarkMapFinished()
    // (isMapFinished is set directly by MapManager, then it calls this)
    public void OnMapFinished()
    {
        TryEndPhase();
    }

    private void TryEndPhase()
    {
        if (isMapFinished && !isShopkeeperAlive)
            StartCoroutine(EndPhase());
    }

    // ── Main coroutine ────────────────────────────────────────────────

    private IEnumerator ShopSequence()
    {
        // 1. Sink triangle into the ground
        if (triangleObject != null)
        {
            while (Vector3.Distance(triangleObject.position, triangleSinkTarget) > 0.05f)
            {
                float step = triangleSinkSpeed * Time.deltaTime;
                triangleObject.position   = Vector3.MoveTowards(triangleObject.position, triangleSinkTarget, step);
                triangleObject.localScale = Vector3.MoveTowards(triangleObject.localScale, Vector3.zero, step * 0.3f);
                yield return null;
            }
            triangleObject.gameObject.SetActive(false);
        }

        // 2. Raise stall from underground
        if (stallPrefab != null)
        {
            Vector3 underGround = Vector3.down * 6f;
            spawnedStall = Instantiate(stallPrefab, underGround, Quaternion.identity);
            while (spawnedStall.transform.position.y < 0f)
            {
                spawnedStall.transform.position = Vector3.MoveTowards(
                    spawnedStall.transform.position, Vector3.zero, stallRiseSpeed * Time.deltaTime);
                yield return null;
            }
            spawnedStall.transform.position = Vector3.zero;
        }

        // 3. Wait for player to walk within confinement radius
        while (true)
        {
            if (playerMovement != null)
            {
                Vector3 flat = playerMovement.transform.position;
                flat.y = 0f;
                if (flat.magnitude <= confinementRadius)
                    break;
            }
            yield return null;
        }

        // 4. Lock the player inside the radius
        ConfinePlayer(true);

        // 5. Trigger map change
        MapManager.Instance?.BeginMapChange();

        // The rest is event-driven: shopkeeper death and map-finished fire TryEndPhase()
    }

    private IEnumerator EndPhase()
    {
        // Small grace period so confetti plays etc.
        yield return new WaitForSeconds(1f);

        ConfinePlayer(false);

        if (spawnedStall != null)
        {
            Destroy(spawnedStall);
            spawnedStall = null;
        }

        WaveManager.Instance?.StartNextWave();
    }

    // ── Confinement ───────────────────────────────────────────────────

    private void ConfinePlayer(bool confine)
    {
        if (playerMovement == null) return;
        playerMovement.isConfined      = confine;
        playerMovement.confinementCenter = Vector3.zero;
        playerMovement.confinementRadius = confinementRadius;
    }
}
