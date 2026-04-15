using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Regen Settings")]
    public float regenDelay = 2.5f;
    public float regenDuration = 5f;

    private float lastDamageTime;
    private Coroutine regenCoroutine;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        // Taking Damage
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateUI();
        // Remember when we took DMG
        lastDamageTime = Time.time;

        // Restart Regen when Hit during Regen
        if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        regenCoroutine = StartCoroutine(RegenHealth());
    }

    IEnumerator RegenHealth()
    {
        // Wait for Delay before Regen
        while (Time.time - lastDamageTime < regenDelay)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Calculate Regen Speed
        float regenSpeed = maxHealth / regenDuration;

        while (currentHealth < maxHealth)
        {
            currentHealth += regenSpeed * Time.deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            UpdateUI();
            yield return null;
        }
    }

    void UpdateUI()
    {
        if (UIManager.Instance != null)
        {
            // Make HUD Show INT not FLOAT
            UIManager.Instance.UpdateHealthUI(Mathf.RoundToInt(currentHealth));
        }
    }
}