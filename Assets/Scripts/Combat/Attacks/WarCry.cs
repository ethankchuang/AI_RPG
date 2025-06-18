using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/War Cry", fileName = "WarCry")]
public class WarCry : AttackSO
{
    [Header("War Cry Properties")]
    [Range(0.1f, 1.0f)]
    public float damageMultiplierToEnemies = 0.5f;  // 50% of attack stat as damage
    [Range(0.1f, 1.0f)]
    public float healMultiplierToAllies = 0.3f;     // 30% of attack stat as healing
    public int effectRange = 2;
    
    private void OnEnable()
    {
        // Set default values for war cry
        attackName = "War Cry";
        description = "A rallying shout that damages nearby enemies and heals nearby allies based on your attack stat.";
        damageMultiplier = 0.0f; // Damage is handled separately
        baseDamage = 0;
        range = 0;  // Non-targeted
        SPCost = 2;
        requiresTarget = false; // This is an area effect ability
    }

    public override void ExecuteNonTargeted(Unit attacker)
    {
        Debug.Log($"{attacker.name} lets out a mighty war cry!");
        
        // Find all units within range
        Unit[] allUnits = Object.FindObjectsOfType<Unit>();
        
        foreach (Unit unit in allUnits)
        {
            // Calculate distance to the unit
            float distance = Vector2.Distance(
                new Vector2(attacker.transform.position.x, attacker.transform.position.y),
                new Vector2(unit.transform.position.x, unit.transform.position.y)
            );
            
            // Skip if unit is too far away or is the attacker
            if (distance > effectRange || unit == attacker)
                continue;
            
            // Apply effects based on unit type
            if (unit is Enemy)
            {
                // Calculate damage based on attacker's attack stat
                int scaledDamage = Mathf.RoundToInt(attacker.attackDamage * damageMultiplierToEnemies);
                scaledDamage = Mathf.Max(1, scaledDamage); // Minimum 1 damage
                
                unit.TakeDamage(scaledDamage);
                Debug.Log($"War Cry damages {unit.name} for {scaledDamage}!");
            }
            else if (unit is Player)
            {
                // Calculate healing based on attacker's attack stat
                int scaledHealing = Mathf.RoundToInt(attacker.attackDamage * healMultiplierToAllies);
                scaledHealing = Mathf.Max(1, scaledHealing); // Minimum 1 healing
                
                unit.Heal(scaledHealing);
                Debug.Log($"War Cry heals {unit.name} for {scaledHealing}!");
            }
        }
        
        // Call base method for VFX and effects
        base.ExecuteNonTargeted(attacker);
    }
    
    public override void Execute(Unit attacker, Unit target)
    {
        // This shouldn't be called for non-targeted attacks, but handle it anyway
        ExecuteNonTargeted(attacker);
    }
} 