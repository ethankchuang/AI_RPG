using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Heavy Strike", fileName = "HeavyStrike")]
public class HeavyStrike : AttackSO
{
    private void OnEnable()
    {
        // Set default values for heavy strike
        attackName = "Heavy Strike";
        description = "A powerful attack that deals high damage.";
        damage = 15;
        range = 2;
        SPCost = 1;
    }

    public override void Execute(Unit attacker, Unit target)
    {
        base.Execute(attacker, target);
        
        // Heavy strike has a chance to stun
        if (Random.value < 0.3f) // 30% chance
        {
            // TODO: Implement stun effect
            Debug.Log($"{target.name} was stunned!");
        }
    }
} 