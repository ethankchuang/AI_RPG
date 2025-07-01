using UnityEngine;

// Status effect that increases speed
[System.Serializable]
public class SpeedBoostEffect : StatusEffect
{
    [Header("Speed Boost Properties")]
    public int speedIncrease = 10; // Speed bonus to apply
    
    private int originalSpeed = 0;
    
    public SpeedBoostEffect()
    {
        effectName = "Speed Boost";
        description = "Increases speed by 10";
        effectColor = Color.yellow;
        stackable = false;
        duration = 3; // Lasts for 3 turns
    }
    
    public SpeedBoostEffect(int speedBonus, int actionDuration)
    {
        effectName = "Speed Boost";
        description = $"Increases speed by {speedBonus}";
        effectColor = Color.yellow;
        stackable = false;
        speedIncrease = speedBonus;
        duration = actionDuration;
    }
    
    public override void OnApply(Unit target)
    {
        base.OnApply(target);
        
        // Store original speed and apply boost
        originalSpeed = target.speed;
        target.speed += speedIncrease;
        
        Debug.Log($"{target.name} speed increased from {originalSpeed} to {target.speed} for {duration} turns");
    }
    
    public override void OnRemove(Unit target)
    {
        base.OnRemove(target);
        
        // Restore original speed
        target.speed = originalSpeed;
        
        Debug.Log($"{target.name} speed restored to {originalSpeed}");
    }
    
    public override StatusEffect Clone()
    {
        return new SpeedBoostEffect(speedIncrease, duration);
    }
} 