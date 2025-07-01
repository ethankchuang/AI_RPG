using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Combat/Attacks/Rally", fileName = "Rally")]
public class Rally : AttackSO
{
    [Header("Rally Properties")]
    [Range(10, 50)]
    public int actionValueBonus = 25;  // Action value to give to all teammates
    
    private void OnEnable()
    {
        // Set default values for rally
        attackName = "Rally";
        description = $"Inspire your teammates, advancing their action values by {actionValueBonus} points and increasing their speed by 10 for 3 turns.";
        damageMultiplier = 0.0f; // This is a support ability
        baseDamage = 0;
        range = 0;  // Self-target
        SPCost = 2;  // Higher cost due to team-wide effect
        requiresTarget = false; // This is a self-targeting ability
    }

    public override void ExecuteNonTargeted(Unit attacker)
    {
        Debug.Log($"{attacker.name} rallies the team!");
        
        // Get all player units from the game manager
        List<Player> teammates = GetTeammates(attacker);
        
        int teammatesAffected = 0;
        foreach (Player teammate in teammates)
        {
            if (teammate != null && teammate != attacker)
            {
                // Advance the teammate's action value
                int originalActionValue = teammate.actionValue;
                teammate.actionValue += actionValueBonus;
                
                // Cap at 100 (maximum action value)
                if (teammate.actionValue > 100)
                {
                    teammate.actionValue = 100;
                }
                
                // Apply speed boost effect (10 speed increase for 3 turns)
                SpeedBoostEffect speedBoost = new SpeedBoostEffect(10, 3);
                teammate.ApplyStatusEffect(speedBoost);
                
                Debug.Log($"{teammate.name} action value advanced from {originalActionValue} to {teammate.actionValue} and gained speed boost!");
                teammatesAffected++;
            }
        }
        
        Debug.Log($"Rally affected {teammatesAffected} teammates!");
        
        // Call base method for VFX and effects
        base.ExecuteNonTargeted(attacker);
    }
    
    public override void Execute(Unit attacker, Unit target)
    {
        // This shouldn't be called for non-targeted attacks, but handle it anyway
        ExecuteNonTargeted(attacker);
    }
    
    private List<Player> GetTeammates(Unit attacker)
    {
        List<Player> teammates = new List<Player>();
        
        // Get all player units in the scene
        Player[] allPlayers = FindObjectsOfType<Player>();
        
        foreach (Player player in allPlayers)
        {
            if (player != null && player != attacker)
            {
                teammates.Add(player);
            }
        }
        
        return teammates;
    }
} 