using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ActionOrderDisplay : MonoBehaviour
{
    [Header("References")]
    public Transform cardContainer; // Container holding the 6 cards
    public Sprite cardBackground; // Background sprite for the cards

    private List<Image> cardBackgrounds = new List<Image>();
    private List<Image> cardSprites = new List<Image>();
    private GameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        // Find GameManager
        gameManager = GameManager.Instance;
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (cardBackground == null)
            return;

        // Get all card images from the container
        foreach (Transform child in cardContainer)
        {
            // Get the background image (should be on a child object named "Background")
            Transform backgroundObj = child.Find("Background");
            if (backgroundObj != null)
            {
                Image backgroundImage = backgroundObj.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    cardBackgrounds.Add(backgroundImage);
                    // Set the background sprite
                    backgroundImage.sprite = cardBackground;
                    backgroundImage.enabled = true;
                    backgroundImage.color = Color.white;
                }
            }

            // Get the sprite image (should be on a child object named "UnitSprite")
            Transform spriteObj = child.Find("UnitSprite");
            if (spriteObj != null)
            {
                Image spriteImage = spriteObj.GetComponent<Image>();
                if (spriteImage != null)
                {
                    cardSprites.Add(spriteImage);
                    // Make sure the sprite image is visible
                    spriteImage.enabled = true;
                    spriteImage.color = Color.white;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Update the display every frame to reflect current action values
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (gameManager == null)
            return;

        // Get all units and predict the next 6 turns
        List<Unit> units = gameManager.GetAllUnits();
        List<Unit> turnOrder = PredictTurnOrder(units, 6);

        // Update each card
        for (int i = 0; i < cardSprites.Count; i++)
        {
            Image cardSprite = cardSprites[i];
            if (cardSprite == null)
                continue;

            // If we have a unit for this position, display its sprite
            if (i < turnOrder.Count && turnOrder[i] != null)
            {
                Unit unit = turnOrder[i];
                SpriteRenderer unitSpriteRenderer = unit.GetComponentInChildren<SpriteRenderer>();
                if (unitSpriteRenderer != null && unitSpriteRenderer.sprite != null)
                {
                    cardSprite.sprite = unitSpriteRenderer.sprite;
                    cardSprite.enabled = true;
                    
                    // Use the unit's actual color with full opacity
                    Color unitColor = unitSpriteRenderer.color;
                    cardSprite.color = new Color(unitColor.r, unitColor.g, unitColor.b, 1f);
                }
            }
            else
            {
                // No unit for this position, hide the sprite
                cardSprite.sprite = null;
                cardSprite.enabled = false;
            }
        }
    }

    private List<Unit> PredictTurnOrder(List<Unit> allUnits, int turnsToPredict)
    {
        List<Unit> turnOrder = new List<Unit>();
        
        // Create a copy of units with their current action values for simulation
        List<UnitActionData> simulatedUnits = new List<UnitActionData>();
        foreach (Unit unit in allUnits)
        {
            if (unit != null)
            {
                simulatedUnits.Add(new UnitActionData
                {
                    unit = unit,
                    actionValue = unit.actionValue,
                    speed = unit.speed,
                    priority = unit.priority
                });
            }
        }

        // Simulate the next turns
        for (int turn = 0; turn < turnsToPredict && simulatedUnits.Count > 0; turn++)
        {
            // Find the minimum action value needed to reach threshold
            int leastActionNeeded = int.MaxValue;
            
            foreach (UnitActionData unitData in simulatedUnits)
            {
                int threshold = 100 - unitData.speed;
                int actionNeeded = threshold - unitData.actionValue;
                
                if (actionNeeded < leastActionNeeded)
                {
                    leastActionNeeded = actionNeeded;
                }
            }
            
            // Find all units that need the least action (tied units)
            List<UnitActionData> tiedUnits = new List<UnitActionData>();
            foreach (UnitActionData unitData in simulatedUnits)
            {
                int threshold = 100 - unitData.speed;
                int actionNeeded = threshold - unitData.actionValue;
                
                if (actionNeeded == leastActionNeeded)
                {
                    tiedUnits.Add(unitData);
                }
            }
            
            // Use priority to break ties
            UnitActionData nextUnit = null;
            int highestPriority = int.MinValue;
            
            foreach (UnitActionData unitData in tiedUnits)
            {
                if (unitData.priority > highestPriority)
                {
                    highestPriority = unitData.priority;
                    nextUnit = unitData;
                }
            }
            
            if (nextUnit == null) break;
            
            // Add this unit to the turn order
            turnOrder.Add(nextUnit.unit);
            
            // Reset the active unit's action value to 0
            nextUnit.actionValue = 0;
            
            // Distribute the action value to all other units
            foreach (UnitActionData unitData in simulatedUnits)
            {
                if (unitData != nextUnit)
                {
                    unitData.actionValue += leastActionNeeded;
                }
            }
        }

        return turnOrder;
    }

    // Helper class for simulating action values
    private class UnitActionData
    {
        public Unit unit;
        public int actionValue;
        public int speed;
        public int priority;
    }
}
