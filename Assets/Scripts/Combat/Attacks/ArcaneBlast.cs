using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Arcane Blast", fileName = "ArcaneBlast")]
public class ArcaneBlast : AttackSO
{
    private void OnEnable()
    {
        // Set default values for arcane blast
        attackName = "Arcane Blast";
        description = "A precise magical attack that deals damage based on your attack stat and pierces through defenses.";
        damageMultiplier = 1.1f;  // 110% of attack stat (slightly higher than basic)
        baseDamage = 1;
        range = 5;
        SPCost = 1;
    }

    public override void Execute(Unit attacker, Unit target)
    {
        base.Execute(attacker, target);
        
        // Arcane blast has a chance to restore mana (skill points)
        if (Random.value < 0.3f) // 30% chance
        {
            if (attacker is Player player)
            {
                GameManager.Instance.RestoreSkillPoints(1);
                Debug.Log($"{attacker.name} regains magical energy!");
            }
        }
    }
} 