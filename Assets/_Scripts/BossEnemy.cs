using UnityEngine;
using UnityEngine.AI;

// The boss only chases once it has line of sight on the player.
// After spotting the player it never loses track of them.
public class BossEnemy : Enemy
{
    [Header("Boss — Line of Sight")]
    public float sightRange = 25f;
    [Tooltip("Total horizontal FOV cone in degrees (e.g. 90 = 45° each side)")]
    public float sightAngle = 90f;
    public LayerMask sightBlockers;

    private bool playerSpotted;

    protected override void UpdateAI()
    {
        if (player == null || agent == null || !agent.isOnNavMesh) return;

        if (!playerSpotted && CanSeePlayer())
            playerSpotted = true;

        if (playerSpotted)
            base.UpdateAI(); // normal chase + attack once spotted
        else
            agent.isStopped = true; // stand still until player enters LoS
    }

    private bool CanSeePlayer()
    {
        Vector3 origin    = transform.position + Vector3.up * 1.5f;
        Vector3 toPlayer  = player.position - origin;
        float   dist      = toPlayer.magnitude;

        if (dist > sightRange) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > sightAngle * 0.5f) return false;

        return !Physics.Raycast(origin, toPlayer.normalized, dist, sightBlockers);
    }
}
