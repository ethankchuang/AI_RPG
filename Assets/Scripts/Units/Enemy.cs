using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Unit
{
    protected override void Awake()
    {
        base.Awake();
    }
    
    public override void Start()
    {
        base.Start();
    }
    
    // Explicitly ensure that enemies always show health bars
    /*
    protected override bool ShouldShowHealthBar()
    {
        return true;
    }
    */
    
    // Called at the start of enemy turn
    public override void ResetForNewTurn()
    {
        base.ResetForNewTurn();
    }
    
    // Empty implementation that does nothing
    public void ExecuteTurn()
    {
        // Does nothing - AI will be implemented later
    }
}
