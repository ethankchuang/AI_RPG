using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Quick Strike", fileName = "QuickStrike")]
public class QuickStrike : AttackSO
{
    private void OnEnable()
    {
        // Set default values for quick strike
        attackName = "Quick Strike";
        description = "A fast attack that deals moderate damage based on your attack stat with extended range.";
        damageMultiplier = 0.8f;  // 80% of attack stat (lower damage but extended range)
        baseDamage = 1;
        range = 2;
        SPCost = 1;
    }

    public override void Execute(Unit attacker, Unit target)
    {
        base.Execute(attacker, target);
        
        // Quick strike has a chance to allow another action
        if (Random.value < 0.25f) // 25% chance
        {
            Debug.Log($"{attacker.name} feels energized and can act again!");
            // TODO: Implement bonus action system
        }
    }
} 