using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Heavy Slam", fileName = "HeavySlam")]
public class HeavySlam : AttackSO
{
    private void OnEnable()
    {
        // Set default values for heavy slam
        attackName = "Heavy Slam";
        description = "A powerful slam attack that deals massive damage based on the enemy's attack stat.";
        damageMultiplier = 1.8f;  // 180% of attack stat
        baseDamage = 3;
        range = 1;
        SPCost = 0;
    }

    public override void Execute(Unit attacker, Unit target)
    {
        base.Execute(attacker, target);
        
        // Heavy slam has a chance to stun (visual feedback for now)
        if (Random.value < 0.3f) // 30% chance
        {
            Debug.Log($"{target.name} is stunned by the heavy slam!");
            // TODO: Implement actual stun effect when status effects are expanded
        }
    }
} 