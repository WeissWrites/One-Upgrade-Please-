using UnityEngine;

// Attach to the player. Receives the ability chosen in the shop and activates it on Q.
public class PlayerAbilityHolder : MonoBehaviour
{
    public static PlayerAbilityHolder Instance { get; private set; }

    public AbilityBase currentAbility { get; private set; }

    private float lastActivationTime = -9999f;

    void Awake() { Instance = this; }

    public void GrantAbility(AbilityBase ability)
    {
        currentAbility = ability;
        Debug.Log($"Ability granted: {ability.abilityName}");
    }

    void Update()
    {
        if (currentAbility == null) return;
        if (Input.GetKeyDown(KeyCode.Q) && Time.time >= lastActivationTime + currentAbility.cooldown)
        {
            currentAbility.Activate(gameObject);
            lastActivationTime = Time.time;
        }
    }

    public float GetCooldownRemaining()
    {
        if (currentAbility == null) return 0f;
        return Mathf.Max(0f, lastActivationTime + currentAbility.cooldown - Time.time);
    }
}
