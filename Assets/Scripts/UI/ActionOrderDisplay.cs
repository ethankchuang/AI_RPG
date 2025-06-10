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

        // Get all units and sort by action value (highest first)
        List<Unit> units = gameManager.GetAllUnits();
        units = units.OrderByDescending(u => u.actionValue).ToList();

        // Update each card
        for (int i = 0; i < cardSprites.Count; i++)
        {
            Image cardSprite = cardSprites[i];
            if (cardSprite == null)
                continue;

            // If we have a unit for this position, display its sprite
            if (i < units.Count && units[i] != null)
            {
                Unit unit = units[i];
                SpriteRenderer unitSpriteRenderer = unit.GetComponentInChildren<SpriteRenderer>();
                if (unitSpriteRenderer != null && unitSpriteRenderer.sprite != null)
                {
                    cardSprite.sprite = unitSpriteRenderer.sprite;
                    cardSprite.enabled = true;
                    
                    // Make the current active unit's card more prominent
                    if (unit == Unit.ActiveUnit)
                    {
                        cardSprite.color = new Color(1, 1, 1, 1); // Full opacity for current unit
                    }
                    else
                    {
                        // Fade based on action value
                        float normalizedValue = unit.actionValue / 100f;
                        cardSprite.color = new Color(1, 1, 1, 0.5f + (normalizedValue * 0.5f));
                    }
                }
            }
            else
            {
                // No unit for this position, make sprite transparent
                cardSprite.sprite = null;
                cardSprite.color = new Color(1, 1, 1, 0.5f);
            }
        }
    }
}
