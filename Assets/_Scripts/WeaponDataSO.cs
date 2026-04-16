using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "ScriptableObjects/WeaponData")]
public class WeaponDataSO : ScriptableObject
{
    [System.Serializable]
    public struct WeaponRarityData
    {
        public string rarityName;
        [Range(0, 3)] public int rarityLevel; // 0=Common, 1=Rare, 2=Epic, 3=Legendary

        [Header("Stats Boosts")]
        public int damage;
        public int headshotDamage;
        public float shootingDelay;
        public float spreadIntensity;
        public float snappiness;
        public float returnSpeed;

        [Header("Attachments Activation")]
        public bool hasSight;
        public bool hasLaser;
        public bool hasGrip;
    }

    [Header("General Info")]
    public string weaponName;
    public Weapon.ShootingMode shootingMode;
    public int magazineSize;
    public int startingReservedAmmo;
    public float reloadTime;
    public float kickBackZ;
    public float kickRotationX;

    [Header("Rarity Tiers")]
    public List<WeaponRarityData> rarityTiers = new List<WeaponRarityData>();

    [Header("Visuals & Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip equipSound;
    public float swapInTime = 0.5f;

    public WeaponRarityData GetRarityData(int level)
    {
        foreach (var tier in rarityTiers)
        {
            if (tier.rarityLevel == level) return tier;
        }
        return rarityTiers[0];
    }
}