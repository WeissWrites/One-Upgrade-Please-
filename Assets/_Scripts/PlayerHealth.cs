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

    private static readonly WaitForSeconds RegenTick = new(0.1f);

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        if (CameraRecoil.Instance != null) CameraRecoil.Instance.TriggerHitShake();
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateUI();
        lastDamageTime = Time.time;

        if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        regenCoroutine = StartCoroutine(RegenHealth());
    }

    IEnumerator RegenHealth()
    {
        while (Time.time - lastDamageTime < regenDelay)
            yield return RegenTick;

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
            UIManager.Instance.UpdateHealthUI(Mathf.RoundToInt(currentHealth));
    }
}
