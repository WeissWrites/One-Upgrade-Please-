using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(Animator))]
[System.Serializable]
public class WeaponDisplaySettings
{
    public GameObject prefab;

    [Header("Standard")]
    public Vector3 offset = Vector3.zero;
    public Vector3 rotation = Vector3.zero;
    public float scale = 1.2f;

    [Header("Akimbo")]
    public Vector3 akimboOffset = Vector3.zero;
    public Vector3 akimboRotation = Vector3.zero;
    public float akimboScale = 1.2f;
}

public class MysteryBox : MonoBehaviour
{
    [Header("Weapon Data")]
    public List<WeaponDisplaySettings> weaponSettings;
    public Transform weaponSpawnPoint;

    [Header("Settings")]
    public float displayDuration = 20f;
    public float despawnAnimationLength = 0.5f;

    [Header("References")]
    public GameObject interactionPrompt;
    public TextMeshProUGUI promptText;
    public Animator lidAnimator;
    public Animator holderAnimator;

    private bool isRolling = false;
    private bool weaponReady = false;
    private bool isDespawning = false;
    private bool playerNearby = false;

    private GameObject currentDisplayWeapon;
    private int lastRolledIndex;
    private int lastRolledRarity;
    private Coroutine idleTimerCoroutine;

    private int lastWeaponIndex = -1;

    private void Awake()
    {
        if (holderAnimator == null) holderAnimator = GetComponent<Animator>();
        if (lidAnimator == null) lidAnimator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        foreach (Transform child in transform) child.gameObject.SetActive(true);
        isRolling = false;
        weaponReady = false;
        isDespawning = false;
        if (currentDisplayWeapon != null) Destroy(currentDisplayWeapon);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (holderAnimator != null)
        {
            holderAnimator.ResetTrigger("Disappear");
            holderAnimator.Play("Mystery Box Spawn", 0, 0f);
            holderAnimator.Update(0f);
        }
        StartIdleTimer();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (currentDisplayWeapon != null) Destroy(currentDisplayWeapon);
        playerNearby = false;
    }

    private void Update()
    {
        if (!playerNearby || !Input.GetKeyDown(KeyCode.F)) return;
        if (!isRolling && !weaponReady && !isDespawning)
            OpenBox();
        else if (weaponReady)
            TakeWeapon();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = true;
        if (interactionPrompt != null) interactionPrompt.SetActive(true);
        UpdatePrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = false;
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    private void UpdatePrompt()
    {
        if (promptText == null) return;
        if (weaponReady)
        {
            string weaponName = currentDisplayWeapon.name.Replace("(Clone)", "").Trim();
            promptText.text = $"Press <color=#FFD700><b>[F]</b></color> to Take {weaponName}";
        }
        else if (!isRolling && !isDespawning)
            promptText.text = "Press <color=#FFD700><b>[F]</b></color> to Open Mystery Box";
        else
            promptText.text = "";
    }

    public void OpenBox()
    {
        StopIdleTimer();
        if (lidAnimator != null) lidAnimator.SetTrigger("Open");
        StartCoroutine(RollRoutine());
    }

    private int RollWeapon()
    {
        int index;
        do { index = Random.Range(0, weaponSettings.Count); }
        while (index == lastWeaponIndex && weaponSettings.Count > 1);
        lastWeaponIndex = index;
        return index;
    }

    private IEnumerator RollRoutine()
    {
        isRolling = true;
        UpdatePrompt();

        lastRolledIndex = RollWeapon();
        lastRolledRarity = DetermineRarity();

        // Timing Settings
        float totalRollTime = 3.0f;     // The flickering lasts 3 seconds
        float riseTime = 0.5f;          // The actual motion only takes 0.5 seconds

        float elapsed = 0;
        int currentWep = 0;
        float cycleSpeed = 0.05f;

        while (elapsed < totalRollTime)
        {
            // Calculate progress for the rise (clamped so it stops moving at 0.5s)
            float moveT = Mathf.Clamp01(elapsed / riseTime);

            // Rise from -2 to 0 and Scale from 0.7 to 1.0
            float currentYOffset = Mathf.Lerp(-2f, 0f, moveT);
            float currentScaleMult = Mathf.Lerp(0.7f, 1.0f, moveT);

            GameObject temp = SpawnDisplayWeapon(currentWep, DetermineRarity(), currentYOffset, currentScaleMult);

            // Flicker logic - slows down slightly over the full 3 seconds
            float waitTime = Mathf.Lerp(cycleSpeed, 0.15f, elapsed / totalRollTime);
            yield return new WaitForSeconds(waitTime);

            if (temp != null) Destroy(temp);
            elapsed += waitTime;

            currentWep++;
            if (currentWep >= weaponSettings.Count) currentWep = 0;
        }

        // Final weapon: Fully risen and full scale
        currentDisplayWeapon = SpawnDisplayWeapon(lastRolledIndex, lastRolledRarity, 0f, 1.0f);
        if (currentDisplayWeapon != null)
        {
            Weapon w = currentDisplayWeapon.GetComponent<Weapon>();
            if (w != null)
            {
                w.SetRarity(lastRolledRarity);
                w.enabled = false;
            }
            foreach (Renderer r in currentDisplayWeapon.GetComponentsInChildren<Renderer>()) r.enabled = true;
        }

        isRolling = false;
        weaponReady = true;
        UpdatePrompt();
        StartIdleTimer();
    }

    private GameObject SpawnDisplayWeapon(int index, int rarity, float yOffset, float scaleMultiplier)
    {
        if (index >= weaponSettings.Count || weaponSettings[index].prefab == null) return null;
        WeaponDisplaySettings settings = weaponSettings[index];
        GameObject weapon = Instantiate(settings.prefab, weaponSpawnPoint);
        Weapon weaponScript = weapon.GetComponent<Weapon>();

        if (weaponScript != null)
        {
            if (rarity != -1) weaponScript.SetRarity(rarity);
            weaponScript.enabled = false;
        }

        bool isAkimbo = (weaponScript != null && weaponScript.isAkimbo);
        Vector3 verticalLift = new Vector3(0, yOffset, 0);

        if (isAkimbo)
        {
            weapon.transform.localPosition = settings.akimboOffset + verticalLift;
            weapon.transform.localRotation = Quaternion.Euler(settings.akimboRotation);
            weapon.transform.localScale = (Vector3.one * settings.akimboScale) * scaleMultiplier;
        }
        else
        {
            weapon.transform.localPosition = settings.offset + verticalLift;
            weapon.transform.localRotation = Quaternion.Euler(settings.rotation);
            weapon.transform.localScale = (Vector3.one * settings.scale) * scaleMultiplier;
        }

        foreach (Renderer r in weapon.GetComponentsInChildren<Renderer>()) r.enabled = true;
        return weapon;
    }

    private void TakeWeapon()
    {
        WeaponManager wm = FindFirstObjectByType<WeaponManager>();
        if (wm != null) wm.GiveWeapon(lastRolledIndex, lastRolledRarity);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        playerNearby = false;
        if (currentDisplayWeapon != null) Destroy(currentDisplayWeapon);
        weaponReady = false;
        UpdatePrompt();
        StartDespawn();
    }

    private void StartDespawn()
    {
        if (isDespawning || !gameObject.activeInHierarchy) return;
        StopIdleTimer();
        isDespawning = true;
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        playerNearby = false;
        if (lidAnimator != null) lidAnimator.SetTrigger("Close");
        if (holderAnimator != null) holderAnimator.SetTrigger("Disappear");
        StartCoroutine(DespawnCoroutine());
    }

    private IEnumerator DespawnCoroutine()
    {
        yield return new WaitForSeconds(despawnAnimationLength);
        if (currentDisplayWeapon != null) Destroy(currentDisplayWeapon);
        yield return new WaitForSeconds(despawnAnimationLength);
        if (MysteryBoxManager.Instance != null) MysteryBoxManager.Instance.OnBoxClosed();
        if (transform.parent != null) transform.parent.gameObject.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void StartIdleTimer()
    {
        StopIdleTimer();
        idleTimerCoroutine = StartCoroutine(IdleTimerRoutine());
    }

    private void StopIdleTimer() { if (idleTimerCoroutine != null) StopCoroutine(idleTimerCoroutine); }

    private IEnumerator IdleTimerRoutine()
    {
        yield return new WaitForSeconds(displayDuration);
        StartDespawn();
    }

    private int DetermineRarity()
    {
        float roll = Random.Range(0f, 100f);
        if (roll <= 5f) return 3;
        if (roll <= 20f) return 2;
        if (roll <= 50f) return 1;
        return 0;
    }
}