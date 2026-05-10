using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Drives all shop canvas panels.
// Wire up the references in the Inspector; this script just shows/hides panels and populates data.
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    // ── Ability Window ────────────────────────────────────────────────
    [Header("Ability Selection Panel")]
    public GameObject abilityPanel;
    // Three ability slots. Each slot: a button, name label, description label, reroll button.
    public AbilitySlotUI[] abilitySlots = new AbilitySlotUI[3];

    // ── Upgrade Window ────────────────────────────────────────────────
    [Header("Upgrade Panel")]
    public GameObject upgradePanel;
    // Populate these buttons in the Inspector (one per upgrade track).
    public Button btnDamage;
    public Button btnAccuracy;
    public Button btnAmmo;
    public Button btnShootingDelay;
    public Button btnReloadSpeed;
    public TextMeshProUGUI lblDamageLevel;
    public TextMeshProUGUI lblAccuracyLevel;
    public TextMeshProUGUI lblAmmoLevel;
    public TextMeshProUGUI lblShootingDelayLevel;
    public TextMeshProUGUI lblReloadSpeedLevel;

    // ── Internal ──────────────────────────────────────────────────────
    private ShopkeeperNPC activeShopkeeper;
    private AbilityBase[] currentOfferedAbilities = new AbilityBase[3];
    private int[] rerollsRemaining = new int[] { 1, 1, 1 };

    void Awake()
    {
        Instance = this;
        HideAll();
    }

    // ─────────────────────────────────────────────────────────────────
    // Public API called by ShopkeeperNPC
    // ─────────────────────────────────────────────────────────────────

    public void OpenAbilityWindow(ShopkeeperNPC shopkeeper, AbilityBase[] pool)
    {
        activeShopkeeper = shopkeeper;
        rerollsRemaining = new int[] { 1, 1, 1 };

        DealAbilities(pool);

        abilityPanel.SetActive(true);
        upgradePanel.SetActive(false);
        LockCursor(false);
    }

    public void OpenUpgradeWindow()
    {
        abilityPanel.SetActive(false);
        upgradePanel.SetActive(true);
        RefreshUpgradeLabels();
    }

    public void HideAll()
    {
        if (abilityPanel  != null) abilityPanel.SetActive(false);
        if (upgradePanel  != null) upgradePanel.SetActive(false);
        LockCursor(true);
        activeShopkeeper = null;
    }

    // ─────────────────────────────────────────────────────────────────
    // Ability window internals
    // ─────────────────────────────────────────────────────────────────

    private void DealAbilities(AbilityBase[] pool)
    {
        List<AbilityBase> available = new List<AbilityBase>(pool);
        for (int i = 0; i < 3 && available.Count > 0; i++)
        {
            int idx = Random.Range(0, available.Count);
            currentOfferedAbilities[i] = available[idx];
            available.RemoveAt(idx);

            int slotIndex = i; // capture for lambda
            abilitySlots[i].SetAbility(
                currentOfferedAbilities[i],
                rerollsRemaining[i],
                () => PickAbility(slotIndex),
                () => RerollSlot(slotIndex, pool)
            );
        }
    }

    private void PickAbility(int slotIndex)
    {
        AbilityBase chosen = currentOfferedAbilities[slotIndex];
        if (chosen == null) return;

        PlayerAbilityHolder.Instance?.GrantAbility(chosen);
        activeShopkeeper?.NotifyAbilityPicked();
        OpenUpgradeWindow();
    }

    private void RerollSlot(int slotIndex, AbilityBase[] pool)
    {
        if (rerollsRemaining[slotIndex] <= 0) return;
        rerollsRemaining[slotIndex]--;

        // Pick a new ability not currently shown in other slots
        List<AbilityBase> available = new List<AbilityBase>(pool);
        for (int i = 0; i < 3; i++)
        {
            if (i != slotIndex && currentOfferedAbilities[i] != null)
                available.Remove(currentOfferedAbilities[i]);
        }

        if (available.Count == 0) return;
        currentOfferedAbilities[slotIndex] = available[Random.Range(0, available.Count)];

        abilitySlots[slotIndex].SetAbility(
            currentOfferedAbilities[slotIndex],
            rerollsRemaining[slotIndex],
            () => PickAbility(slotIndex),
            () => RerollSlot(slotIndex, pool)
        );
    }

    // ─────────────────────────────────────────────────────────────────
    // Upgrade window internals
    // ─────────────────────────────────────────────────────────────────

    void Start()
    {
        btnDamage?.onClick.AddListener(()        => ApplyUpgrade(UpgradeTrack.Damage));
        btnAccuracy?.onClick.AddListener(()      => ApplyUpgrade(UpgradeTrack.Accuracy));
        btnAmmo?.onClick.AddListener(()          => ApplyUpgrade(UpgradeTrack.Ammo));
        btnShootingDelay?.onClick.AddListener(() => ApplyUpgrade(UpgradeTrack.ShootingDelay));
        btnReloadSpeed?.onClick.AddListener(()   => ApplyUpgrade(UpgradeTrack.ReloadSpeed));
    }

    private enum UpgradeTrack { Damage, Accuracy, Ammo, ShootingDelay, ReloadSpeed }

    private void ApplyUpgrade(UpgradeTrack track)
    {
        PlayerUpgrades u = PlayerUpgrades.Instance;
        if (u == null) return;

        switch (track)
        {
            case UpgradeTrack.Damage:       if (u.damageUpgrades        < 4) u.damageUpgrades++;        break;
            case UpgradeTrack.Accuracy:     if (u.accuracyUpgrades      < 4) u.accuracyUpgrades++;      break;
            case UpgradeTrack.Ammo:         if (u.ammoUpgrades          < 4) u.ammoUpgrades++;          break;
            case UpgradeTrack.ShootingDelay:if (u.shootingDelayUpgrades < 4) u.shootingDelayUpgrades++; break;
            case UpgradeTrack.ReloadSpeed:  if (u.reloadSpeedUpgrades   < 4) u.reloadSpeedUpgrades++;   break;
        }

        RefreshUpgradeLabels();
    }

    private void RefreshUpgradeLabels()
    {
        if (PlayerUpgrades.Instance == null) return;
        SetLabel(lblDamageLevel,       PlayerUpgrades.Instance.damageUpgrades);
        SetLabel(lblAccuracyLevel,     PlayerUpgrades.Instance.accuracyUpgrades);
        SetLabel(lblAmmoLevel,         PlayerUpgrades.Instance.ammoUpgrades);
        SetLabel(lblShootingDelayLevel, PlayerUpgrades.Instance.shootingDelayUpgrades);
        SetLabel(lblReloadSpeedLevel,  PlayerUpgrades.Instance.reloadSpeedUpgrades);
    }

    private static void SetLabel(TextMeshProUGUI label, int level)
    {
        if (label != null) label.text = $"Lv {level}/4";
    }

    // ─────────────────────────────────────────────────────────────────
    // Input: E or ESC closes the current open window
    // ─────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!AnyWindowOpen()) return;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            HideAll();
    }

    public bool AnyWindowOpen() =>
        (abilityPanel != null && abilityPanel.activeSelf) ||
        (upgradePanel != null && upgradePanel.activeSelf);

    // ─────────────────────────────────────────────────────────────────

    private static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}
