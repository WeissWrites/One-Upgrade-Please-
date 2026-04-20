using UnityEngine;

public class PlayerUpgrades : MonoBehaviour
{
    public static PlayerUpgrades Instance { get; private set; }

    void Awake() { Instance = this; }

    [Range(0, 4)] public int damageUpgrades = 0;
    [Range(0, 4)] public int accuracyUpgrades = 0;
    [Range(0, 4)] public int ammoUpgrades = 0;
    [Range(0, 4)] public int shootingDelayUpgrades = 0;
    [Range(0, 4)] public int reloadSpeedUpgrades = 0;

    public const float DamageMult        = 1.5f;
    public const float AccuracyMult      = 1.2f;
    public const float AmmoMult          = 1.05f;
    public const float ShootingDelayMult = 1.1f;
    public const float ReloadSpeedMult   = 1.1f;
}
