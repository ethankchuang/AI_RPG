using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Skill Point Regeneration", fileName = "SkillPointRegeneration")]
public class Meditate : AttackSO
{
    private void OnEnable()
    {
        // Set default values for skill point regeneration
        attackName = "Meditate";
        description = "A calming technique that restores 2 skill points. No damage dealt.";
        damageMultiplier = 0f;  // No damage
        baseDamage = 0;
        range = 0;  // Self-targeting
        SPCost = 0;  // Free to use
        requiresTarget = false;  // Self-targeting attack
    }

    public override void ExecuteNonTargeted(Unit attacker)
    {
        base.ExecuteNonTargeted(attacker);
        
        // Restore 2 skill points
        if (attacker is Player player)
        {
            GameManager.Instance.RestoreSkillPoints(2);
            Debug.Log($"{attacker.name} meditates and regains 2 skill points!");
        }
    }
} 