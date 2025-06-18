using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Healing Light", fileName = "HealingLight")]
public class HealingLight : AttackSO
{
    [Header("Healing Properties")]
    [Range(0.1f, 2.0f)]
    public float healMultiplier = 0.8f;  // 80% of attack stat as healing
    
    private void OnEnable()
    {
        // Set default values for healing light
        attackName = "Healing Light";
        description = "A divine ability that heals the caster based on their attack stat.";
        damageMultiplier = 0.0f; // This is a healing ability
        baseDamage = 0;
        range = 0;  // Self-target
        SPCost = 1;
        requiresTarget = false; // This is a self-targeting ability
    }

    public override void Execute(Unit attacker, Unit target)
    {
        // This shouldn't be called for non-targeted attacks, but handle it anyway
        ExecuteNonTargeted(attacker);
    }
    
    public override void ExecuteNonTargeted(Unit attacker)
    {
        // Calculate healing based on attacker's attack stat
        if (attacker != null)
        {
            int scaledHealing = Mathf.RoundToInt(attacker.attackDamage * healMultiplier);
            scaledHealing = Mathf.Max(1, scaledHealing); // Minimum 1 healing
            
            attacker.Heal(scaledHealing);
            Debug.Log($"{attacker.name} healed for {scaledHealing} HP!");
        }
        
        // Call base method for VFX and effects
        base.ExecuteNonTargeted(attacker);
    }
} 