using UnityEngine;

public abstract class AbilityBase : ScriptableObject
{
    public string abilityName = "Ability";
    [TextArea] public string description = "";
    public float cooldown = 500f;
    public Sprite icon;

    public abstract void Activate(GameObject player);
}
