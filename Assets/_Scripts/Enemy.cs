using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    [Header("Data Source")]
    public EnemyDataSO data;

    [Header("Melee Attack")]
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    [Header("Movement")]
    [Tooltip("How fast the zombie turns. Default NavMesh value is 120 — higher = snappier turns.")]
    public float angularSpeed = 720f;
    public float acceleration = 20f;

    [Header("Animation")]
    [Tooltip("Name of the float parameter in the Animator that controls movement speed")]
    public string speedParameter = "Speed";

    protected float currentHealth;
    [SerializeField] protected float attackDamage = 10f;
    protected Transform player;
    protected NavMeshAgent agent;

    private float nextAttackTime;
    private Animator anim;
    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private bool isDead = false;
    private bool hasSpeedParam = false;

    private static readonly int HashAttack   = Animator.StringToHash("Attack");
    private static readonly int HashHit      = Animator.StringToHash("Hit");
    private static readonly int HashHitIndex = Animator.StringToHash("HitIndex");
    private static readonly int HashDie      = Animator.StringToHash("Die");
    private static readonly WaitForSeconds CorpseDelay = new(3f);

    void Awake()
    {
        anim  = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        rb    = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = 0f;
        agent.autoTraverseOffMeshLink = true;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        if (data != null)
            currentHealth = data.maxHealth;

        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.name == speedParameter && p.type == AnimatorControllerParameterType.Float)
            {
                hasSpeedParam = true;
                break;
            }
        }
    }

    void Update() => UpdateAI();

    private static readonly int HashPunchingState = Animator.StringToHash("Zombie Punching");

    protected virtual void UpdateAI()
    {
        if (player == null || agent == null || !agent.isOnNavMesh) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distToPlayer = toPlayer.magnitude;

        // Freeze in place and only rotate toward player while punching
        bool isPunching = anim.GetCurrentAnimatorStateInfo(0).shortNameHash == HashPunchingState;
        if (isPunching)
        {
            agent.isStopped = true;
            if (toPlayer != Vector3.zero)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(toPlayer),
                    angularSpeed * Time.deltaTime);

            if (hasSpeedParam) anim.SetFloat(speedParameter, 0f);
            return;
        }

        if (distToPlayer > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;

            if (Time.time >= nextAttackTime)
            {
                anim.SetTrigger(HashAttack);
                nextAttackTime = Time.time + attackCooldown;
            }
        }

        if (hasSpeedParam)
            anim.SetFloat(speedParameter, agent.velocity.magnitude);
    }

    // Called by the Zombie Punching animation event at the moment of impact
    public void DealDamage()
    {
        if (isDead || playerHealth == null) return;
        playerHealth.TakeDamage(attackDamage);
    }

    public void SetRuntimeStats(float health, float damage, float speed)
    {
        currentHealth = health;
        attackDamage  = damage;
        if (agent != null) agent.speed = speed;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
            Die();
        else
        {
            anim.SetInteger(HashHitIndex, Random.Range(0, 4));
            anim.SetTrigger(HashHit);
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        anim.SetTrigger(HashDie);
        rb.isKinematic = true;

        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in allColliders) c.enabled = false;

        agent.enabled = false;

        if (WaveManager.Instance != null) WaveManager.Instance.OnEnemyDied();

        StartCoroutine(CleanupCorpse());
    }

    IEnumerator CleanupCorpse()
    {
        yield return CorpseDelay;
        Destroy(gameObject);
    }
}
