using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach one of these to each of the 3 ability slot root GameObjects in the ability panel.
// Wire up the child UI elements in the Inspector.
public class AbilitySlotUI : MonoBehaviour
{
    [Header("Slot UI References")]
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI descriptionLabel;
    public Button pickButton;
    public Button rerollButton;
    public TextMeshProUGUI rerollCountLabel;
    public Image abilityIcon;

    public void SetAbility(AbilityBase ability, int rerollsLeft, Action onPick, Action onReroll)
    {
        if (nameLabel        != null) nameLabel.text        = ability != null ? ability.abilityName    : "—";
        if (descriptionLabel != null) descriptionLabel.text = ability != null ? ability.description    : "";
        if (abilityIcon      != null) abilityIcon.sprite    = ability?.icon;
        if (rerollCountLabel != null) rerollCountLabel.text = rerollsLeft > 0 ? $"Reroll ({rerollsLeft})" : "No rerolls";

        pickButton?.onClick.RemoveAllListeners();
        pickButton?.onClick.AddListener(() => onPick());
        if (pickButton != null) pickButton.interactable = ability != null;

        rerollButton?.onClick.RemoveAllListeners();
        rerollButton?.onClick.AddListener(() => onReroll());
        if (rerollButton != null) rerollButton.interactable = rerollsLeft > 0;
    }
}
