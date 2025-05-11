using UnityEngine;

public class HexGridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public GameObject hexTilePrefab;
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float hexRadius = 1f; // Distance from center to corner
    // Mathematical constants
    private const float ROOT_3 = 1.73205080757f; // √3

    void Start()
    {
        GenerateGrid();
    }
    
    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        // Clear existing grid if any
        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);
            
        // For flat-topped hexagons:
        // Width = 2 * radius
        // Height = sqrt(3) * radius
        float hexWidth = hexRadius * 2f;
        float hexHeight = hexRadius * ROOT_3;
        
        // For exactly flush hexagons, the horizontal distance between centers 
        // should be 1.5 * radius for flat-top hexagons
        float xSpacing = hexRadius * 1.5f;
        float ySpacing = hexHeight;
        
        for (int col = 0; col < gridWidth; col++)
        {
            for (int row = 0; row < gridHeight; row++)
            {
                // Calculate position for flat-top hexagons with vertical stacking
                float xPos;
                float yPos;
                
                // For flat-top hexes stacked vertically, we offset every other column horizontally
                if (col % 2 == 1)
                {
                    xPos = col * xSpacing;
                    yPos = row * ySpacing + (ySpacing * 0.5f);
                }
                else
                {
                    xPos = col * xSpacing;
                    yPos = row * ySpacing;
                }
                
                // Create the hex tile
                Vector3 position = new Vector3(xPos, yPos, 0);
                GameObject tile = Instantiate(hexTilePrefab, position, Quaternion.identity, transform);
                
                // Set tile properties and name
                tile.name = $"Hex_{col}_{row}";
                
                // Get and set the HexTile component
                HexTile hexTile = tile.GetComponent<HexTile>();
                if (hexTile != null)
                {
                    hexTile.Initialize(col, row);
                }
            }
        }
    }
    
#if UNITY_EDITOR
    // Visualize the grid in the editor
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && hexTilePrefab != null)
        {
            // Calculations for flat-topped hexagons
            float hexWidth = hexRadius * 2f;
            float hexHeight = hexRadius * ROOT_3;
            
            float xSpacing = hexRadius * 1.5f;
            float ySpacing = hexHeight;
            
            Gizmos.color = Color.yellow;
            
            for (int col = 0; col < gridWidth; col++)
            {
                for (int row = 0; row < gridHeight; row++)
                {
                    float xPos;
                    float yPos;
                    
                    if (col % 2 == 1)
                    {
                        xPos = col * xSpacing;
                        yPos = row * ySpacing + (ySpacing * 0.5f);
                    }
                    else
                    {
                        xPos = col * xSpacing;
                        yPos = row * ySpacing;
                    }
                    
                    Vector3 position = new Vector3(xPos, yPos, 0);
                    Gizmos.DrawWireSphere(position, 0.1f);
                    
                    // Draw hex outline
                    DrawHexGizmo(position);
                }
            }
        }
    }
    
    // Draw a hexagon gizmo at the given position
    private void DrawHexGizmo(Vector3 center)
    {
        Vector3[] corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            // For flat-top hexes, angles start at right (0) and go counterclockwise
            float angle = (Mathf.PI / 3f) * i;
            float x = center.x + hexRadius * Mathf.Cos(angle);
            float y = center.y + hexRadius * Mathf.Sin(angle);
            corners[i] = new Vector3(x, y, center.z);
        }
        
        for (int i = 0; i < 6; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 6]);
        }
    }
#endif
}

