using UnityEngine;

// Status effect that reduces damage taken
[System.Serializable]
public class DamageReductionEffect : StatusEffect
{
    [Header("Damage Reduction Properties")]
    public float damageReductionPercent = 0.4f; // 40% damage reduction
    
    public DamageReductionEffect()
    {
        effectName = "Damage Reduction";
        description = "Reduces damage taken by 40%";
        effectColor = Color.blue;
        stackable = false;
        duration = 1; // Lasts for 1 turn
    }
    
    public DamageReductionEffect(float reductionPercent, int actionDuration)
    {
        effectName = "Damage Reduction";
        description = $"Reduces damage taken by {reductionPercent * 100}%";
        effectColor = Color.blue;
        stackable = false;
        damageReductionPercent = reductionPercent;
        duration = actionDuration;
    }
    
    public override void OnApply(Unit target)
    {
        base.OnApply(target);
        
        Debug.Log($"{target.name} damage reduction applied: {damageReductionPercent * 100}% reduction for {duration} turn(s)");
    }
    
    public override void OnRemove(Unit target)
    {
        base.OnRemove(target);
        
        Debug.Log($"{target.name} damage reduction removed");
    }
    
    // Override TakeDamage to apply damage reduction
    public int ApplyDamageReduction(int originalDamage)
    {
        int reducedDamage = Mathf.CeilToInt(originalDamage * (1.0f - damageReductionPercent));
        return Mathf.Max(1, reducedDamage); // Ensure minimum 1 damage
    }
    
    public override StatusEffect Clone()
    {
        return new DamageReductionEffect(damageReductionPercent, duration);
    }
} 