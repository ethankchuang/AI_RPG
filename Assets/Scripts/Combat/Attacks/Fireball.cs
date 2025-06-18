using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Fireball", fileName = "Fireball")]
public class Fireball : AttackSO
{
    private void OnEnable()
    {
        // Set default values for fireball
        attackName = "Fireball";
        description = "A magical attack that deals fire damage based on your attack stat with long range.";
        damageMultiplier = 1.3f;  // 130% of attack stat (high damage, high cost)
        baseDamage = 2;
        range = 6;
        SPCost = 2;
    }

    public override void Execute(Unit attacker, Unit target)
    {
        base.Execute(attacker, target);
        
        // Fireball has a chance to deal area damage
        if (Random.value < 0.4f) // 40% chance
        {
            Debug.Log($"Fireball spreads to nearby enemies!");
            // TODO: Implement area damage to adjacent tiles
        }
    }
} 