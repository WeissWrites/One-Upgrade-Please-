using UnityEngine;
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
}