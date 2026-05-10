using UnityEngine;
using TMPro;

// Attach to the player. Raycasts from the camera on E press and calls Interact() on whatever is hit.
[RequireComponent(typeof(PlayerMovement))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 3f;
    public LayerMask interactMask = ~0;

    [Header("UI")]
    [Tooltip("A world-space or screen-space TextMeshPro label that shows 'Press E to ...'")]
    public TextMeshProUGUI promptLabel;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        if (promptLabel != null) promptLabel.gameObject.SetActive(false);
    }

    void Update()
    {
        IInteractable target = GetLookedAtInteractable();

        if (promptLabel != null)
        {
            if (target != null)
            {
                promptLabel.gameObject.SetActive(true);
                promptLabel.text = target.GetPrompt();
            }
            else
            {
                promptLabel.gameObject.SetActive(false);
            }
        }

        if (Input.GetKeyDown(KeyCode.E) && target != null)
            target.Interact(gameObject);
    }

    private IInteractable GetLookedAtInteractable()
    {
        if (cam == null) return null;
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactRange, interactMask))
            return null;
        return hit.collider.GetComponentInParent<IInteractable>();
    }
}
