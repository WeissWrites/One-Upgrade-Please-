using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("Detection Settings")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    [Header("Audio Settings")]
    public AudioSource footstepSource;
    public float walkStepInterval = 0.5f;
    private float stepTimer;

    [Header("Footstep Sounds")]
    public AudioClip[] sandSounds;
    public AudioClip[] concreteSounds;

    void Update()
    {
        if (groundCheck == null) return;
        // Check if walking
        bool isMoving = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
        // Check if on floor
        bool hittingFloor = Physics.Raycast(groundCheck.position, Vector3.down, groundDistance, groundMask);
        if (hittingFloor && isMoving)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0)
            {
                PlayFootstep();
                stepTimer = walkStepInterval;
            }
        }
        else
        {
            stepTimer = 0;
        }
    }

    void PlayFootstep()
    {
        if (groundCheck == null) return;
        RaycastHit hit;
        if (Physics.Raycast(groundCheck.position, Vector3.down, out hit, 0.5f, groundMask))
        {
            AudioClip[] clipsToUse = null;
            switch (hit.collider.tag)
            {
                case "Terrain": clipsToUse = sandSounds; break;
                case "Concrete": clipsToUse = concreteSounds; break;
                default: clipsToUse = sandSounds; break;
            }
            if (clipsToUse != null && clipsToUse.Length > 0)
            {
                AudioClip clip = clipsToUse[Random.Range(0, clipsToUse.Length)];
                footstepSource.pitch = Random.Range(0.9f, 1.1f);
                footstepSource.PlayOneShot(clip);
            }
        }
    }
}