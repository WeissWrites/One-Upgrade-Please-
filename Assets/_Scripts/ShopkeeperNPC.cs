using UnityEngine;

// Attach to the shopkeeper mesh GameObject.
// The shopkeeper handles both the ability selection and the upgrade window in sequence.
public class ShopkeeperNPC : MonoBehaviour, IInteractable
{
    [Header("Ability Pool")]
    [Tooltip("All possible abilities the player can be offered. Fill this with your AbilityBase assets.")]
    public AbilityBase[] abilityPool;

    [Header("Shooting to Kill")]
    [Tooltip("How many bullet hits it takes to blow up the shopkeeper")]
    public int hitsRequired = 5;

    [Header("Confetti")]
    public GameObject confettiPrefab;
    public Transform confettiSpawnPoint;

    private int hitCount = 0;
    private bool abilityPicked = false;
    private bool isDead = false;

    // ── IInteractable ─────────────────────────────────────────────────

    public string GetPrompt() => "Press E to talk";

    public void Interact(GameObject instigator)
    {
        if (isDead) return;
        if (ShopUI.Instance == null) return;

        // If a window is already open, close it instead
        if (ShopUI.Instance.AnyWindowOpen())
        {
            ShopUI.Instance.HideAll();
            return;
        }

        // Open the correct window depending on progress
        if (!abilityPicked)
            ShopUI.Instance.OpenAbilityWindow(this, abilityPool);
        else
            ShopUI.Instance.OpenUpgradeWindow();
    }

    // Called by ShopUI after the player picks an ability
    public void NotifyAbilityPicked()
    {
        abilityPicked = true;
    }

    // ── Bullet hits ──────────────────────────────────────────────────

    // Called by Weapon.cs ProcessHit
    public void RegisterHit()
    {
        if (isDead || !abilityPicked) return; // can't be killed before ability is chosen

        hitCount++;
        if (hitCount >= hitsRequired)
            Die();
    }

    // ─────────────────────────────────────────────────────────────────

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        ShopUI.Instance?.HideAll();

        if (confettiPrefab != null)
        {
            Vector3 pos = confettiSpawnPoint != null ? confettiSpawnPoint.position : transform.position;
            Instantiate(confettiPrefab, pos, Quaternion.identity);
        }

        ShopkeeperSequence.Instance?.OnShopkeeperKilled();
        Destroy(gameObject);
    }
}
