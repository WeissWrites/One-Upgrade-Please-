using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public GameObject[] weapons;
    private int selectedWeapon = 0;

    void Start()
    {
        SelectWeapon();
    }

    void Update()
    {
        int previousSelected = selectedWeapon;
        // Switch Weapons (1, 2)
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedWeapon = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedWeapon = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedWeapon = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedWeapon = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5)) selectedWeapon = 4;
        if (Input.GetKeyDown(KeyCode.Alpha6)) selectedWeapon = 5;
        if (Input.GetKeyDown(KeyCode.Alpha7)) selectedWeapon = 6;
        if (Input.GetKeyDown(KeyCode.Alpha8)) selectedWeapon = 7;
        if (Input.GetKeyDown(KeyCode.Alpha9)) selectedWeapon = 8;
        if (Input.GetKeyDown(KeyCode.Alpha0)) selectedWeapon = 9;
        if (previousSelected != selectedWeapon)
        {
            SelectWeapon();
        }
    }

    void SelectWeapon()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            // Activate Weapon / Deactivate others
            weapons[i].SetActive(i == selectedWeapon);
        }
    }
}