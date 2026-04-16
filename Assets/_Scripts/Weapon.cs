using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Weapon : MonoBehaviour
{
    public WeaponDataSO data;
    [Range(0, 3)] public int currentRarityLevel = 0;
    public LayerMask shootingLayerMask;
    public Camera weaponCamera;

    public enum ShootingMode { Single, Burst, Automatic }
    [Header("Attachments GameObjects")]
    public GameObject sightAttachment;
    public GameObject laserAttachment;
    public GameObject gripAttachment;

    [Header("Live Stats")]
    public int bulletsLeft;
    public int totalReservedAmmo;

    public bool isAkimbo;
    private bool isShooting;
    private bool readyToShoot = true;
    private bool isReloading;
    private bool isSwapping;
    private WeaponDataSO.WeaponRarityData currentStats;
    [Header("Impact Effects")]
    public GameObject bulletImpactPrefab;
    public GameObject bloodImpactPrefab;

    [Header("Recoil State")]
    private Vector3 targetRotation;
    private Vector3 currentRotation;
    private Vector3 targetPosition;
    private Vector3 currentPosition;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    [Header("Visual Effects")]

    public Animator weaponAnimator;
    public Animator weaponAnimatorAkimbo;
    public GameObject akimboModel;
    public Transform bulletSpawn;
    public ParticleSystem muzzleFlash;
    public Transform secondaryBulletSpawn;
    public ParticleSystem secondaryMuzzleFlash;
    public AudioSource audioSourceSFX;
    private List<string> akimboEligibleGuns = new List<string> { "Hudson H9", "FiveSeven", "Ruger Mark IV 2245 Lite", "MicroUzi" };
    void Awake()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        InitializeWeapon();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) SetRarity(currentRarityLevel + 1);
        if (Input.GetKeyDown(KeyCode.Y)) SetRarity(currentRarityLevel - 1);

        if (isSwapping) return;
        HandleInput();
    }
    void LateUpdate()
    {
        if (isSwapping)
        {
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            return;
        }
        HandleRecoilMath();
    }
    public void InitializeWeapon()
    {
        if (data != null)
        {
            // Apply the specific stats for our current rarity
            SetRarity(currentRarityLevel);
            // Akimbo guns get double mags
            int magMult = isAkimbo ? 2 : 1;
            bulletsLeft = data.magazineSize * magMult;
            totalReservedAmmo = data.startingReservedAmmo * magMult;
        }
    }
    public void SetRarity(int newLevel)
    {
        // Set the Rarity of Gun
        currentRarityLevel = Mathf.Clamp(newLevel, 0, 3);

        if (data != null)
        {
            currentStats = data.GetRarityData(currentRarityLevel);
            // Check if weapon can be Akimbo
            isAkimbo = (currentRarityLevel == 3 && akimboEligibleGuns.Contains(data.weaponName));
            UpdateVisualAttachments();
            Debug.Log($"{data.weaponName} set to {currentStats.rarityName} rarity. Akimbo: {isAkimbo}");
        }
    }

    private void UpdateVisualAttachments()
    {
        if (sightAttachment != null) sightAttachment.SetActive(currentStats.hasSight);
        if (laserAttachment != null) laserAttachment.SetActive(currentStats.hasLaser);
        if (gripAttachment != null) gripAttachment.SetActive(currentStats.hasGrip);

        if (akimboModel != null)
        {
            akimboModel.SetActive(isAkimbo);
        }
    }
    private void FireOneShot(Transform spawnPoint, ParticleSystem flash)
    {
        bulletsLeft--;
        readyToShoot = false;
        if (flash != null) flash.Play();
        PlayFireSound(spawnPoint.position);

        // Apply Recoil
        targetPosition += new Vector3(0, 0, -data.kickBackZ);
        targetRotation += new Vector3(-data.kickRotationX, Random.Range(-1f, 1f), 0);

        // Raycast
        Ray ray = weaponCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 finalDirection = AddSpread(ray.direction);

        if (Physics.Raycast(weaponCamera.transform.position, finalDirection, out RaycastHit hit, 1000f, shootingLayerMask))
        {
            ProcessHit(hit);
        }

        CheckReloadOrReset();
    }
    private IEnumerator FireAkimboRoutine()
    {
        readyToShoot = false;

        // Shot Right Hand
        FireSingleAkimboShot(bulletSpawn, muzzleFlash);
        // Small Delay
        yield return new WaitForSeconds(0.1f);
        // Shot Left Hand
        if (bulletsLeft > 0)
        {
            FireSingleAkimboShot(secondaryBulletSpawn ?? bulletSpawn, secondaryMuzzleFlash ?? muzzleFlash);
        }

        CheckReloadOrReset();
    }
    private void FireSingleAkimboShot(Transform spawn, ParticleSystem flash)
    {
        bulletsLeft--;
        if (flash != null) flash.Play();
        PlayFireSound(spawn.position);

        targetPosition += new Vector3(0, 0, -data.kickBackZ * 0.7f); // Less recoil for Akimbo
        targetRotation += new Vector3(-data.kickRotationX * 0.7f, Random.Range(-1.5f, 1.5f), 0);

        Ray ray = weaponCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 dir = AddSpread(ray.direction);

        if (Physics.Raycast(weaponCamera.transform.position, dir, out RaycastHit hit, 1000f, shootingLayerMask))
        {
            ProcessHit(hit);
        }
    }
    private void Reload()
    {
        int maxMag = data.magazineSize * (isAkimbo ? 2 : 1);
        if (isReloading || bulletsLeft == maxMag || totalReservedAmmo <= 0) return;

        isReloading = true;
        readyToShoot = false;

        if (HasAnimator(weaponAnimator))
        {
            weaponAnimator.SetTrigger("Reload");
        }
        if (isAkimbo && HasAnimator(weaponAnimatorAkimbo))
        {
            weaponAnimatorAkimbo.SetTrigger("Reload");
        }

        if (data.reloadSound != null && audioSourceSFX != null)
        {
            audioSourceSFX.PlayOneShot(data.reloadSound);
            if (isAkimbo) audioSourceSFX.PlayOneShot(data.reloadSound);
        }
        CancelInvoke("ReloadFinished");
        Invoke("ReloadFinished", data.reloadTime);
    }
    private void ProcessHit(RaycastHit hit)
    {
        Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(hit.collider.CompareTag("Head") ? currentStats.headshotDamage : currentStats.damage);
            if (bloodImpactPrefab != null)
            {
                GameObject blood = Instantiate(bloodImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(blood, 2f);
            }
        }
        else
        {
            SpawnEnvironmentImpact(hit, hit.collider.tag);
        }
    }
    private void CheckReloadOrReset()
    {
        if (bulletsLeft <= 0 && totalReservedAmmo > 0)
        {
            if (!IsInvoking(nameof(Reload)))
            {
                Invoke(nameof(Reload), 0.2f);
            }
        }
        else
        {
            Invoke(nameof(ResetShot), currentStats.shootingDelay);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateAmmoUI(bulletsLeft, totalReservedAmmo);
        }
    }
    void HandleInput()
    {
        if (data.shootingMode == Weapon.ShootingMode.Automatic) isShooting = Input.GetKey(KeyCode.Mouse0);
        else isShooting = Input.GetKeyDown(KeyCode.Mouse0);

        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < (data.magazineSize * (isAkimbo ? 2 : 1)) && !isReloading && totalReservedAmmo > 0)
            Reload();

        if (readyToShoot && isShooting && !isReloading && !isSwapping && bulletsLeft > 0)
        {
            if (isAkimbo) StartCoroutine(FireAkimboRoutine());
            else FireOneShot(bulletSpawn, muzzleFlash);
        }
    }

    void HandleRecoilMath()
    {
        // Move back over time
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, currentStats.returnSpeed * Time.deltaTime);
        targetPosition = Vector3.Lerp(targetPosition, Vector3.zero, currentStats.returnSpeed * Time.deltaTime);

        currentRotation = Vector3.Lerp(currentRotation, targetRotation, currentStats.snappiness * Time.deltaTime);
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, currentStats.snappiness * Time.deltaTime);

        // Apply values
        transform.localPosition = originalPosition + currentPosition;
        transform.localRotation = originalRotation * Quaternion.Euler(currentRotation);
    }

    private Vector3 AddSpread(Vector3 baseDir)
    {
        // Add current Spread amount
        float x = Random.Range(-currentStats.spreadIntensity, currentStats.spreadIntensity);
        float y = Random.Range(-currentStats.spreadIntensity, currentStats.spreadIntensity);
        return (baseDir + weaponCamera.transform.right * x + weaponCamera.transform.up * y).normalized;
    }

    private void ResetShot() { if (!isReloading && !isSwapping) readyToShoot = true; }


    private void ReloadFinished()
    {
        int maxMag = data.magazineSize * (isAkimbo ? 2 : 1);
        int bulletsNeeded = maxMag - bulletsLeft;
        int amountToFill = Mathf.Min(totalReservedAmmo, bulletsNeeded);

        bulletsLeft += amountToFill;
        totalReservedAmmo -= amountToFill;

        isReloading = false;
        readyToShoot = true;
        if (UIManager.Instance != null) UIManager.Instance.UpdateAmmoUI(bulletsLeft, totalReservedAmmo);
    }
    private void OnEnable()
    {
        isSwapping = true;
        isReloading = false;
        readyToShoot = true;

        targetPosition = targetRotation = currentPosition = currentRotation = Vector3.zero;

        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;

        if (data != null && data.equipSound != null && audioSourceSFX != null)
        {
            audioSourceSFX.PlayOneShot(data.equipSound);
        }

        if (HasAnimator(weaponAnimator))
        {
            weaponAnimator.Rebind();
            weaponAnimator.Update(0f);
        }
        if (isAkimbo && HasAnimator(weaponAnimatorAkimbo))
        {
            weaponAnimatorAkimbo.Rebind();
            weaponAnimatorAkimbo.Update(0f);
        }

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateAmmoUI(bulletsLeft, totalReservedAmmo);
        // Reload if Gun empty on Swap to
        if (bulletsLeft <= 0 && totalReservedAmmo > 0)
        {

            Invoke(nameof(Reload), data != null ? data.swapInTime : 0.5f);
        }

        CancelInvoke("FinishSwapping");
        Invoke("FinishSwapping", data != null ? data.swapInTime : 0.5f);
    }
    private void OnDisable()
    {
        CancelInvoke();
        StopAllCoroutines();

        if (audioSourceSFX != null) audioSourceSFX.Stop();

        isReloading = false;
        isSwapping = true;
    }
    private void PlayFireSound(Vector3 pos)
    {
        if (data.fireSound != null && AudioManagerShooting.Instance != null)
        {
            float randomPitch = 1.0f + Random.Range(-0.1f, 0.1f);
            AudioManagerShooting.Instance.PlayFiringSound(data.fireSound, pos, randomPitch);
        }
    }
    private void SpawnEnvironmentImpact(RaycastHit hit, string tag)
    {
        if (bulletImpactPrefab == null) return;

        GameObject impact = Instantiate(bulletImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        Transform sandEffect = impact.transform.Find("SandImpactDebris");
        Transform stoneEffect = impact.transform.Find("StoneImpactDebris");
        // Find Particle
        if (sandEffect != null) sandEffect.gameObject.SetActive(false);
        if (stoneEffect != null) stoneEffect.gameObject.SetActive(false);

        // Enable Correct one
        if (tag == "Terrain")
        {
            if (sandEffect != null) sandEffect.gameObject.SetActive(true);
        }
        else if (tag == "Concrete")
        {
            if (stoneEffect != null) stoneEffect.gameObject.SetActive(true);
        }
        else
        {
            // Default
            if (stoneEffect != null) stoneEffect.gameObject.SetActive(true);
        }

        Destroy(impact, 5f);
    }

    private bool HasAnimator(Animator anim)
    {
        return anim != null && anim.runtimeAnimatorController != null;
    }
    private void FinishSwapping() => isSwapping = false;
}