using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Basic Attack", fileName = "BasicAttack")]
public class BasicAttack : AttackSO
{
    private void OnEnable()
    {
        // Set default values for basic attack
        attackName = "Basic Attack";
        description = "A simple attack that deals damage based on your attack stat and restores 1 skill point.";
        damageMultiplier = 1.0f;  // 100% of attack stat
        baseDamage = 0;
        range = 1;
        SPCost = 0;
    }

    public override void Execute(Unit attacker, Unit target)
    {
        base.Execute(attacker, target);
        
        // Basic attacks restore 1 skill point to the shared pool
        if (attacker is Player player)
        {
            GameManager.Instance.RestoreSkillPoints(1);
        }
    }
} 