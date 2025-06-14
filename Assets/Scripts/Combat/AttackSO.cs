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
    public int damage = 10;
    public int range = 1;
    public int SPCost = 0;  // Skill Point cost, 0 for basic attacks
    
    [Header("Visuals")]
    public Sprite icon;
    public GameObject vfxPrefab;  // Visual effect prefab to spawn
    
    // List of effects to apply
    public List<AttackEffectSO> effects = new List<AttackEffectSO>();
    
    // Called when the attack is executed
    public virtual void Execute(Unit attacker, Unit target)
    {
        if (target == null) return;
        
        // Apply damage
        target.TakeDamage(damage);
        
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
}

// Effect base class
public abstract class AttackEffectSO : ScriptableObject 
{
    public abstract void ApplyEffect(Unit source, Unit target);
}
