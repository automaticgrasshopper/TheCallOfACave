using UnityEngine;

namespace TCC.Data
{
    /// <summary>Three-minute economy tuning. Income comes from manually collected
    /// factory parts; spending competes across population, food, facilities and roles.</summary>
    [CreateAssetMenu(menuName = "TCC/Economy Config", fileName = "EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [Tooltip("Coins the player starts with.")]
        public int startMoney = 300;

        [Header("Eggs")]
        [Tooltip("Coins earned when an unhatched egg is clicked and sold.")]
        public int eggSellValue = 100;

        // Legacy scene-bootstrap compatibility; the old passive LaborZone is
        // disabled by ScenePresentationBaker and pays nothing in the live game.
        [HideInInspector] public int laborIncomePerSec = 0;

        [Header("Buying")]
        [Tooltip("Coins to buy one juvenile into the nursery.")]
        public int buyJuvenileCost = 120;
        public int buyFoodCost = 30;
        public int factoryBuildCost = 100;
        public int barracksBuildCost = 250;
        public int hospitalBuildCost = 400;
        public int academyBuildCost = 700;

        [Header("Facility upgrades: level 1 -> 2 -> 3")]
        public int factoryUpgradeLevel2 = 350;
        public int factoryUpgradeLevel3 = 650;
        public int barracksUpgradeLevel2 = 250;
        public int barracksUpgradeLevel3 = 450;
        public int hospitalUpgradeLevel2 = 250;
        public int hospitalUpgradeLevel3 = 450;
        public int academyUpgradeLevel2 = 900;
        public int academyUpgradeLevel3 = 1300;

        [Header("Medical doctors: per doctor")]
        public int doctorTrainingCoins = 150;
        public int doctorTrainingFood = 3;
    }
}
