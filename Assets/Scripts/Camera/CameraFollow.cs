using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // The player or unit to follow
    
    [Header("Follow Settings")]
    public float smoothSpeed = 5f; // Higher value = faster camera
    public Vector3 offset = new Vector3(0, 0, -10); // Default for 2D games
    
    [Header("Bounds")]
    public bool limitBounds = false;
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -10f;
    public float maxY = 10f;
    
    [Header("Auto-Target")]
    [Tooltip("If true, will automatically find the player unit")]
    public bool autoFindPlayer = true;
    
    [Header("Camera Drag")]
    public bool enableDragging = true;
    public float dragSpeed = 2.0f;
    private bool isDragging = false;
    private Vector3 dragOrigin;
    
    [Header("Zoom Settings")]
    public bool enableZoom = true;
    public float zoomSpeed = 2.0f;
    public float minZoom = 2.0f;
    public float maxZoom = 15.0f;
    private Camera mainCamera;
    
    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }
    
    private void Start()
    {
        // Try to find the player at startup
        if (autoFindPlayer && target == null && !enableDragging)
        {
            FindAndSetPlayerTarget();
        }
    }
    
    private void Update()
    {
        if (enableDragging)
        {
            HandleDragging();
        }
        
        if (enableZoom)
        {
            HandleZoom();
        }
    }
    
    private void HandleZoom()
    {
        if (mainCamera == null)
            return;
            
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        // Only process if there's actual scroll input
        if (scrollInput != 0)
        {
            // Calculate new orthographic size
            float newSize = mainCamera.orthographicSize - scrollInput * zoomSpeed;
            
            // Clamp between min and max zoom levels
            mainCamera.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
    
    private void HandleDragging()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isDragging = true;
            dragOrigin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        
        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }
        
        if (isDragging)
        {
            Vector3 currentPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 difference = dragOrigin - currentPosition;
            
            // Move the camera by the difference
            transform.position += difference;
            
            // Apply bounds limitation if enabled
            if (limitBounds)
            {
                Vector3 position = transform.position;
                position.x = Mathf.Clamp(position.x, minX, maxX);
                position.y = Mathf.Clamp(position.y, minY, maxY);
                transform.position = position;
            }
        }
    }
    
    private void LateUpdate()
    {
        // Skip following if dragging is enabled
        if (enableDragging)
            return;
        
        // If we don't have a target, try to find the player
        if (target == null && autoFindPlayer)
        {
            FindAndSetPlayerTarget();
        }
        
        // Don't update if we don't have a target
        if (target == null)
            return;
            
        // Calculate the desired position (target position + offset)
        Vector3 desiredPosition = target.position + offset;
        
        // Smoothly move the camera toward that position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // Apply bounds limitation if enabled
        if (limitBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        }
        
        // Update the camera position
        transform.position = smoothedPosition;
    }
    
    // Find the player unit and set it as the target
    private void FindAndSetPlayerTarget()
    {
        // Try to get player unit
        Unit playerUnit = FindPlayerUnit();
        if (playerUnit != null)
        {
            target = playerUnit.transform;
        }
    }
    
    // Find the player unit
    private Unit FindPlayerUnit()
    {
        Unit[] allUnits = FindObjectsOfType<Unit>();
        
        // Look for a unit with "Player" in the name
        foreach (Unit unit in allUnits)
        {
            if (unit.gameObject.name.Contains("Player"))
            {
                return unit;
            }
        }
        
        // If that fails, try to get the player from GameManager
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.selectedUnit != null)
        {
            return gameManager.selectedUnit;
        }
        
        return null;
    }
    
    // Set a new target for the camera to follow
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    // Set a new unit target for the camera to follow
    public void SetTarget(Unit unit)
    {
        if (unit != null)
        {
            target = unit.transform;
        }
    }
    
    // Center on the player at current position (instant)
    public void CenterOnTarget()
    {
        if (target != null)
        {
            Vector3 targetPos = target.position + offset;
            
            if (limitBounds)
            {
                targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
                targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
            }
            
            transform.position = targetPos;
        }
    }
    
    // Toggle between dragging and following
    public void ToggleDragging()
    {
        enableDragging = !enableDragging;
    }
    
    // Enable or disable zoom
    public void ToggleZoom()
    {
        enableZoom = !enableZoom;
    }
    
    // Reset zoom to default
    public void ResetZoom(float defaultSize = 5f)
    {
        if (mainCamera != null)
        {
            mainCamera.orthographicSize = defaultSize;
        }
    }
    
    // Set the camera bounds based on the grid size
    public void SetBoundsFromGrid(float gridWidth, float gridHeight, float hexRadius)
    {
        // Estimate grid world boundaries
        float worldWidth = gridWidth * hexRadius * 1.5f;
        float worldHeight = gridHeight * hexRadius * ROOT_3;
        
        // Expand slightly to account for hex radius
        minX = -hexRadius;
        maxX = worldWidth + hexRadius;
        minY = -hexRadius;
        maxY = worldHeight + hexRadius;
        
        // Enable bounds
        limitBounds = true;
    }
    
    // The √3 constant used in hexagon calculations
    private const float ROOT_3 = 1.73205080757f;
} 