using UnityEngine;

public enum EnemyType
{
    Light,
    Medium,
    Heavy
}

[CreateAssetMenu(fileName = "New Enemy", menuName = "Characters/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Enemy Info")]
    public EnemyType enemyType;
    public string enemyName;
    [TextArea(3, 5)]
    public string description;
    public Sprite enemySprite;
    
    [Header("Base Stats")]
    public int maxHealth = 20;
    public int attackDamage = 5;
    public int speed = 15;
    public int movementRange = 3;
    
    [Header("Visual")]
    public Color enemyColor = Color.red;
    
    [Header("Special Abilities")]
    public AttackSO specialAttack;      // Optional special attack
    [Range(0.0f, 1.0f)]
    public float specialAttackChance = 0.2f;  // Chance to use special attack
    
    // Note: All units are spawned at z = -1 to ensure they render in front of the map tiles (which are at z = 0)
} 