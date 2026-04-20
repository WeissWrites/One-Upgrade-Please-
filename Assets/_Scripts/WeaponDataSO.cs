using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "ScriptableObjects/WeaponData")]
public class WeaponDataSO : ScriptableObject
{
    [System.Serializable]
    public struct WeaponStats
    {
        [Header("Damage")]
        public int damage;
        public int headshotDamage;

        [Header("Ammo")]
        public int magazineSize;
        public int startingReservedAmmo;

        [Header("Reload Speed")]
        public float reloadTime;

        [Header("Accuracy")]
        public float spreadIntensity;
        public float spreadPerShot;
        public float spreadRecovery;

        [Header("Fire Rate")]
        public float shootingDelay;

        [Header("Recoil")]
        public float snappiness;
        public float returnSpeed;
    }

    [Header("General Info")]
    public string weaponName;
    public Weapon.ShootingMode shootingMode;
    public float kickBackZ;
    public float kickRotationX;

    [Header("Base Stats")]
    public WeaponStats baseStats;

    [Header("Current Stats")]
    public WeaponStats currentStats;

    [Header("Attachments")]
    public bool canHaveSight;
    public bool canHaveLaser;
    public bool canHaveGrip;
    public bool canBeAkimbo;

    [Header("Scope")]
    public bool canScope = false;
    [Tooltip("Zoom multiplier when scoped. 2 = 2x zoom (halves FOV). 1 = no zoom.")]
    public float zoomFactor = 2f;

    [Header("Visuals & Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip equipSound;
    public AudioClip scopeSound;
    public float swapInTime = 0.5f;
}
