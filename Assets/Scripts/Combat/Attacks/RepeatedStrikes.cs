using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Repeated Strikes", fileName = "RepeatedStrikes")]
public class RepeatedStrikes : AttackSO
{
    // Static counter to track usage across all instances
    private static int usageCount = 0;
    
    private void OnEnable()
    {
        // Set default values for repeated strikes
        attackName = "Repeated Strikes";
        description = "A technique that grows stronger with each use. Damage increases every time you use this attack.";
        damageMultiplier = 0.8f;  // 80% of attack stat (starts lower)
        baseDamage = 0;
        range = 1;
        SPCost = 1;
    }

    protected override int CalculateDamage(Unit attacker)
    {
        // Get base scaled damage
        int baseDamage = base.CalculateDamage(attacker);
        
        // Add bonus damage based on usage count
        // Each use adds 20% of attack stat as bonus damage
        int bonusDamage = Mathf.RoundToInt(attacker.attackDamage * 0.2f * usageCount);
        
        int totalDamage = baseDamage + bonusDamage;
        
        return Mathf.Max(1, totalDamage);
    }
    
    public override void Execute(Unit attacker, Unit target)
    {
        if (target == null) return;
        
        // Increment usage count before calculating damage
        usageCount++;
        
        // Calculate total damage including usage bonus
        int totalDamage = CalculateDamage(attacker);
        int baseDamage = base.CalculateDamage(attacker);
        int bonusDamage = totalDamage - baseDamage;
        
        target.TakeDamage(totalDamage);
        Debug.Log($"Repeated Strikes (use #{usageCount}) deals {totalDamage} damage ({baseDamage} base + {bonusDamage} from repetition)!");
        
        // Spawn VFX if we have one
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, target.transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }
        
        // Apply all effects
        foreach (var effect in effects) {
            effect.ApplyEffect(attacker, target);
        }
    }
    
    // Method to reset the counter (useful for new battles or game sessions)
    public static void ResetCounter()
    {
        usageCount = 0;
    }
    
    // Method to get current usage count (useful for UI display)
    public static int GetUsageCount()
    {
        return usageCount;
    }
} 