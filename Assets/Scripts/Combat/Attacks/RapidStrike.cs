using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Rapid Strike", fileName = "RapidStrike")]
public class RapidStrike : AttackSO
{
    private void OnEnable()
    {
        // Set default values for rapid strike
        attackName = "Rapid Strike";
        description = "A quick attack that deals moderate damage with a chance for a follow-up strike.";
        damageMultiplier = 0.7f;  // 70% of attack stat (lower per hit)
        baseDamage = 1;
        range = 1;
        SPCost = 0;
    }

    public override void Execute(Unit attacker, Unit target)
    {
        base.Execute(attacker, target);
        
        // Rapid strike has a chance for a second hit
        if (Random.value < 0.4f) // 40% chance
        {
            Debug.Log($"{attacker.name} strikes again!");
            
            // Calculate damage for second hit (same as first)
            int secondHitDamage = CalculateDamage(attacker);
            target.TakeDamage(secondHitDamage);
            
            Debug.Log($"Second hit deals {secondHitDamage} damage!");
        }
    }
} 