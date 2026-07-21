using System;
using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Gameplay;
using TCC.UI;

namespace TCC.Managers
{
    /// <summary>Single source of truth for all stackable player-owned cargo.</summary>
    public class InventoryManager : Singleton<InventoryManager>
    {
        private readonly int[] _counts = new int[Enum.GetValues(typeof(InventoryItemType)).Length];

        public event Action Changed;

        private void Start() => Changed?.Invoke();

        public int Count(InventoryItemType type) => _counts[(int)type];

        public void Add(InventoryItemType type, int amount = 1)
        {
            if (amount <= 0) return;
            _counts[(int)type] += amount;
            Changed?.Invoke();
            if (type == InventoryItemType.Food)
                GameEvents.RaiseFoodChanged(Count(InventoryItemType.Food));
        }

        public bool TryRemove(InventoryItemType type, int amount = 1)
        {
            if (amount <= 0) return true;
            int index = (int)type;
            if (_counts[index] < amount) return false;
            _counts[index] -= amount;
            Changed?.Invoke();
            if (type == InventoryItemType.Food)
                GameEvents.RaiseFoodChanged(Count(InventoryItemType.Food));
            return true;
        }

        public bool TrySellOne(InventoryItemType type)
        {
            int value = SaleValue(type);
            if (value <= 0 || !TryRemove(type)) return false;
            GameEvents.RaiseMoneyEarned(value);
            if (ToastView.Exists && LocalizationManager.Exists)
                ToastView.Instance.Message(string.Format(
                    LocalizationManager.Instance.Get(LocalizationTable.Keys.ToastCargoSold), value));
            return true;
        }

        public int SaleValue(InventoryItemType type)
        {
            var cfg = SimulationManager.Exists ? SimulationManager.Instance.Config : null;
            if (cfg == null) return 0;
            switch (type)
            {
                case InventoryItemType.MetalScrap: return cfg.factoryLevel1SaleValue;
                case InventoryItemType.RefinedComponent: return cfg.factoryLevel2SaleValue;
                case InventoryItemType.EliteEquipment: return cfg.factoryLevel3SaleValue;
                default: return 0;
            }
        }
    }
}
