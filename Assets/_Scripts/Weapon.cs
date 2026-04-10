using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public LayerMask shootingLayerMask;
    public Camera weaponCamera;
    [Header("Damage Settings")]
    public int damage;
    public int headshotDamage;
    [Header("Shooting Settings")]
    // Shooting THESE ARE FOR DEBUGGING
    public bool isShooting;
    public bool readyToShoot;
    [Header("Weapon Settings")]
    public float shootingDelay = 2f;
    // Burst
    public int bulletPerBurst = 3;
    public int burstBulletsLeft;
    // Spread
    public float spreadIntensity;
    // Bullet
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 30;
    public float bulletPrefabLifeTime = 5f;
    // Muzzle Flash
    public GameObject muzzleEffect;

    public enum ShootingMode
    {
        Single, Burst, Automatic
    }

    void Awake()
    {
        readyToShoot = true;
        burstBulletsLeft = bulletPerBurst;
    }

    public ShootingMode currentShootingMode;

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

        if (readyToShoot && isShooting)
        {
            burstBulletsLeft = bulletPerBurst;
            FireWeapon();
        }
    }

    private void FireWeapon()
    {
        muzzleEffect.GetComponent<ParticleSystem>().Play();
        readyToShoot = false;

        Ray ray = weaponCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 baseDirection = ray.direction;

        // Add Spread
        Vector3 finalDirection = AddSpread(baseDirection);

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
        tracer.GetComponent<Rigidbody>().linearVelocity = finalDirection * bulletVelocity; // Set very high velocity
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
}
