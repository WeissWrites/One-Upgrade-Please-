using UnityEngine;
using System.Collections;

public class GolemBoss : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform throwPoint;
    public GameObject headProjectilePrefab;
    public SkinnedMeshRenderer bodyHeadMesh; // Under SkinnedMeshes in your hierarchy
    public GameObject handHeadVisual;        // The "fake" head parented to your Hand Bone

    [Header("Stats")]
    public float bossHealth = 500f;
    public bool isInvulnerable = false;

    [Header("Attack Settings")]
    public float attackRange = 25f;
    public float attackCooldown = 3f;
    public LayerMask sightLayers;

    [Header("Spine Tracking")]
    public Transform spineBone;
    public float spineTrackSpeed = 5f;

    [Header("Aura Settings")]
    public float auraRange = 15f;
    public float auraDamage = 2f;
    public float auraTickRate = 0.33f;

    private bool isAttacking;
    private float nextAuraTick;
    private float nextAttackTime;
    private float currentSpineAngle;
    private Quaternion spineRestRotation;
    private Animator anim;
    private static readonly int RegrowHash = Animator.StringToHash("Regrow");
    private static readonly WaitForSeconds WaitRegrow = new(3f);

    void Start()
    {
        anim = GetComponent<Animator>();
        if (player == null) player = GameObject.FindWithTag("Player").transform;
        if (spineBone != null) spineRestRotation = spineBone.localRotation;
    }

    void Update()
    {
        if (player == null) return;

        HandleRotation();
        HandleAura();

        if (CanSeePlayer() && !isAttacking && !isInvulnerable && Time.time >= nextAttackTime)
            StartCoroutine(StartAttackSequence());
    }

    void LateUpdate()
    {
        if (spineBone == null || player == null) return;

        if (!isInvulnerable)
        {
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0;
            float targetAngle = Vector3.SignedAngle(transform.forward, toPlayer, Vector3.up);
            targetAngle = Mathf.Clamp(targetAngle, -30f, 30f);
            currentSpineAngle = Mathf.Lerp(currentSpineAngle, targetAngle, Time.deltaTime * spineTrackSpeed);
        }

        spineBone.localRotation = spineRestRotation * Quaternion.Euler(0, currentSpineAngle, 0);
    }

    void HandleRotation()
    {
        if (isInvulnerable) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        if (dir == Vector3.zero) return;

        float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        if (Mathf.Abs(angle) > 30f)
        {
            float excess = angle - Mathf.Sign(angle) * 30f;
            Quaternion target = Quaternion.Euler(0, transform.eulerAngles.y + excess, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * 5f);
        }
    }

    void HandleAura()
    {
        if (Time.time >= nextAuraTick)
        {
            if (player != null && player.gameObject.activeInHierarchy && Vector3.Distance(transform.position, player.position) <= auraRange)
                player.GetComponent<PlayerHealth>()?.TakeDamage(auraDamage);
            nextAuraTick = Time.time + auraTickRate;
        }
    }

    public void TakeDamage(float amount)
    {
        if (isInvulnerable) return;

        bossHealth -= amount;
        Debug.Log("Boss hit! Remaining HP: " + bossHealth);
        if (bossHealth <= 0)
        {
            anim.SetTrigger("Death");
            enabled = false;
        }
    }

    bool CanSeePlayer()
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 toPlayer = (player.position + Vector3.up) - origin;
        if (toPlayer.magnitude > attackRange) return false;
        return !Physics.Raycast(origin, toPlayer.normalized, out _, toPlayer.magnitude, sightLayers);
    }

    IEnumerator StartAttackSequence()
    {
        isAttacking = true;
        anim.SetTrigger("AttackHeadThrow");
        yield return null;
    }

    // --- ANIMATION EVENTS ---

    public void PickUpHeadEvent()
    {
        bodyHeadMesh.gameObject.SetActive(false);
        handHeadVisual.SetActive(true);
    }

    public void ThrowHeadEvent()
    {
        handHeadVisual.SetActive(false);
        isInvulnerable = true;

        GameObject headObj = Instantiate(headProjectilePrefab, throwPoint.position, Quaternion.identity);
        headObj.GetComponent<GolemHeadFollower>().Setup(player, this);

        isAttacking = false;
    }

    public void OnHeadDefeated()
    {
        isInvulnerable = false;
        isAttacking = true;
        StartCoroutine(RegrowSequence());
    }

    IEnumerator RegrowSequence()
    {
        bodyHeadMesh.gameObject.SetActive(true);
        anim.SetTrigger(RegrowHash);

        yield return WaitRegrow; // match your animation clip length

        isAttacking = false;
    }
}
