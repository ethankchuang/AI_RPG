using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class UIDebugger : MonoBehaviour
{
    public TextMeshProUGUI debugText;
    
    private void Update()
    {
        if (debugText == null)
            return;
            
        // Display whether the pointer is over UI
        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        debugText.text = $"Pointer over UI: {isOverUI}";
        
        // Change color based on state
        debugText.color = isOverUI ? Color.green : Color.red;
        
        // Log clicks
        if (Input.GetMouseButtonDown(0))
        {
    
        }
    }
} 