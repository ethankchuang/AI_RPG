using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base attack class
[CreateAssetMenu(menuName = "Combat/Attack", fileName = "NewAttack")]
public class AttackSO : ScriptableObject
{
    [Header("Core Properties")]
    public string attackName = "New Attack";
    [TextArea] public string description;
    [Range(0.1f, 5.0f)]
    public float damageMultiplier = 1.0f;  // Multiplier for character's attack stat
    public int baseDamage = 0;  // Flat damage added to scaled damage
    public int range = 1;
    public int SPCost = 0;  // Skill Point cost, 0 for basic attacks
    
    [Header("Targeting")]
    public bool requiresTarget = true;  // If false, executes immediately without target selection
    
    [Header("Visuals")]
    public Sprite icon;
    public GameObject vfxPrefab;  // Visual effect prefab to spawn
    
    // List of effects to apply
    public List<AttackEffectSO> effects = new List<AttackEffectSO>();
    
    // Called when the attack is executed with a target
    public virtual void Execute(Unit attacker, Unit target)
    {
        if (target == null) return;
        
        // Calculate scaled damage
        int scaledDamage = CalculateDamage(attacker);
        
        // Apply damage
        target.TakeDamage(scaledDamage);
        
        // Spawn VFX if we have one
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, target.transform.position, Quaternion.identity);
            Destroy(vfx, 2f); // Clean up after 2 seconds
        }
        
        // Apply all effects
        foreach (var effect in effects) {
            effect.ApplyEffect(attacker, target);
        }
    }
    
    // Calculate damage based on attacker's stats
    protected virtual int CalculateDamage(Unit attacker)
    {
        if (attacker == null) return baseDamage;
        
        // Scale damage based on attacker's attack stat
        float scaledDamage = attacker.attackDamage * damageMultiplier;
        int totalDamage = Mathf.RoundToInt(scaledDamage) + baseDamage;
        
        return Mathf.Max(1, totalDamage); // Ensure minimum 1 damage
    }
    
    // Called when the attack is executed without a target (self-targeting or area effects)
    public virtual void ExecuteNonTargeted(Unit attacker)
    {
        // For non-targeted attacks, apply effects to the attacker by default
        // Child classes can override this for different behavior (area effects, etc.)
        
        // Spawn VFX at attacker position if we have one
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, attacker.transform.position, Quaternion.identity);
            Destroy(vfx, 2f); // Clean up after 2 seconds
        }
        
        // Apply all effects to the attacker (for buffs, heals, etc.)
        foreach (var effect in effects) {
            effect.ApplyEffect(attacker, attacker);
        }
    }
}

// Effect base class
public abstract class AttackEffectSO : ScriptableObject 
{
    public abstract void ApplyEffect(Unit source, Unit target);
}
