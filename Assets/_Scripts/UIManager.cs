using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Health Display")]
    public TextMeshProUGUI currentHealthText;

    [Header("Ammo Display")]
    public TextMeshProUGUI currentAmmoText;
    public TextMeshProUGUI reservedAmmoText;

    [Header("Crosshair Display")]
    public CrosshairController crosshair;

    [Header("Weapon Slots Display")]
    public RawImage[] weaponSlotImages = new RawImage[3];
    public Image[] weaponSlotBorders = new Image[3];
    public Color activeSlotColor = Color.white;
    public Color inactiveSlotColor = new Color(1f, 1f, 1f, 0.35f);

    void Awake()
    {
        Instance = this;
    }

    public void UpdateCrosshair(float spread)
    {
        if (crosshair != null) crosshair.UpdateCrosshair(spread);
    }

    public void SnapCrosshair(float spread)
    {
        if (crosshair != null) crosshair.SnapCrosshair(spread);
    }

    public void UpdateAmmoUI(int current, int reserved)
    {
        currentAmmoText.text = current.ToString();
        reservedAmmoText.text = reserved.ToString();
    }

    public void UpdateHealthUI(int health)
    {
        currentHealthText.text = health.ToString();
    }

    public void SetCrosshairActive(bool active)
    {
        if (crosshair != null) crosshair.gameObject.SetActive(active);
    }

    public void UpdateWeaponSlots(RenderTexture[] textures, bool[] occupied, int activeSlot)
    {
        for (int i = 0; i < weaponSlotImages.Length; i++)
        {
            if (weaponSlotImages[i] == null) continue;
            bool isActive = i == activeSlot;
            bool hasWeapon = occupied != null && i < occupied.Length && occupied[i];

            weaponSlotImages[i].gameObject.SetActive(hasWeapon);
            if (weaponSlotBorders[i] != null) weaponSlotBorders[i].gameObject.SetActive(hasWeapon);

            if (!hasWeapon) continue;

            weaponSlotImages[i].texture = (textures != null && i < textures.Length) ? textures[i] : null;
            weaponSlotImages[i].color = isActive ? activeSlotColor : inactiveSlotColor;
            if (weaponSlotBorders[i] != null)
                weaponSlotBorders[i].color = isActive ? activeSlotColor : inactiveSlotColor;
        }
    }
}
