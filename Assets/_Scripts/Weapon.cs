using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public LayerMask shootingLayerMask;
    public Camera playerCamera;
    // Shooting
    public bool isShooting, readyToShoot;
    bool allowReset = true;
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

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 baseDirection = ray.direction;

        // Add Spread
        Vector3 finalDirection = AddSpread(baseDirection);

        // Bullet Hit
        if (Physics.Raycast(playerCamera.transform.position, finalDirection, out RaycastHit hit, 1000f, shootingLayerMask))
        {
            // Handle Damage
            if (hit.collider.CompareTag("Enemy"))
            {
                // hit.collider.GetComponent<EnemyHealth>().TakeDamage(damage);
            }
            // Spawn Impact Effect
            CreateImpact(hit);
        }
        // Spawn Bullet Visual
        GameObject tracer = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);
        tracer.transform.forward = finalDirection;
        tracer.GetComponent<Rigidbody>().linearVelocity = finalDirection * bulletVelocity; // Set very high velocity
        StartCoroutine(DestroyBulletAfterTime(tracer, 1f));

        // Handle Reset/Burst logic
        if (allowReset)
        {
            Invoke("ResetShot", shootingDelay);
            allowReset = true;
        }
        // Shooting in Burst Mode
        if (currentShootingMode == ShootingMode.Burst && burstBulletsLeft > 1)
        {
            burstBulletsLeft--;
            Invoke("FireWeapon", shootingDelay);
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
        Vector3 spread = playerCamera.transform.right * x + playerCamera.transform.up * y;

        return (baseDir + spread).normalized;
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(bullet);
    }
}
