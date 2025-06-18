using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Taunting Shout", fileName = "TauntingShout")]
public class TauntingShout : AttackSO
{
    [Header("Taunt Properties")]
    public int aggroIncrease = 5;
    public int tauntDuration = 3;
    
    private void OnEnable()
    {
        // Set default values for taunting shout
        attackName = "Taunting Shout";
        description = $"Increase your taunt value by {aggroIncrease} for {tauntDuration} actions, making enemies more likely to target you.";
        damageMultiplier = 0.0f; // This is a self-buff ability
        baseDamage = 0;
        range = 0;  // Self-target
        SPCost = 0;
        requiresTarget = false; // This is a self-targeting ability
    }

    public override void ExecuteNonTargeted(Unit attacker)
    {
        Debug.Log($"{attacker.name} lets out a taunting shout!");
        
        // Create and apply the taunt effect
        TauntEffect tauntEffect = new TauntEffect(aggroIncrease, tauntDuration);
        attacker.ApplyStatusEffect(tauntEffect);
        
        // Call base method for VFX and effects
        base.ExecuteNonTargeted(attacker);
    }
    
    public override void Execute(Unit attacker, Unit target)
    {
        // This shouldn't be called for non-targeted attacks, but handle it anyway
        ExecuteNonTargeted(attacker);
    }
} 