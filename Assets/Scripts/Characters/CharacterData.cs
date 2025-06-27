using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Characters/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Character Info")]
    public CharacterType characterType;
    public string characterName;
    [TextArea(3, 5)]
    public string description;
    public Sprite characterSprite;
    
    [Header("Base Stats")]
    public int maxHealth = 25;
    public int attackDamage = 5;
    public int speed = 20;
    public int movementRange = 5;
    
    [Header("Attacks")]
    public AttackSO basicAttack;
    public AttackSO skill1;
    public AttackSO skill2;
    
    [Header("Visual")]
    public Color characterColor = Color.white;
    
    [Header("Aggro System")]
    [Tooltip("How likely enemies are to target this character (higher = more likely to be targeted)")]
    public int aggroValue = 1;
    
    // Note: All units are spawned at z = -1 to ensure they render in front of the map tiles (which are at z = 0)
} 