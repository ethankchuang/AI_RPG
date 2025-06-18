using UnityEngine;

// Specific taunt effect that increases aggro value
[System.Serializable]
public class TauntEffect : StatusEffect
{
    [Header("Taunt Properties")]
    public int aggroIncrease = 5;
    
    private int originalAggro = 0;
    
    public TauntEffect()
    {
        effectName = "Taunted";
        description = "Increased likelihood of being targeted by enemies";
        effectColor = Color.red;
        stackable = false;
    }
    
    public TauntEffect(int aggroBonus, int actionDuration)
    {
        effectName = "Taunted";
        description = $"Increased likelihood of being targeted by enemies (+{aggroBonus} aggro)";
        effectColor = Color.red;
        stackable = false;
        aggroIncrease = aggroBonus;
        duration = actionDuration;
    }
    
    public override void OnApply(Unit target)
    {
        base.OnApply(target);
        
        if (target is Player player)
        {
            originalAggro = player.aggroValue;
            player.aggroValue += aggroIncrease;
            Debug.Log($"{player.name} aggro increased from {originalAggro} to {player.aggroValue}");
        }
    }
    
    public override void OnRemove(Unit target)
    {
        base.OnRemove(target);
        
        if (target is Player player)
        {
            player.aggroValue = originalAggro;
            Debug.Log($"{player.name} aggro restored to {originalAggro}");
        }
    }
    
    public override StatusEffect Clone()
    {
        return new TauntEffect(aggroIncrease, duration);
    }
} 