using UnityEngine;

[System.Serializable]
public abstract class StatusEffect
{
    [Header("Effect Properties")]
    public string effectName;
    public string description;
    public int duration; // Number of actions/turns remaining
    public bool stackable = false; // Can multiple instances exist?
    
    [Header("Visual")]
    public Color effectColor = Color.white;
    public Sprite effectIcon;
    
    // Called when the effect is first applied
    public virtual void OnApply(Unit target)
    {
        Debug.Log($"{effectName} applied to {target.name} for {duration} actions");
    }
    
    // Called at the start of each action/turn while active
    public virtual void OnActionStart(Unit target)
    {
        duration--;
        if (duration <= 0)
        {
            OnRemove(target);
        }
    }
    
    // Called when the effect expires or is removed
    public virtual void OnRemove(Unit target)
    {
        Debug.Log($"{effectName} removed from {target.name}");
    }
    
    // Called every frame while active (for continuous effects)
    public virtual void OnUpdate(Unit target)
    {
        // Override in subclasses for effects that need constant updates
    }
    
    // Check if this effect has expired
    public bool IsExpired()
    {
        return duration <= 0;
    }
    
    // Get a copy of this effect (for applying to multiple targets)
    public abstract StatusEffect Clone();
} 