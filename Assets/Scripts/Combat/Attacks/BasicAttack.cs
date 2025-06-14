using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Basic Attack", fileName = "BasicAttack")]
public class BasicAttack : AttackSO
{
    private void OnEnable()
    {
        // Set default values for basic attack
        attackName = "Basic Attack";
        description = "A simple attack that deals damage and restores 1 skill point.";
        damage = 5;
        range = 1;
        SPCost = 0;
    }

    public override void Execute(Unit attacker, Unit target)
    {
        base.Execute(attacker, target);
        
        // Basic attacks restore 1 skill point
        if (attacker is Player player)
        {
            GameManager.Instance.RestoreSkillPoints(1);
        }
    }
} 