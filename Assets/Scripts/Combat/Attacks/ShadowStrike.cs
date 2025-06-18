using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attacks/Shadow Strike", fileName = "ShadowStrike")]
public class ShadowStrike : AttackSO
{
    private void OnEnable()
    {
        // Set default values for shadow strike
        attackName = "Shadow Strike";
        description = "A stealthy attack that scales with your attack stat and deals more damage the more health you have lost.";
        damageMultiplier = 1.2f;  // 120% of attack stat
        baseDamage = 0;
        range = 1;
        SPCost = 1;
    }

    protected override int CalculateDamage(Unit attacker)
    {
        // Get base scaled damage
        int baseDamage = base.CalculateDamage(attacker);
        
        // Calculate extra damage based on attacker's missing health
        int missingHealth = attacker.maxHealth - attacker.currentHealth;
        int bonusDamage = missingHealth / 2; // Half of missing health as bonus damage
        
        int totalDamage = baseDamage + bonusDamage;
        
        return Mathf.Max(1, totalDamage);
    }
    
    public override void Execute(Unit attacker, Unit target)
    {
        if (target == null) return;
        
        // Calculate total damage including missing health bonus
        int totalDamage = CalculateDamage(attacker);
        int baseDamage = base.CalculateDamage(attacker);
        int bonusDamage = totalDamage - baseDamage;
        
        target.TakeDamage(totalDamage);
        Debug.Log($"Shadow Strike deals {totalDamage} damage ({baseDamage} base + {bonusDamage} from missing health)!");
        
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
} 