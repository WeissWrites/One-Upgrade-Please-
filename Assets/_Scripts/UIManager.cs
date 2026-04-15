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

    void Awake()
    {
        Instance = this;
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
}