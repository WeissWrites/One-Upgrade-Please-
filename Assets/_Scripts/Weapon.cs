using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponDataSO data;
    public LayerMask shootingLayerMask;
    public Camera weaponCamera;

    public enum ShootingMode { Single, Burst, Automatic }
    [Header("Rarity Settings")]
    [Range(0, 3)] public int currentRarity = 0; // 0:Common, 1:Rare, 2:Epic, 3:Legendary

    [Header("Attachments GameObjects")]
    public GameObject sightAttachment;
    public GameObject laserAttachment;
    public GameObject gripAttachment;
    public GameObject muzzleAttachment;
    [Header("Live Stats")]
    public int bulletsLeft;
    public int totalReservedAmmo;
    private bool isShooting;
    private bool readyToShoot = true;
    private bool isReloading;
    private bool isSwapping;
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
    public Transform bulletSpawn;
    public ParticleSystem muzzleFlash;
    public AudioSource audioSourceSFX;
    void Start()
    {
        RefreshWeaponVisuals();
    }
    void Awake()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;

        if (data != null)
        {
            bulletsLeft = data.magazineSize;
            totalReservedAmmo = data.startingReservedAmmo;
        }
    }

    void Update()
    {
        if (isSwapping) return;

        HandleInput();
        HandleRecoilMath();
    }
    void LateUpdate()
    {
        if (isSwapping) return;

        HandleRecoilMath();
    }

    private void FireWeapon()
    {
        bulletsLeft--;
        readyToShoot = false;

        // Visuals and Audio
        if (muzzleFlash != null) muzzleFlash.Play();

        if (data.fireSound != null && AudioManagerShooting.Instance != null)
        {
            float randomPitch = 1.0f + Random.Range(-0.1f, 0.1f);
            AudioManagerShooting.Instance.PlayFiringSound(data.fireSound, bulletSpawn.position, randomPitch);
        }

        // Recoil
        targetPosition += new Vector3(0, 0, -data.kickBackZ);
        targetRotation += new Vector3(-data.kickRotationX, Random.Range(-1f, 1f), 0);

        // Bullet
        Ray ray = weaponCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 finalDirection = AddSpread(ray.direction);

        if (Physics.Raycast(weaponCamera.transform.position, finalDirection, out RaycastHit hit, 1000f, shootingLayerMask))
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                // Hit Enemy
                enemy.TakeDamage(hit.collider.CompareTag("Head") ? data.headshotDamage : data.damage);
                if (bloodImpactPrefab != null)
                {
                    GameObject blood = Instantiate(bloodImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(blood, 2f);
                }
            }
            else if (hit.collider.CompareTag("Concrete"))
            {
                // Hit Stone/Concrete
                SpawnEnvironmentImpact(hit, "Stone");
            }
            else if (hit.collider.CompareTag("Terrain"))
            {
                // Hit Sand
                SpawnEnvironmentImpact(hit, "Sand");
            }
        }
        // Reload Automatically if empty
        if (bulletsLeft <= 0 && totalReservedAmmo > 0)
        {
            if (!IsInvoking("Reload")) Invoke("Reload", 0.2f);
        }
        else
        {
            CancelInvoke("ResetShot");
            Invoke("ResetShot", data.shootingDelay);
        }
        if (data != null && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateAmmoUI(bulletsLeft, totalReservedAmmo);
        }
    }
    void HandleInput()
    {
        if (data.shootingMode == ShootingMode.Automatic)
            isShooting = Input.GetKey(KeyCode.Mouse0);
        else
            isShooting = Input.GetKeyDown(KeyCode.Mouse0);

        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < data.magazineSize && !isReloading && totalReservedAmmo > 0)
            Reload();

        // Only fire if ready AND not busy with other actions
        if (readyToShoot && isShooting && !isReloading && !isSwapping && bulletsLeft > 0)
            FireWeapon();
    }

    void HandleRecoilMath()
    {
        // Move back over time
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, data.returnSpeed * Time.deltaTime);
        targetPosition = Vector3.Lerp(targetPosition, Vector3.zero, data.returnSpeed * Time.deltaTime);

        currentRotation = Vector3.Lerp(currentRotation, targetRotation, data.snappiness * Time.deltaTime);
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, data.snappiness * Time.deltaTime);

        // Apply values
        transform.localPosition = originalPosition + currentPosition;
        transform.localRotation = originalRotation * Quaternion.Euler(currentRotation);
    }

    private Vector3 AddSpread(Vector3 baseDir)
    {
        float x = Random.Range(-data.spreadIntensity, data.spreadIntensity);
        float y = Random.Range(-data.spreadIntensity, data.spreadIntensity);
        return (baseDir + weaponCamera.transform.right * x + weaponCamera.transform.up * y).normalized;
    }

    private void ResetShot()
    {
        if (!isReloading && !isSwapping)
        {
            readyToShoot = true;
        }
    }

    private void Reload()
    {
        if (isReloading || bulletsLeft == data.magazineSize || totalReservedAmmo <= 0) return;

        isReloading = true;
        CancelInvoke("ResetShot");
        readyToShoot = false;

        if (weaponAnimator != null) weaponAnimator.SetTrigger("Reload");
        if (data.reloadSound != null && audioSourceSFX != null)
        {
            audioSourceSFX.clip = data.reloadSound;
            audioSourceSFX.Play();
        }

        CancelInvoke("ReloadFinished");
        Invoke("ReloadFinished", data.reloadTime);
    }

    private void ReloadFinished()
    {
        int bulletsNeeded = data.magazineSize - bulletsLeft;
        int amountToFill = Mathf.Min(totalReservedAmmo, bulletsNeeded);
        bulletsLeft += amountToFill;
        totalReservedAmmo -= amountToFill;

        isReloading = false;
        readyToShoot = true;
        if (data != null && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateAmmoUI(bulletsLeft, totalReservedAmmo);
        }
    }

    private void OnEnable()
    {
        isSwapping = true;
        isReloading = false;

        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;

        targetPosition = Vector3.zero;
        targetRotation = Vector3.zero;
        currentPosition = Vector3.zero;
        currentRotation = Vector3.zero;
        if (weaponAnimator != null) weaponAnimator.Play("Idle", 0, 0f);

        if (data.equipSound != null && audioSourceSFX != null)
        {
            audioSourceSFX.clip = data.equipSound;
            audioSourceSFX.Play();
        }
        if (data != null && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateAmmoUI(bulletsLeft, totalReservedAmmo);
        }
        CancelInvoke("FinishSwapping");
        Invoke("FinishSwapping", data.swapInTime);
    }
    private void OnDisable()
    {
        if (audioSourceSFX != null)
        {
            audioSourceSFX.Stop();
        }
        CancelInvoke();
        isReloading = false;
        isSwapping = false;
        readyToShoot = true;

        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
    }
    private void SpawnEnvironmentImpact(RaycastHit hit, string type)
    {
        if (bulletImpactPrefab == null) return;

        GameObject impact = Instantiate(bulletImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        impact.transform.SetParent(hit.transform);

        Transform stoneDebris = impact.transform.Find("StoneImpactDebris");
        Transform sandDebris = impact.transform.Find("SandImpactDebris");
        Transform bulletHole = impact.transform.Find("BulletHole");

        if (stoneDebris) stoneDebris.gameObject.SetActive(false);
        if (sandDebris) sandDebris.gameObject.SetActive(false);

        if (type == "Stone" && stoneDebris) stoneDebris.gameObject.SetActive(true);
        if (type == "Sand" && sandDebris) sandDebris.gameObject.SetActive(true);

        if (bulletHole) bulletHole.gameObject.SetActive(true);

        Destroy(impact, 5f);
    }
    public void RefreshWeaponVisuals()
    {

        // Activate Attachment bases on Rarity
        // Common (0): No attachments
        // Rare (1): Sight
        // Epic (2): Sight + Laser
        // Legendary (3): Sight + Laser + Grip
        if (sightAttachment) sightAttachment.SetActive(currentRarity >= 1);
        if (laserAttachment) laserAttachment.SetActive(currentRarity >= 2);

        // Only Rifles have grips, so we check if it exists
        if (gripAttachment) gripAttachment.SetActive(currentRarity >= 3);

        // 2. Special Case: Sniper is always Legendary
        if (data.weaponName == "CZ 600 Trail") // Use your Sniper's name
        {
            currentRarity = 3;
            if (sightAttachment) sightAttachment.SetActive(true);
            if (laserAttachment) laserAttachment.SetActive(true);
        }
    }

    private void FinishSwapping() => isSwapping = false;
}