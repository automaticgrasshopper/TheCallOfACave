using UnityEngine;
using TCC.Gameplay;

namespace TCC.Data
{
    /// <summary>
    /// All tunable numbers for the living colony. Creatures are cave bugs that are
    /// born from the nursery, wander the whole cave, age through their whole life,
    /// and — as unassigned adults — lay eggs. Everything is
    /// read from here so balancing happens in the Inspector during "play-and-tune".
    /// </summary>
    [CreateAssetMenu(menuName = "TCC/Simulation Config", fileName = "SimulationConfig")]
    public class SimulationConfig : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed = 1f;
        [Tooltip("How often a wandering bug picks a new drift direction.")]
        public float wanderChangeInterval = 1.8f;

        [Header("Life and health")]
        [Tooltip("Each bug rolls a permanent lifespan inside this range when born.")]
        public float lifespanMinSeconds = 160f;
        public float totalLifespanSeconds = 180f;
        public float juvenileSeconds = 30f;
        public float elderStartSeconds = 150f;
        public float healthMax = 100f;
        public float healthLossInterval = 2.5f;
        public float healthLossPerTick = 1f;
        public float foodHealing = 35f;
        public float hungerLossPerSecond = 0.55f;
        public float foodHungerRestore = 55f;
        public float foodSenseRadius = 4f;
        public float criticalHealth = 20f;
        public float criticalAgeMultiplier = 2f;
        public float infectedHealthMultiplier = 2f;
        public float infectedAgeMultiplier = 2f;

        [Header("Breeding")]
        [Tooltip("A prime adult lays one egg every this many seconds.")]
        public float eggLayIntervalSeconds = 60f;
        public float firstEggMinSeconds = 35f;
        [Tooltip("Each egg rolls a permanent incubation duration inside this range.")]
        public float eggHatchMinSeconds = 20f;
        public float eggHatchMaxSeconds = 40f;
        [Tooltip("Cap on eggs on screen.")]
        public int maxEggs = 30;
        [Tooltip("Cap on live creatures.")]
        public int maxCreatures = 40;

        [Header("Individual variation")]
        [Range(0.5f, 1.5f)] public float productionEfficiencyMin = 0.9f;
        [Range(0.5f, 1.5f)] public float productionEfficiencyMax = 1.1f;

        [Header("Stage scale (world units; ~0.68 ≈ 68px)")]
        public float infantScale = 0.52f;
        public float primeScale = 0.68f;
        public float elderScale = 0.62f;

        [Header("Initial population")]
        public int startInfants = 3;
        public int startAdults = 0;
        public int startEggs = 2;

        [Header("Soldier training")]
        [Tooltip("Training represents two colony-years in the prototype time scale.")]
        public float soldierTrainingSeconds = 10f;
        [Tooltip("A completed soldier has this fraction of its pre-training remaining life.")]
        [Range(0.1f, 1f)] public float soldierRemainingLifeMultiplier = 0.5f;

        [Header("Facilities and production")]
        [Tooltip("World grid size used while placing facilities.")]
        public float buildingGridSize = 0.5f;
        [Tooltip("Square half-extent reserved at placement for the level-3 footprint.")]
        public float buildingReservedHalfExtent = 2.2f;
        public float facilityLevel1Radius = 1.15f;
        public float facilityLevel2Radius = 1.5f;
        public float facilityLevel3Radius = 1.9f;
        [Tooltip("Placement-center bounds for the visible world left of the command sidebar.")]
        public Vector2 buildingAreaMin = new Vector2(-9.5f, -5f);
        public Vector2 buildingAreaMax = new Vector2(3.5f, 5f);
        [Tooltip("Level 1 makes basic parts, level 2 makes intermediate parts, level 3 randomly makes one of two advanced parts.")]
        public float factoryLevel1Interval = 8f;
        public float factoryLevel2Interval = 7f;
        public float factoryLevel3Interval = 12f;
        public int factoryLevel1SaleValue = 80;
        public int factoryLevel2SaleValue = 130;
        public int factoryLevel3SaleValue = 240;
        public int advancedEquipmentSaleValue = 480;
        public float hospitalTreatmentSeconds = 5f;
        public float hospitalCombatHealPerSecond = 8f;
        public float academyTrainingSeconds = 10f;
        public float contaminationCleanSeconds = 1f;

        [Header("Combat")]
        public float firstInvasionSeconds = 90f;
        public float invasionWaveInterval = 45f;
        public float heavyEnemyStartSeconds = 300f;
        public float soldierMaxHealth = 100f;
        public float soldierDefense = 3f;
        public float enemyMaxHealth = 110f;
        public float heavyEnemyMaxHealth = 330f;
        public float soldierDamage = 10f;
        public float enemyDamage = 11f;
        public float heavyEnemyDamage = 22f;
        public float attackInterval = 0.75f;
        [Tooltip("Untrained adults deal one fifth of a soldier's damage.")]
        public float civilianDamageDivisor = 5f;
        [Header("Facility durability")]
        public float facilityLevel1MaxHealth = 100f;
        public float facilityLevel2MaxHealth = 120f;
        public float facilityLevel3MaxHealth = 300f;
        [Tooltip("Fraction of maximum durability restored per second while no enemy targets the facility.")]
        public float facilityRegenPercentPerSecond = 0.01f;
        public int enemyFacilityDamageMin = 5;
        public int enemyFacilityDamageMax = 15;
        public int heavyEnemyFacilityDamageMin = 10;
        public int heavyEnemyFacilityDamageMax = 30;
        public float combatAgeMultiplier = 1.5f;
        public float eliteSoldierMultiplier = 2f;
        [Tooltip("Advanced equipment is 150% stronger than a normal soldier, for 250% total attack.")]
        public float advancedSoldierMultiplier = 2.5f;

        public float FactoryInterval(int level)
            => level >= 3 ? factoryLevel3Interval : level == 2 ? factoryLevel2Interval : factoryLevel1Interval;

        public InventoryItemType FactoryItem(int level)
            => level >= 3 ? (Random.value < .5f ? InventoryItemType.AdvancedPartA : InventoryItemType.AdvancedPartB)
                : level == 2 ? InventoryItemType.RefinedComponent
                : InventoryItemType.MetalScrap;

        public float FacilityRadius(int level)
            => level >= 3 ? facilityLevel3Radius : level == 2 ? facilityLevel2Radius : facilityLevel1Radius;

        public float FacilityMaxHealth(int level)
            => level >= 3 ? facilityLevel3MaxHealth
                : level == 2 ? facilityLevel2MaxHealth
                : level == 1 ? facilityLevel1MaxHealth : 0f;
    }
}
