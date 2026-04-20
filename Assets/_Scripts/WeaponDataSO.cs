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

        public List<WeaponUpgradeData> upgradeLevels;

        [Header("Attachments Activation")]
        public bool hasSight;
        public bool hasLaser;
        public bool hasGrip;

        [System.Serializable]
        public struct WeaponUpgradeData
        {
            [Range(0, 4)] public int upgradeLevel;
            [Header("Stats")]
            public int damage;
            public int headshotDamage;
            public float shootingDelay;
            public float spreadIntensity;
            public float spreadPerShot;
            public float spreadRecovery;
            public float snappiness;
            public float returnSpeed;
        }
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

    public WeaponRarityData GetRarityData(int level)
    {
        foreach (var tier in rarityTiers)
        {
            if (tier.rarityLevel == level) return tier;
        }
        return rarityTiers[0];
    }

    public WeaponRarityData.WeaponUpgradeData GetUpgradeData(int rarityLevel, int upgradeLevel)
    {
        var rarity = GetRarityData(rarityLevel);
        foreach (var upgrade in rarity.upgradeLevels)
        {
            if (upgrade.upgradeLevel == upgradeLevel) return upgrade;
        }
        return rarity.upgradeLevels[0];
    }
}
