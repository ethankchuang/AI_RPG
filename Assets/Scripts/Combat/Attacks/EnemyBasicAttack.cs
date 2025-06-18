using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Enemy Basic Attack", fileName = "EnemyBasicAttack")]
public class EnemyBasicAttack : AttackSO
{
    private void OnEnable()
    {
        // Set default values for enemy basic attack
        attackName = "Claw";
        description = "A basic melee attack that deals damage based on the enemy's attack stat.";
        damageMultiplier = 1.0f;  // 100% of attack stat
        baseDamage = 0;
        range = 1;
        SPCost = 0;
    }
} 