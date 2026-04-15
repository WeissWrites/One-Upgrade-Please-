using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "ScriptableObjects/WeaponData")]
public class WeaponDataSO : ScriptableObject
{
    [Header("Info")]
    public string weaponName;

    [Header("Shooting")]
    public int damage;
    public int headshotDamage;
    public float shootingDelay;
    public float spreadIntensity;
    public Weapon.ShootingMode shootingMode;

    [Header("Recoil Visuals")]
    public float kickBackZ;
    public float kickRotationX;
    public float snappiness;
    public float returnSpeed;

    [Header("Ammo")]
    public int magazineSize;
    public int startingReservedAmmo;
    public float reloadTime;

    [Header("Visuals & Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip equipSound;
    public float swapInTime = 0.5f;
}