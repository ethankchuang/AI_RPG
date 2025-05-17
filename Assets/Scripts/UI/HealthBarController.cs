using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarController : MonoBehaviour
{
    [Header("UI Components")]
    public Slider healthSlider;
    public Image fillImage;
    public TextMeshProUGUI healthText;
    
    [Header("Colors")]
    public Color fullHealthColor = Color.green;
    public Color mediumHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public float lowHealthThreshold = 0.3f;  // 30% health
    public float mediumHealthThreshold = 0.6f;  // 60% health
    
    [Header("Settings")]
    public bool showHealthText = true;
    public bool followTarget = true;
    public Vector3 offset = new Vector3(0, 1.2f, 0);
    
    private Unit targetUnit;
    private Camera mainCamera;
    private RectTransform rectTransform;
    
    public void Initialize(Unit unit)
    {
        targetUnit = unit;
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
        
        if (targetUnit != null)
        {
            UpdateHealthBar();
        }
    }
    
    private void Start()
    {
        if (healthText != null)
        {
            healthText.gameObject.SetActive(showHealthText);
        }
    }
    
    private void LateUpdate()
    {
        if (targetUnit == null) 
            return;
            
        UpdateHealthBar();
        
        if (followTarget)
        {
            UpdatePosition();
        }
    }
    
    private void UpdateHealthBar()
    {
        // Update slider value
        if (healthSlider != null)
        {
            healthSlider.maxValue = targetUnit.maxHealth;
            healthSlider.value = targetUnit.currentHealth;
        }
        
        // Update health text
        if (healthText != null && showHealthText)
        {
            healthText.text = $"CURRENT HP: {targetUnit.currentHealth}/{targetUnit.maxHealth}";
        }
        
        // Update fill color based on health percentage
        if (fillImage != null)
        {
            float healthPercent = (float)targetUnit.currentHealth / targetUnit.maxHealth;
            
            if (healthPercent <= lowHealthThreshold)
            {
                fillImage.color = lowHealthColor;
            }
            else if (healthPercent <= mediumHealthThreshold)
            {
                float t = (healthPercent - lowHealthThreshold) / (mediumHealthThreshold - lowHealthThreshold);
                fillImage.color = Color.Lerp(lowHealthColor, mediumHealthColor, t);
            }
            else
            {
                float t = (healthPercent - mediumHealthThreshold) / (1f - mediumHealthThreshold);
                fillImage.color = Color.Lerp(mediumHealthColor, fullHealthColor, t);
            }
        }
    }
    
    private void UpdatePosition()
    {
        if (mainCamera == null || targetUnit == null)
            return;
            
        // Convert world position to screen position
        Vector3 screenPos = mainCamera.WorldToScreenPoint(targetUnit.transform.position + offset);
        
        // Don't show if behind camera
        if (screenPos.z < 0)
        {
            healthSlider.gameObject.SetActive(false);
            return;
        }
        
        // Make visible and update position
        healthSlider.gameObject.SetActive(true);
        transform.position = screenPos;
    }
} 