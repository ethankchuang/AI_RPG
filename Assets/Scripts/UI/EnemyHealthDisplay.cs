using UnityEngine;
using TMPro;

public class EnemyHealthDisplay : MonoBehaviour
{
    [Header("Health Display Settings")]
    public Vector3 offset = new Vector3(0, 0.8f, 0);
    public Color fullHealthColor = Color.green;
    public Color mediumHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public float lowHealthThreshold = 0.3f;  // 30% health
    public float mediumHealthThreshold = 0.6f;  // 60% health
    
    private TextMeshPro healthText;
    private Unit targetUnit;
    private Camera mainCamera;
    
    public void Initialize(Unit unit)
    {
        targetUnit = unit;
        mainCamera = Camera.main;
        
        // Create the text component
        CreateHealthText();
        
        if (targetUnit != null)
        {
            UpdateHealthDisplay();
        }
    }
    
    private void CreateHealthText()
    {
        // Create a new GameObject for the text
        GameObject textObject = new GameObject("HealthText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = offset;
        
        // Add TextMeshPro component
        healthText = textObject.AddComponent<TextMeshPro>();
        
        // Configure the text
        healthText.text = "100/100";
        healthText.fontSize = 3f;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.sortingOrder = 10; // Make sure it renders on top
        
        // Set initial color
        healthText.color = fullHealthColor;
    }
    
    private void LateUpdate()
    {
        if (targetUnit == null || healthText == null)
            return;
            
        UpdateHealthDisplay();
        UpdateFacing();
    }
    
    private void UpdateHealthDisplay()
    {
        // Safety check for valid health values
        if (targetUnit.maxHealth <= 0)
            return;
            
        // Ensure current health is not negative
        int currentHP = Mathf.Max(0, targetUnit.currentHealth);
        
        // Update text content
        healthText.text = $"{currentHP}/{targetUnit.maxHealth}";
        
        // Update color based on health percentage
        float healthPercent = (float)currentHP / targetUnit.maxHealth;
        
        if (healthPercent <= lowHealthThreshold)
        {
            healthText.color = lowHealthColor;
        }
        else if (healthPercent <= mediumHealthThreshold)
        {
            float t = (healthPercent - lowHealthThreshold) / (mediumHealthThreshold - lowHealthThreshold);
            healthText.color = Color.Lerp(lowHealthColor, mediumHealthColor, t);
        }
        else
        {
            float t = (healthPercent - mediumHealthThreshold) / (1f - mediumHealthThreshold);
            healthText.color = Color.Lerp(mediumHealthColor, fullHealthColor, t);
        }
    }
    
    private void UpdateFacing()
    {
        // Make the text always face the camera
        if (mainCamera != null && healthText != null)
        {
            healthText.transform.LookAt(mainCamera.transform);
            healthText.transform.Rotate(0, 180, 0); // Flip to face camera correctly
        }
    }
    
    private void OnDestroy()
    {
        if (healthText != null)
        {
            Destroy(healthText.gameObject);
        }
    }
} 