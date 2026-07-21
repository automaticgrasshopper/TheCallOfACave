using UnityEngine;

namespace TCC.Data
{
    /// <summary>Money-side tuning. Income comes from three taps (a slow passive
    /// drip, prime adults working the labor circle, and selling eggs); the only
    /// sink is buying new juveniles to keep the colony from aging out.</summary>
    [CreateAssetMenu(menuName = "TCC/Economy Config", fileName = "EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [Tooltip("Coins the player starts with.")]
        public int startMoney = 300;

        [Header("Passive income")]
        [Tooltip("Coins granted every interval, regardless of anything else.")]
        public int passiveIncome = 0;
        public float passiveIntervalSeconds = 10f;

        [Header("Labor")]
        [Tooltip("Coins per second earned by each prime adult inside the labor circle.")]
        public int laborIncomePerSec = 2;

        [Header("Eggs & buying")]
        [Tooltip("Coins earned when an egg is clicked (sold) instead of left to hatch.")]
        public int eggSellValue = 0;
        [Tooltip("Coins to buy one juvenile into the nursery.")]
        public int buyJuvenileCost = 120;
        public int buyFoodCost = 50;
        public int factoryBuildCost = 100;
        public int barracksBuildCost = 200;
        public int hospitalBuildCost = 500;
        public int academyBuildCost = 1000;
        public int doctorTrainingCoins = 300;
        public int doctorTrainingFood = 5;
    }
}
