using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public LayerMask shootingLayerMask;
    public Camera weaponCamera;
    public enum ShootingMode
    {
        Single, Burst, Automatic
    }
    [Header("Damage Settings")]
    public int damage;
    public int headshotDamage;
    [Header("Shooting Settings")]
    // Shooting THESE ARE FOR DEBUGGING
    public bool isShooting;
    public bool readyToShoot;
    [Header("Weapon Settings")]

    public float shootingDelay = 2f;
    // Spread
    public float spreadIntensity;
    public int magazineSize = 30;
    public int bulletsLeft;
    public int totalReservedAmmo = 90;
    public float reloadTime = 1.5f;
    public bool isReloading;
    // Burst
    public int bulletPerBurst = 3;
    public int burstBulletsLeft;
    // Bullet
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 30;
    public float bulletPrefabLifeTime = 5f;
    // Muzzle Flash
    public GameObject muzzleEffect;
    // Shooting Mode
    public ShootingMode currentShootingMode;
    [Header("Audio Settings")]
    public AudioSource audioSourceSFX;
    public AudioClip fireSound;
    public float basePitch = 1.0f;
    [Range(0f, 0.5f)] public float pitchVariation = 0.1f;
    public AudioClip reloadSound;
    public AudioClip equipSound;
    [Header("Animation")]

    public Animator weaponAnimator;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    public float swapInTime = 0.5f;
    private bool isSwapping;
    void Awake()
    {
        spawnPosition = transform.localPosition;
        spawnRotation = transform.localRotation;

        readyToShoot = true;
        burstBulletsLeft = bulletPerBurst;
        bulletsLeft = magazineSize;
    }
    void Update()
    {
        if (currentShootingMode == ShootingMode.Automatic)
        {
            isShooting = Input.GetKey(KeyCode.Mouse0);
        }
        else if (currentShootingMode == ShootingMode.Single || currentShootingMode == ShootingMode.Burst)
        {
            isShooting = Input.GetKeyDown(KeyCode.Mouse0);
        }
        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !isReloading && !isSwapping && totalReservedAmmo > 0
        || bulletsLeft == 0 && !isReloading && !isSwapping && totalReservedAmmo > 0)
        {
            Reload();
        }

        if (readyToShoot && isShooting && !isReloading && !isSwapping && bulletsLeft > 0)
        {
            burstBulletsLeft = bulletPerBurst;
            FireWeapon();
        }
    }

    private void FireWeapon()
    {
        bulletsLeft--;
        muzzleEffect.GetComponent<ParticleSystem>().Play();
        readyToShoot = false;

        Ray ray = weaponCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 baseDirection = ray.direction;
        // Add Spread
        Vector3 finalDirection = AddSpread(baseDirection);
        // Fire Sound
        if (fireSound != null)
        {
            // Randomize pitch
            float randomPitch = basePitch + Random.Range(-pitchVariation, pitchVariation);
            // Enables overlapping
            AudioManagerShooting.Instance.PlayFiringSound(fireSound, bulletSpawn.position, randomPitch);
        }

        // Bullet Hit
        if (Physics.Raycast(weaponCamera.transform.position, finalDirection, out RaycastHit hit, 1000f, shootingLayerMask))
        {
            // Check if we hit an Enemy
            if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Head"))
            {
                // Determine Damage amount
                int damageToDeal = damage;
                // Headshot
                if (hit.collider.CompareTag("Head"))
                {
                    damageToDeal = headshotDamage;
                }
                // Bodyshot
                Enemy enemyScript = hit.collider.gameObject.GetComponentInParent<Enemy>();

                if (enemyScript != null)
                {
                    enemyScript.TakeDamage(damageToDeal);
                }
            }

            CreateImpact(hit);
        }
        // Spawn Bullet Visual
        GameObject tracer = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);
        tracer.transform.forward = finalDirection;
        tracer.GetComponent<Rigidbody>().linearVelocity = finalDirection * bulletVelocity;
        StartCoroutine(DestroyBulletAfterTime(tracer, bulletPrefabLifeTime));
        // Shooting in Burst Mode
        if (currentShootingMode == ShootingMode.Burst && burstBulletsLeft > 1)
        {
            burstBulletsLeft--;
            Invoke("FireWeapon", shootingDelay);
        }
        else
        {
            Invoke("ResetShot", shootingDelay);
        }
    }
    private void Reload()
    {
        isReloading = true;
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("Reload");
        }

        Invoke("ReloadFinished", reloadTime);
    }
    private void ReloadFinished()
    {
        int bulletsToReplenish = magazineSize - bulletsLeft;

        // If enough bullets, fill Magazine
        if (totalReservedAmmo >= bulletsToReplenish)
        {
            bulletsLeft = magazineSize;
            totalReservedAmmo -= bulletsToReplenish;
        }
        else // If not, fill as much as you can
        {
            bulletsLeft += totalReservedAmmo;
            totalReservedAmmo = 0;
        }

        isReloading = false;
    }
    private void PlayEquipSound()
    {
        if (audioSourceSFX != null && equipSound != null)
        {
            audioSourceSFX.Stop();
            audioSourceSFX.clip = equipSound;
            audioSourceSFX.time = 0;
            audioSourceSFX.pitch = 1f;
            audioSourceSFX.Play();
        }
    }
    private void PlayReloadSound()
    {
        if (reloadSound != null)
        {
            audioSourceSFX.PlayOneShot(reloadSound);
        }
    }
    private void CreateImpact(RaycastHit hit)
    {
        GameObject hole = Instantiate(
            GlobalReferences.Instance.bulletImpactEffectPrefab,
            hit.point,
            Quaternion.LookRotation(hit.normal)
        );
        hole.transform.SetParent(hit.transform);
    }
    private Vector3 AddSpread(Vector3 baseDir)
    {
        float x = Random.Range(-spreadIntensity, spreadIntensity);
        float y = Random.Range(-spreadIntensity, spreadIntensity);
        Vector3 spread = weaponCamera.transform.right * x + weaponCamera.transform.up * y;

        return (baseDir + spread).normalized;
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bullet != null)
        {
            Destroy(bullet);
        }
    }

    private void OnEnable() // When Weapon is swapped to
    {
        isReloading = false;
        isSwapping = true;

        PlayEquipSound();

        // Spawn gun at correct position
        transform.localPosition = spawnPosition;
        transform.localRotation = spawnRotation;

        if (weaponAnimator != null)
        {
            weaponAnimator.Rebind();
            weaponAnimator.Update(0f);
        }
        Invoke("FinishSwapping", swapInTime);
    }
    private void OnDisable() // When Weapon is swapped away
    {
        // Gun Swapped = Stop Reload Sound / Equip Sound
        if (isSwapping || isReloading)
        {
            if (audioSourceSFX != null)
            {
                audioSourceSFX.Stop();
            }
        }
        // Gun swapped = Cancel reload and don't replenish ammo (Cancel Previous Swapping)
        CancelInvoke("ReloadFinished");
        CancelInvoke("FinishSwapping");
        // Safety
        isSwapping = false;
        isReloading = false;

        transform.localPosition = spawnPosition;
        transform.localRotation = spawnRotation;

        // Reset Animator
        if (weaponAnimator != null)
        {
            weaponAnimator.Rebind();
        }
    }
    private void FinishSwapping()
    {
        isSwapping = false;
    }
}
