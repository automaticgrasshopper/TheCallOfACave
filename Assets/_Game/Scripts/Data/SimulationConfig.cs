using UnityEngine;

namespace TCC.Data
{
    /// <summary>
    /// All tunable numbers for the living colony. Creatures are cave bugs that are
    /// born from the nursery, wander the left activity area, age through their whole
    /// life on a timer (no hunger), and — as prime adults — lay eggs. Everything is
    /// read from here so balancing happens in the Inspector during "play-and-tune".
    /// </summary>
    [CreateAssetMenu(menuName = "TCC/Simulation Config", fileName = "SimulationConfig")]
    public class SimulationConfig : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed = 0.9f;
        [Tooltip("How often a wandering bug picks a new drift direction.")]
        public float wanderChangeInterval = 2.4f;

        [Header("Lifespan (seconds) & stage split")]
        [Tooltip("Total life is rolled uniformly in this range on birth.")]
        public float lifespanMin = 160f;
        public float lifespanMax = 200f;
        [Tooltip("Fraction of the whole life spent as a juvenile.")]
        [Range(0f, 1f)] public float juvenileFraction = 0.2f;
        [Tooltip("Fraction spent as a prime adult. Elder gets the remainder.")]
        [Range(0f, 1f)] public float primeFraction = 0.6f;

        [Header("Breeding")]
        [Tooltip("A prime adult lays one egg every this many seconds.")]
        public float eggLayIntervalSeconds = 20f;
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
        public int startAdults = 3;
    }
}
