using UnityEngine;

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
        public float totalLifespanSeconds = 180f;
        public float juvenileSeconds = 30f;
        public float elderStartSeconds = 150f;
        public float healthMax = 100f;
        public float healthLossInterval = 2.5f;
        public float healthLossPerTick = 1f;
        public float foodHealing = 35f;
        public float criticalHealth = 20f;
        public float criticalAgeMultiplier = 2f;
        public float infectedHealthMultiplier = 2f;
        public float infectedAgeMultiplier = 2f;

        [Header("Breeding")]
        [Tooltip("A prime adult lays one egg every this many seconds.")]
        public float eggLayIntervalSeconds = 60f;
        public float firstEggMinSeconds = 35f;
        [Tooltip("An unsold egg hatches into a juvenile after this long.")]
        public float eggHatchSeconds = 15f;
        [Tooltip("Cap on eggs on screen.")]
        public int maxEggs = 30;
        [Tooltip("Cap on live creatures.")]
        public int maxCreatures = 40;

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
        public float factoryPartInterval = 8f;
        public int factoryPartValue = 80;
        public float hospitalTreatmentSeconds = 5f;
        public float hospitalCombatHealPerSecond = 8f;
        public float academyTrainingSeconds = 10f;
        public float contaminationCleanSeconds = 1f;

        [Header("Combat")]
        public float firstInvasionSeconds = 90f;
        public float soldierMaxHealth = 100f;
        public float enemyMaxHealth = 110f;
        public float soldierDamage = 10f;
        public float enemyDamage = 11f;
        public float attackInterval = 0.75f;
        public float combatAgeMultiplier = 1.5f;
    }
}
