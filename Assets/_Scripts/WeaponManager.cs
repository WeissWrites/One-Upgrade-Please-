using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Slots (3 active weapons)")]
    public GameObject[] weapons = new GameObject[3];

    [Header("Weapon Pool (all 13 guns, disabled in scene)")]
    public GameObject[] allWeapons;


    public static WeaponManager Instance { get; private set; }
    private int selectedWeapon = 0;
    private int[] slotRarities = new int[3];

    void Awake() { Instance = this; }

    void Start()
    {
        if (allWeapons != null)
            foreach (GameObject w in allWeapons)
                if (w != null) w.SetActive(false);

        SelectWeapon();
    }

    void Update()
    {
        // Build visual order: main hand + the two other slots in index order
        int slot2 = -1, slot3 = -1;
        for (int i = 0; i < weapons.Length; i++)
        {
            if (i == selectedWeapon) continue;
            if (slot2 == -1) slot2 = i;
            else { slot3 = i; break; }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) && slot2 >= 0 && weapons[slot2] != null)
        { SwapSlots(selectedWeapon, slot2); SelectWeapon(); }

        if (Input.GetKeyDown(KeyCode.Alpha2) && slot3 >= 0 && weapons[slot3] != null)
        { SwapSlots(selectedWeapon, slot3); SelectWeapon(); }
    }

    void SwapSlots(int a, int b)
    {
        GameObject tempWeapon = weapons[a];
        weapons[a] = weapons[b];
        weapons[b] = tempWeapon;

        int tempRarity = slotRarities[a];
        slotRarities[a] = slotRarities[b];
        slotRarities[b] = tempRarity;
    }

    void SelectWeapon()
    {
        for (int i = 0; i < weapons.Length; i++)
            if (weapons[i] != null) weapons[i].SetActive(false);

        GameObject active = weapons[selectedWeapon];
        if (active != null)
        {
            Weapon w = active.GetComponent<Weapon>();
            if (w != null) w.SetRarity(slotRarities[selectedWeapon]);
            active.SetActive(true);
        }

        RefreshHUD();
    }

    public void RefreshHUD()
    {
        if (UIManager.Instance == null) return;

        // Build display order: active weapon → icon 0, others → icons 1, 2
        int[] displaySlots = new int[weapons.Length]; // displaySlots[iconSlot] = weaponSlot index
        displaySlots[0] = selectedWeapon;
        int iconIdx = 1;
        for (int i = 0; i < weapons.Length; i++)
            if (i != selectedWeapon) displaySlots[iconIdx++] = i;

        bool[] occupied = new bool[weapons.Length];
        for (int i = 0; i < weapons.Length; i++)
            occupied[i] = weapons[displaySlots[i]] != null;

        if (WeaponIconRenderer.Instance != null && allWeapons != null)
        {
            for (int icon = 0; icon < weapons.Length; icon++)
            {
                int wSlot = displaySlots[icon];
                if (weapons[wSlot] == null) { WeaponIconRenderer.Instance.ClearSlot(icon); continue; }
                int weaponIndex = -1;
                for (int p = 0; p < allWeapons.Length; p++)
                    if (allWeapons[p] == weapons[wSlot]) { weaponIndex = p / 3; break; }
                if (weaponIndex >= 0)
                {
                    Weapon wComp = weapons[wSlot].GetComponent<Weapon>();
                    int rarity = wComp != null ? wComp.currentRarityLevel : slotRarities[wSlot];
                    WeaponIconRenderer.Instance.SetSlot(icon, weaponIndex, rarity);
                }
            }
        }

        RenderTexture[] textures = WeaponIconRenderer.Instance != null
            ? WeaponIconRenderer.Instance.GetTextures() : null;
        UIManager.Instance.UpdateWeaponSlots(textures, occupied, 0); // icon 0 is always active
    }

    public void GiveWeapon(int weaponIndex, int rarity)
    {
        if (allWeapons == null) return;

        // Find an unused copy of this weapon (3 copies per weapon, laid out sequentially)
        GameObject incoming = null;
        int baseIndex = weaponIndex * 3;
        for (int c = 0; c < 3; c++)
        {
            int idx = baseIndex + c;
            if (idx >= allWeapons.Length) break;
            GameObject candidate = allWeapons[idx];
            if (candidate == null) continue;
            bool alreadySlotted = false;
            for (int s = 0; s < weapons.Length; s++)
                if (weapons[s] == candidate) { alreadySlotted = true; break; }
            if (!alreadySlotted) { incoming = candidate; break; }
        }
        // Fallback: all 3 copies are slotted, just use first copy
        if (incoming == null && baseIndex < allWeapons.Length)
            incoming = allWeapons[baseIndex];
        if (incoming == null) return;

        // Use first open slot if available, otherwise replace current
        int slot = -1;
        for (int i = 0; i < weapons.Length; i++)
            if (weapons[i] == null) { slot = i; break; }
        if (slot == -1) slot = selectedWeapon;

        if (weapons[slot] != null && weapons[slot] != incoming)
            weapons[slot].SetActive(false);

        weapons[slot] = incoming;
        slotRarities[slot] = rarity;
        selectedWeapon = slot;
        SelectWeapon();
    }
}
