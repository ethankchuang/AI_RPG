using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public enum StoryLocation
{
    Town,
    Wilderness,
    Desert,
    Mountains,
    DestroyedTown,
    Portal
}

[System.Serializable]
public enum StoryPhase
{
    Combat,
    Campfire,
    Transition
}

[System.Serializable]
public class StoryBeat
{
    public string id;
    public string title;
    public string description;
    public StoryLocation location;
    public StoryPhase phase;
    public string[] choices;
    public string[] consequences;
    public bool isCompleted = false;
    public int requiredActionPoints = 0;
    
    public StoryBeat(string id, string title, string description, StoryLocation location, StoryPhase phase)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.location = location;
        this.phase = phase;
        this.choices = new string[0];
        this.consequences = new string[0];
    }
}

[System.Serializable]
public class CompanionRelationship
{
    public CharacterType companionType;
    public int relationshipLevel; // -100 to 100
    public string currentStatus; // "Friendly", "Neutral", "Hostile", etc.
    
    public CompanionRelationship(CharacterType type)
    {
        companionType = type;
        relationshipLevel = 0;
        currentStatus = "Neutral";
    }
}

[System.Serializable]
public class PlayerStats
{
    public int actionPoints = 3;
    public int gold = 100;
    public List<string> inventory = new List<string>();
    public List<string> statusEffects = new List<string>();
    
    public void ResetActionPoints()
    {
        actionPoints = 3;
    }
}

public class StoryManager : MonoBehaviour
{
    [Header("Story Configuration")]
    private static StoryLocation currentLocation = StoryLocation.Town;
    private static StoryPhase currentPhase = StoryPhase.Transition;
    private static int currentStoryBeatIndex = 0;
    
    [Header("Player State")]
    [SerializeField] private PlayerStats playerStats = new PlayerStats();
    [SerializeField] private List<CompanionRelationship> companionRelationships = new List<CompanionRelationship>();
    
    [Header("Story Beats")]
    private static List<StoryBeat> storyBeats = new List<StoryBeat>();
    
    // Events
    public static event Action<StoryBeat> OnStoryBeatStarted;
    public static event Action<StoryBeat> OnStoryBeatCompleted;
    public static event Action<StoryLocation> OnLocationChanged;
    public static event Action<StoryPhase> OnPhaseChanged;
    public static event Action<CompanionRelationship> OnRelationshipChanged;
    
    // Singleton
    public static StoryManager Instance { get; private set; }
    
    private void Awake()
    {
        Debug.Log("StoryManager.Awake() called");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("StoryManager instance set, initializing story");
            
            // Only initialize story if it hasn't been initialized yet
            if (storyBeats.Count == 0)
            {
                InitializeStory();
            }
            else
            {
                Debug.Log($"Story already initialized with {storyBeats.Count} beats, current index: {currentStoryBeatIndex}");
            }
        }
        else
        {
            Debug.Log("StoryManager instance already exists, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    private void InitializeStory()
    {
        Debug.Log("StoryManager.InitializeStory() called");
        InitializeCompanionRelationships();
        CreateStoryBeats();
        Debug.Log($"Story initialization complete. Total story beats: {storyBeats.Count}");
    }
    
    private void InitializeCompanionRelationships()
    {
        companionRelationships.Clear();
        companionRelationships.Add(new CompanionRelationship(CharacterType.Paladin));
        companionRelationships.Add(new CompanionRelationship(CharacterType.Rogue));
        companionRelationships.Add(new CompanionRelationship(CharacterType.Mage));
        companionRelationships.Add(new CompanionRelationship(CharacterType.Warrior));
    }
    
    private void CreateStoryBeats()
    {
        Debug.Log("StoryManager.CreateStoryBeats() called");
        storyBeats.Clear();
        
        // ===== TOWN CHAPTER =====
        
        // TOWN - Bar Scene
        var barScene = new StoryBeat("town_bar", "The Tavern", "You enter the bustling tavern and see your companions gathered around a table. The Paladin sits with a noble bearing, the Rogue leans back with a mysterious smile, the Mage studies a glowing crystal, and the Warrior sharpens his blade. They welcome you warmly as you join their table.", StoryLocation.Town, StoryPhase.Transition);
        storyBeats.Add(barScene);
        
        // TOWN - Campfire
        var townCampfire = new StoryBeat("town_campfire", "Evening Campfire", "After the bar incident, you set up camp outside town. The fire crackles as your companions share stories and discuss the journey ahead. The Paladin speaks of honor and duty, the Rogue shares tales of adventure, the Mage discusses ancient magic, and the Warrior talks of battles past.", StoryLocation.Town, StoryPhase.Campfire);
        storyBeats.Add(townCampfire);
        
        // TOWN - Bar Fight (Combat)
        var barFight = new StoryBeat("town_bar_fight", "Bar Brawl", "A fight breaks out! The stranger seems to be the cause of the commotion.", StoryLocation.Town, StoryPhase.Combat);
        storyBeats.Add(barFight);
        
        // TOWN - King's Quest Assignment
        var kingQuestAssignment = new StoryBeat("town_king_assignment", "The King's Request", "A mysterious figure approaches you with news of the king's urgent request for heroes to investigate the growing monster threat.", StoryLocation.Town, StoryPhase.Transition);
        storyBeats.Add(kingQuestAssignment);
        
        // TOWN - Campfire
        var townCampfire2 = new StoryBeat("town_campfire2", "Pre-Quest Campfire", "You discuss the mysterious figure's words and prepare for your meeting with the king.", StoryLocation.Town, StoryPhase.Campfire);
        storyBeats.Add(townCampfire2);
        
        // TOWN - Receive Quest from King
        var kingQuest = new StoryBeat("town_king", "The King's Quest", "The next morning, you meet with the king who tells you about the monsters appearing throughout the land and the ancient relic in the mountains that could defeat them. Your companions listen intently, ready to accept this important quest.", StoryLocation.Town, StoryPhase.Transition);
        storyBeats.Add(kingQuest);
        
        // TOWN - Witness Monsters Attacking (Choice)
        var monsterAttack = new StoryBeat("town_monster_attack", "Monsters at the Gate", "As you prepare to leave town, you witness monsters attacking innocent civilians at the gates.", StoryLocation.Town, StoryPhase.Transition);
        monsterAttack.choices = new string[] { "Defend the civilians", "Ignore and continue" };
        storyBeats.Add(monsterAttack);
        
        // TOWN - Store Visit (Choice)
        var townStore = new StoryBeat("town_store", "Town Store", "You visit the local store to gather supplies before your journey. The shopkeeper offers various options for your quest.", StoryLocation.Town, StoryPhase.Transition);
        townStore.choices = new string[] { "Buy healing potions", "Purchase weapons", "Get camping supplies", "Save money" };
        storyBeats.Add(townStore);
        
        // TOWN - Bandit Attack (Combat)
        var banditAttack = new StoryBeat("town_bandits", "Bandit Ambush", "As you gather resources, bandits led by a fierce leader ambush your party!", StoryLocation.Town, StoryPhase.Combat);
        storyBeats.Add(banditAttack);
        
        // TOWN - Campfire
        var townCampfire3 = new StoryBeat("town_campfire3", "Final Town Campfire", "After the bandit attack, you prepare for the journey ahead.", StoryLocation.Town, StoryPhase.Campfire);
        storyBeats.Add(townCampfire3);
        
        // ===== WILDERNESS CHAPTER =====
        
        // WILDERNESS - Travelers Attacked (Choice)
        var travelersAttacked = new StoryBeat("wilderness_travelers_choice", "Attacked Travelers", "You see a group of travelers being attacked by monsters on the road.", StoryLocation.Wilderness, StoryPhase.Transition);
        travelersAttacked.choices = new string[] { "Help the travelers", "Ignore and continue" };
        storyBeats.Add(travelersAttacked);
        
        // WILDERNESS - Traveler Reward
        var travelerReward = new StoryBeat("wilderness_reward", "Traveler's Gratitude", "The grateful travelers offer you a reward. They share food and stories around the fire, grateful for your help.", StoryLocation.Wilderness, StoryPhase.Transition);
        storyBeats.Add(travelerReward);
        
        // WILDERNESS - Orc Ambush (Combat)
        var orcAmbush = new StoryBeat("wilderness_ambush", "Orc Ambush", "Orcs ambush your party in the wilderness.", StoryLocation.Wilderness, StoryPhase.Combat);
        storyBeats.Add(orcAmbush);
        
        // WILDERNESS - Mysterious Figure Clue
        var mysteriousClue = new StoryBeat("wilderness_clue", "Mysterious Figure", "A mysterious figure appears and gives you a clue about the orc camp's location and their plans.", StoryLocation.Wilderness, StoryPhase.Transition);
        storyBeats.Add(mysteriousClue);
        
        // WILDERNESS - Campfire
        var wildernessCampfire = new StoryBeat("wilderness_campfire", "Wilderness Campfire", "After the orc ambush, you discuss the mysterious figure's information and plan your approach to the orc camp.", StoryLocation.Wilderness, StoryPhase.Campfire);
        storyBeats.Add(wildernessCampfire);
        
        // WILDERNESS - Orc Camp Diplomacy (Choice)
        var orcCamp = new StoryBeat("wilderness_orc_camp", "Orc Camp", "You discover the orc camp and can attempt diplomacy.", StoryLocation.Wilderness, StoryPhase.Transition);
        orcCamp.choices = new string[] { "Attempt diplomacy", "Sneak around", "Attack directly", "Observe first" };
        storyBeats.Add(orcCamp);
        
        // WILDERNESS - Orc Camp Battle (Combat)
        var orcCampFight = new StoryBeat("wilderness_orc_camp_fight", "Orc Camp Battle", "The orc camp erupts into battle!", StoryLocation.Wilderness, StoryPhase.Combat);
        storyBeats.Add(orcCampFight);
        
        // WILDERNESS - Campfire
        var wildernessCampfire2 = new StoryBeat("wilderness_campfire2", "Post-Battle Campfire", "After the orc camp battle, you discuss the encounter and your approach.", StoryLocation.Wilderness, StoryPhase.Campfire);
        storyBeats.Add(wildernessCampfire2);
        
        // ===== DESERT CHAPTER =====
        
        // DESERT - Sandstorm
        var sandstorm = new StoryBeat("desert_sandstorm", "Desert Sandstorm", "A fierce sandstorm approaches as you cross the desert. Your team works together to find shelter and protect each other from the harsh elements.", StoryLocation.Desert, StoryPhase.Transition);
        storyBeats.Add(sandstorm);
        
        // DESERT - Scorpion (Combat)
        var scorpion = new StoryBeat("desert_scorpion", "Giant Scorpion", "A massive scorpion emerges from the sand.", StoryLocation.Desert, StoryPhase.Combat);
        storyBeats.Add(scorpion);
        
        // DESERT - Campfire
        var desertCampfire = new StoryBeat("desert_campfire", "Desert Campfire", "After the scorpion battle, you rest and recover.", StoryLocation.Desert, StoryPhase.Campfire);
        storyBeats.Add(desertCampfire);
        
        // DESERT - Oasis (Choice)
        var oasis = new StoryBeat("desert_oasis", "Desert Oasis", "You find a beautiful oasis with limited resources.", StoryLocation.Desert, StoryPhase.Transition);
        oasis.choices = new string[] { "Share equally", "Prioritize injured", "Save for later", "Take extra for yourself" };
        storyBeats.Add(oasis);
        
        // DESERT - Wolves (Combat)
        var wolves = new StoryBeat("desert_wolves", "Wolf Pack", "A pack of wolves approaches, and with limited resources, your companions are weakened.", StoryLocation.Desert, StoryPhase.Combat);
        storyBeats.Add(wolves);
        
        // DESERT - Campfire
        var desertCampfire2 = new StoryBeat("desert_campfire2", "Post-Wolf Campfire", "After the wolf encounter, you discuss the challenges of the desert.", StoryLocation.Desert, StoryPhase.Campfire);
        storyBeats.Add(desertCampfire2);
        
        // DESERT - Temple (Next for now)
        var temple = new StoryBeat("desert_temple", "Ancient Temple", "You discover an ancient temple with puzzles to solve. Your team works together, combining their unique skills to overcome the challenges.", StoryLocation.Desert, StoryPhase.Transition);
        storyBeats.Add(temple);
        
        // DESERT - Sand Elemental (Combat)
        var sandElemental = new StoryBeat("desert_elemental", "Sand Elemental", "A powerful sand elemental emerges from the temple depths.", StoryLocation.Desert, StoryPhase.Combat);
        storyBeats.Add(sandElemental);
        
        // DESERT - Campfire
        var desertCampfire3 = new StoryBeat("desert_campfire3", "Final Desert Campfire", "After the temple encounter, you prepare for the mountain journey.", StoryLocation.Desert, StoryPhase.Campfire);
        storyBeats.Add(desertCampfire3);
        
        // ===== MOUNTAINS CHAPTER =====
        
        // MOUNTAINS - Troll Encounter (Choice)
        var trollEncounter = new StoryBeat("mountains_troll_choice", "Mountain Trolls", "Three trolls block your path up the mountain.", StoryLocation.Mountains, StoryPhase.Transition);
        trollEncounter.choices = new string[] { "Convince them to let you pass", "Fight the trolls" };
        storyBeats.Add(trollEncounter);
        
        // MOUNTAINS - Campfire
        var mountainCampfire = new StoryBeat("mountains_campfire", "Mountain Campfire", "After the troll encounter, you discuss the mountain challenges ahead.", StoryLocation.Mountains, StoryPhase.Campfire);
        storyBeats.Add(mountainCampfire);
        
        // MOUNTAINS - Trapped Passage (Next for now)
        var trappedArea = new StoryBeat("mountains_trapped", "Trapped Passage", "A dangerous trapped area requires teamwork to navigate. Your team works together, combining their skills to safely pass through.", StoryLocation.Mountains, StoryPhase.Transition);
        storyBeats.Add(trappedArea);
        
        // MOUNTAINS - Dual Guards (Combat)
        var dualGuards = new StoryBeat("mountains_guards", "Dual Guards", "Two powerful guards block the path to the artifact room.", StoryLocation.Mountains, StoryPhase.Combat);
        storyBeats.Add(dualGuards);
        
        // MOUNTAINS - Campfire
        var mountainCampfire2 = new StoryBeat("mountains_campfire2", "Pre-Artifact Campfire", "You prepare for the final challenge to reach the artifact.", StoryLocation.Mountains, StoryPhase.Campfire);
        storyBeats.Add(mountainCampfire2);
        
        // MOUNTAINS - Artifact Room (Choice)
        var artifactRoom = new StoryBeat("mountains_artifact", "The Artifact Room", "You reach the artifact room but must make a terrible choice.", StoryLocation.Mountains, StoryPhase.Transition);
        artifactRoom.choices = new string[] { "Sacrifice a companion", "Try to find another way", "Leave without artifact", "Fight the guardian" };
        storyBeats.Add(artifactRoom);
        
        // MOUNTAINS - Dragon (Combat)
        var dragon = new StoryBeat("mountains_dragon", "The Dragon Guardian", "A mighty dragon guards the artifact.", StoryLocation.Mountains, StoryPhase.Combat);
        storyBeats.Add(dragon);
        
        // MOUNTAINS - Campfire
        var mountainCampfire3 = new StoryBeat("mountains_campfire3", "Final Mountain Campfire", "After defeating the dragon, you prepare to return to town with the artifact.", StoryLocation.Mountains, StoryPhase.Campfire);
        storyBeats.Add(mountainCampfire3);
        
        // ===== DESTROYED TOWN CHAPTER =====
        
        // DESTROYED TOWN - Return
        var destroyedTown = new StoryBeat("destroyed_town", "Return to Ruins", "You return to find your town in ruins. The artifact's power has revealed the true nature of the threat.", StoryLocation.DestroyedTown, StoryPhase.Transition);
        storyBeats.Add(destroyedTown);
        
        // DESTROYED TOWN - Mysterious Stranger (Combat)
        var mysteriousStranger = new StoryBeat("destroyed_stranger", "The Mysterious Stranger", "The stranger from the tavern appears with an army of monsters.", StoryLocation.DestroyedTown, StoryPhase.Combat);
        storyBeats.Add(mysteriousStranger);
        
        // DESTROYED TOWN - Campfire
        var destroyedTownCampfire = new StoryBeat("destroyed_town_campfire", "Destroyed Town Campfire", "After the battle, you discuss the stranger's true identity.", StoryLocation.DestroyedTown, StoryPhase.Campfire);
        storyBeats.Add(destroyedTownCampfire);
        
        // ===== PORTAL CHAPTER =====
        
        // PORTAL - Final Battle
        var finalBattle = new StoryBeat("portal_final", "The Dark Magician", "You follow the stranger through a portal to face the true enemy.", StoryLocation.Portal, StoryPhase.Combat);
        storyBeats.Add(finalBattle);
        
        Debug.Log($"CreateStoryBeats() completed. Total story beats created: {storyBeats.Count}");
    }
    
    public StoryBeat GetCurrentStoryBeat()
    {
        if (currentStoryBeatIndex >= 0 && currentStoryBeatIndex < storyBeats.Count)
        {
            return storyBeats[currentStoryBeatIndex];
        }
        return null;
    }
    
    public void StartNextStoryBeat()
    {
        Debug.Log($"StoryManager.StartNextStoryBeat() called. Current index: {currentStoryBeatIndex}, Total beats: {storyBeats.Count}");
        
        if (currentStoryBeatIndex < storyBeats.Count - 1)
        {
            currentStoryBeatIndex++;
            var beat = GetCurrentStoryBeat();
            if (beat != null)
            {
                Debug.Log($"Starting story beat: {beat.title}, Phase: {beat.phase}");
                currentLocation = beat.location;
                currentPhase = beat.phase;
                OnStoryBeatStarted?.Invoke(beat);
                OnLocationChanged?.Invoke(currentLocation);
                OnPhaseChanged?.Invoke(currentPhase);
            }
            else
            {
                Debug.LogError("GetCurrentStoryBeat() returned null after incrementing index!");
            }
        }
        else
        {
            Debug.LogWarning("Cannot start next story beat - already at the end!");
        }
    }
    
    public void CompleteCurrentStoryBeat()
    {
        var beat = GetCurrentStoryBeat();
        if (beat != null)
        {
            beat.isCompleted = true;
            OnStoryBeatCompleted?.Invoke(beat);
        }
    }
    
    public void MakeChoice(int choiceIndex)
    {
        var beat = GetCurrentStoryBeat();
        if (beat != null && beat.choices != null && choiceIndex >= 0 && choiceIndex < beat.choices.Length)
        {
            // Apply consequences based on choice
            ApplyChoiceConsequences(choiceIndex);
            CompleteCurrentStoryBeat();
        }
        else
        {
            // Linear story beat - just complete it
            CompleteCurrentStoryBeat();
        }
    }
    
    private void ApplyChoiceConsequences(int choiceIndex)
    {
        var beat = GetCurrentStoryBeat();
        if (beat == null) return;
        
        // For now, choices don't matter - just continue the story
        // This will be expanded later when choice consequences are implemented
        
        // Apply minimal relationship changes to keep the system working
        switch (beat.id)
        {
            case "town_monster_attack":
                // Defend or ignore - doesn't matter for now
                break;
                
            case "town_store":
                // Store choices - doesn't matter for now
                break;
                
            case "wilderness_travelers_choice":
                // Help or ignore - doesn't matter for now
                break;
                
            case "wilderness_orc_camp":
                // Orc camp approach - doesn't matter for now
                break;
                
            case "desert_oasis":
                // Oasis resource sharing - doesn't matter for now
                break;
                
            case "mountains_troll_choice":
                // Troll encounter - doesn't matter for now
                break;
                
            case "mountains_artifact":
                // Artifact room choice - doesn't matter for now
                break;
        }
    }
    
    public void ModifyRelationship(CharacterType companionType, int change)
    {
        var relationship = companionRelationships.Find(r => r.companionType == companionType);
        if (relationship != null)
        {
            relationship.relationshipLevel = Mathf.Clamp(relationship.relationshipLevel + change, -100, 100);
            UpdateRelationshipStatus(relationship);
            OnRelationshipChanged?.Invoke(relationship);
        }
    }
    
    public void ModifyAllRelationships(int change)
    {
        foreach (var relationship in companionRelationships)
        {
            relationship.relationshipLevel = Mathf.Clamp(relationship.relationshipLevel + change, -100, 100);
            UpdateRelationshipStatus(relationship);
            OnRelationshipChanged?.Invoke(relationship);
        }
    }
    
    private void UpdateRelationshipStatus(CompanionRelationship relationship)
    {
        if (relationship.relationshipLevel >= 50)
            relationship.currentStatus = "Friendly";
        else if (relationship.relationshipLevel >= 20)
            relationship.currentStatus = "Warm";
        else if (relationship.relationshipLevel >= -20)
            relationship.currentStatus = "Neutral";
        else if (relationship.relationshipLevel >= -50)
            relationship.currentStatus = "Cold";
        else
            relationship.currentStatus = "Hostile";
    }
    
    public CompanionRelationship GetCompanionRelationship(CharacterType companionType)
    {
        return companionRelationships.Find(r => r.companionType == companionType);
    }
    
    public void SpendActionPoints(int amount)
    {
        playerStats.actionPoints = Mathf.Max(0, playerStats.actionPoints - amount);
    }
    
    public void ResetActionPoints()
    {
        playerStats.ResetActionPoints();
    }
    
    public int GetActionPoints()
    {
        return playerStats.actionPoints;
    }
    
    public void AddToInventory(string item)
    {
        playerStats.inventory.Add(item);
    }
    
    public void AddStatusEffect(string effect)
    {
        playerStats.statusEffects.Add(effect);
    }
    
    public void RemoveStatusEffect(string effect)
    {
        playerStats.statusEffects.Remove(effect);
    }
    
    public bool IsStoryComplete()
    {
        return currentStoryBeatIndex >= storyBeats.Count - 1 && GetCurrentStoryBeat()?.isCompleted == true;
    }
    
    public StoryLocation GetCurrentLocation()
    {
        return currentLocation;
    }
    
    public StoryPhase GetCurrentPhase()
    {
        return currentPhase;
    }
} 