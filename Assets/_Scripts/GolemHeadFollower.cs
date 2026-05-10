using UnityEngine;
using UnityEngine.AI;

public class GolemHeadFollower : MonoBehaviour
{
    public enum State { InAir, Settling, Chasing }
    public State currentState = State.InAir;

    [Header("Settings")]
    public float headHealth = 50f;
    public float flightTime = 1.2f; // seconds to reach player — lower = faster and flatter
    public float chaseSpeed = 150f;
    public float chaseAcceleration = 10f;   // low = sluggish, heavy feel
    public float chaseAngularSpeed = 60f;  // low = wide drifty corners
    public float sphereRadius = 0.5f;
    public float stopThreshold = 0.1f;     // velocity below this = fully stopped

    [Header("Visuals")]
    public Transform headVisualChild;
    public GameObject hitEffectPrefab;

    private Transform player;
    private PlayerHealth playerHealth;
    private GolemBoss boss;
    private float rollDamageCooldown = 0f;
    private Rigidbody rb;
    private NavMeshAgent agent;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
    }

    public void Setup(Transform p, GolemBoss b)
    {
        player = p;
        playerHealth = p.GetComponent<PlayerHealth>();
        boss = b;
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null) agent.enabled = false;

        transform.position = b.throwPoint.position;
        rb.position = b.throwPoint.position;

        currentState = State.InAir;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Collider ownCol = GetComponent<Collider>();
        foreach (Collider c in b.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(ownCol, c);

        // Don't push the player — damage is handled by trigger overlap
        Collider playerCol = p.GetComponent<Collider>();
        if (playerCol != null) Physics.IgnoreCollision(ownCol, playerCol);

        Vector3 toTarget = p.position - transform.position;
        Vector3 horizontalVel = new Vector3(toTarget.x, 0f, toTarget.z) / flightTime;
        float verticalVel = toTarget.y / flightTime - 0.5f * Physics.gravity.y * flightTime;
        rb.linearVelocity = horizontalVel + Vector3.up * verticalVel;
    }

    void Update()
    {
        if (currentState == State.Chasing)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(player.position);
                VisualRoll(agent.velocity);
            }
            else if (agent != null && agent.enabled && !agent.isOnNavMesh)
            {
                Debug.LogWarning("Head chasing but NOT on NavMesh!");
            }

            rollDamageCooldown -= Time.deltaTime;
            if (rollDamageCooldown <= 0f && Vector3.Distance(transform.position, player.position) <= sphereRadius + 1f)
            {
                if (playerHealth != null) playerHealth.TakeDamage(100f);
                rollDamageCooldown = 1f;
            }
        }
        else if (currentState == State.InAir)
        {
            VisualRoll(rb.linearVelocity);

            if (Vector3.Distance(transform.position, player.position) <= sphereRadius + 0.5f)
                if (playerHealth != null) playerHealth.TakeDamage(float.MaxValue);
        }
        else if (currentState == State.Settling)
        {
            VisualRoll(rb.linearVelocity);

            // Don't transition while bouncing upward — wait until falling or settled
            if (rb.linearVelocity.y <= 0.1f && IsGrounded())
                StartChasing();
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, sphereRadius + 0.15f);
    }

    void VisualRoll(Vector3 vel)
    {
        if (vel.magnitude < 0.1f || headVisualChild == null) return;
        float angle = vel.magnitude * Time.deltaTime / sphereRadius * Mathf.Rad2Deg;
        Vector3 axis = Vector3.Cross(Vector3.up, vel.normalized);
        Renderer rend = headVisualChild.GetComponent<Renderer>();
        Vector3 pivot = rend != null ? rend.bounds.center : headVisualChild.position;
        headVisualChild.RotateAround(pivot, axis, angle);
    }

    public void TakeDamage(float amount)
    {
        if (currentState == State.InAir) return;

        headHealth -= amount;
        Debug.Log("Head Hit! Remaining HP: " + headHealth);

        if (headHealth <= 0)
        {
            if (boss != null) boss.OnHeadDefeated();
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.collider.CompareTag("Player"))
        {
            float damage = currentState == State.InAir ? float.MaxValue : 100f;
            if (playerHealth != null) playerHealth.TakeDamage(damage);
        }

        // Only settle when hitting an upward-facing surface (actual floor)
        // Wall and player hits keep the head flying freely via physics
        if (currentState == State.InAir && !col.collider.CompareTag("Boss") && !col.collider.CompareTag("Player"))
        {
            foreach (ContactPoint contact in col.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    currentState = State.Settling;
                    break;
                }
            }
        }
    }

    void StartChasing()
    {
        currentState = State.Chasing;

        // Carry horizontal momentum into the agent so it doesn't snap to a dead stop
        Vector3 momentum = rb.linearVelocity;
        momentum.y = 0f;

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        if (agent != null)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                agent.enabled = true;
                agent.Warp(hit.position);
                agent.speed = chaseSpeed;
                agent.acceleration = chaseAcceleration;
                agent.angularSpeed = chaseAngularSpeed;
                agent.velocity = momentum;
            }
            else
            {
                Debug.LogError("Head landed too far from a NavMesh!");
            }
        }
    }
}
