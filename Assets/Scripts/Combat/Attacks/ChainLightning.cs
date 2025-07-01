using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Combat/Attacks/Chain Lightning", fileName = "ChainLightning")]
public class ChainLightning : AttackSO
{
    [Header("Chain Lightning Properties")]
    [Range(1, 5)]
    public int maxBounces = 3;  // Maximum number of bounces
    [Range(0.1f, 1.0f)]
    public float damageIncreasePerBounce = 0.3f;  // 30% damage increase per bounce
    
    private void OnEnable()
    {
        // Set default values for chain lightning
        attackName = "Chain Lightning";
        description = "Lightning that arcs between adjacent enemies, growing stronger with each bounce.";
        damageMultiplier = 0.9f;  // 90% of attack stat (starts moderate)
        baseDamage = 0;
        range = 2;  // Can target enemies within 2 tiles
        SPCost = 2;  // Higher cost due to area effect
    }

    public override void Execute(Unit attacker, Unit target)
    {
        if (target == null) return;
        
        Debug.Log($"{attacker.name} casts Chain Lightning!");
        
        // Track which units have been hit to prevent infinite loops
        HashSet<Unit> hitUnits = new HashSet<Unit>();
        List<Unit> chainOrder = new List<Unit>();
        
        // Start the chain with the initial target
        Unit currentTarget = target;
        int bounceCount = 0;
        
        while (currentTarget != null && bounceCount < maxBounces && !hitUnits.Contains(currentTarget))
        {
            // Add to hit list and chain order
            hitUnits.Add(currentTarget);
            chainOrder.Add(currentTarget);
            
            // Calculate damage for this bounce (increases with each bounce)
            int bounceDamage = CalculateBounceDamage(attacker, bounceCount);
            
            // Deal damage
            currentTarget.TakeDamage(bounceDamage);
            Debug.Log($"Chain Lightning bounces to {currentTarget.name} (bounce #{bounceCount + 1}) for {bounceDamage} damage!");
            
            // Spawn VFX at current target
            if (vfxPrefab != null)
            {
                GameObject vfx = Instantiate(vfxPrefab, currentTarget.transform.position, Quaternion.identity);
                Destroy(vfx, 1f);
            }
            
            // Find next target (adjacent enemy)
            currentTarget = FindNextChainTarget(currentTarget, hitUnits);
            bounceCount++;
        }
        
        // Apply all effects to the initial target
        foreach (var effect in effects) {
            effect.ApplyEffect(attacker, target);
        }
        
        Debug.Log($"Chain Lightning completed after {bounceCount} bounces!");
    }
    
    private int CalculateBounceDamage(Unit attacker, int bounceCount)
    {
        // Base damage
        int baseDamage = Mathf.RoundToInt(attacker.attackDamage * damageMultiplier);
        
        // Add damage increase for each bounce
        float bounceMultiplier = 1f + (damageIncreasePerBounce * bounceCount);
        int totalDamage = Mathf.RoundToInt(baseDamage * bounceMultiplier);
        
        return Mathf.Max(1, totalDamage);
    }
    
    private Unit FindNextChainTarget(Unit currentUnit, HashSet<Unit> hitUnits)
    {
        // Get the hex grid manager
        HexGridManager gridManager = FindObjectOfType<HexGridManager>();
        if (gridManager == null) return null;
        
        // Get current unit's tile
        HexTile currentTile = currentUnit.CurrentTile;
        if (currentTile == null) return null;
        
        // Check all adjacent tiles for enemies
        List<Unit> adjacentEnemies = new List<Unit>();
        
        foreach (HexTile neighbor in currentTile.neighbors)
        {
            if (neighbor == null) continue;
            
            Unit unitOnTile = gridManager.GetUnitOnTile(neighbor);
            if (unitOnTile != null && unitOnTile is Enemy && !hitUnits.Contains(unitOnTile))
            {
                adjacentEnemies.Add(unitOnTile);
            }
        }
        
        // If no adjacent enemies, try to find any enemy within 2 tiles
        if (adjacentEnemies.Count == 0)
        {
            foreach (HexTile neighbor in currentTile.neighbors)
            {
                if (neighbor == null) continue;
                
                foreach (HexTile secondNeighbor in neighbor.neighbors)
                {
                    if (secondNeighbor == null) continue;
                    
                    Unit unitOnTile = gridManager.GetUnitOnTile(secondNeighbor);
                    if (unitOnTile != null && unitOnTile is Enemy && !hitUnits.Contains(unitOnTile))
                    {
                        adjacentEnemies.Add(unitOnTile);
                    }
                }
            }
        }
        
        // Return the first available enemy, or null if none found
        return adjacentEnemies.Count > 0 ? adjacentEnemies[0] : null;
    }
} 