using UnityEngine;

public class CameraFollow : MonoBehaviour
{
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
    
    [Header("Bounds")]
    public bool limitBounds = false;
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -10f;
    public float maxY = 10f;
    
    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
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