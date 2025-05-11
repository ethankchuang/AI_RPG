using UnityEngine;

// This class represents the cube coordinate system for hexagons
// The three coordinates (q, r, s) always sum to zero: q + r + s = 0
[System.Serializable]
public struct HexCoordinates
{
    #region Properties
    // Cube coordinates
    public int q; // x axis
    public int r; // y axis
    public int s; // z axis (-q-r)
    #endregion
    
    #region Constructors
    // Constructor
    public HexCoordinates(int q, int r)
    {
        this.q = q;
        this.r = r;
        this.s = -q - r; // Derived from constraint q + r + s = 0
    }
    
    // Constructor for all three coordinates (validation included)
    public HexCoordinates(int q, int r, int s)
    {
        // Validate that q + r + s = 0
        if (q + r + s != 0)
        {
            Debug.LogWarning("Invalid hex coordinates: q + r + s must equal 0");
            // Force into valid form
            this.q = q;
            this.r = r;
            this.s = -q - r;
        }
        else
        {
            this.q = q;
            this.r = r;
            this.s = s;
        }
    }
    #endregion
    
    #region Coordinate Conversion
    // Convert from offset coordinates to cube coordinates for flat-topped hexagons with vertical stacking
    public static HexCoordinates FromOffsetCoordinates(int col, int row)
    {
        int q = col;
        int r = row - (col - (col & 1)) / 2;
        return new HexCoordinates(q, r);
    }
    
    // Convert from cube coordinates to offset coordinates
    public Vector2Int ToOffsetCoordinates()
    {
        int col = q;
        int row = r + (q - (q & 1)) / 2;
        return new Vector2Int(col, row);
    }
    #endregion
    
    #region Distance and Path
    // Calculate distance between two hex coordinates (in cube coordinate system)
    public static int Distance(HexCoordinates a, HexCoordinates b)
    {
        return Mathf.Max(
            Mathf.Abs(a.q - b.q),
            Mathf.Abs(a.r - b.r),
            Mathf.Abs(a.s - b.s)
        );
    }
    
    // Linear interpolation between two hex coordinates
    public static Vector3 Lerp(HexCoordinates a, HexCoordinates b, float t)
    {
        float q = Mathf.Lerp(a.q, b.q, t);
        float r = Mathf.Lerp(a.r, b.r, t);
        float s = Mathf.Lerp(a.s, b.s, t);
        return new Vector3(q, r, s);
    }
    
    // Round a floating point cube coordinate to the nearest hex
    public static HexCoordinates Round(Vector3 cube)
    {
        int q = Mathf.RoundToInt(cube.x);
        int r = Mathf.RoundToInt(cube.y);
        int s = Mathf.RoundToInt(cube.z);
        
        float qDiff = Mathf.Abs(q - cube.x);
        float rDiff = Mathf.Abs(r - cube.y);
        float sDiff = Mathf.Abs(s - cube.z);
        
        // If one of our rounded coordinates is further from the original,
        // we need to recalculate it to maintain the q + r + s = 0 constraint
        if (qDiff > rDiff && qDiff > sDiff)
            q = -r - s;
        else if (rDiff > sDiff)
            r = -q - s;
        else
            s = -q - r;
        
        return new HexCoordinates(q, r, s);
    }
    
    // Line drawing algorithm to find all hexes between two hexes
    public static HexCoordinates[] DrawLine(HexCoordinates a, HexCoordinates b)
    {
        int distance = Distance(a, b);
        
        // Handle the case of identical coordinates
        if (distance == 0)
            return new HexCoordinates[] { a };
        
        HexCoordinates[] results = new HexCoordinates[distance + 1];
        for (int i = 0; i <= distance; i++)
        {
            float t = distance == 0 ? 0 : (float)i / distance;
            results[i] = Round(Lerp(a, b, t));
        }
        
        return results;
    }
    #endregion
    
    #region Overrides
    // Override for string representation
    public override string ToString()
    {
        return $"({q}, {r}, {s})";
    }
    
    // Override for equality comparison
    public override bool Equals(object obj)
    {
        if (!(obj is HexCoordinates))
            return false;
            
        HexCoordinates other = (HexCoordinates)obj;
        return q == other.q && r == other.r && s == other.s;
    }
    
    // Override GetHashCode
    public override int GetHashCode()
    {
        return q.GetHashCode() ^ r.GetHashCode() ^ s.GetHashCode();
    }
    #endregion
} 