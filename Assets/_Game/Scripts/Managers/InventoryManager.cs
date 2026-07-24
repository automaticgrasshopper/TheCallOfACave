using System;
using System.Collections.Generic;
using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Gameplay;
using TCC.Persistence;
using TCC.UI;

namespace TCC.Managers
{
    /// <summary>Single source of truth for all stackable player-owned cargo.</summary>
    public class InventoryManager : Singleton<InventoryManager>
    {
        private readonly int[] _counts = new int[Enum.GetValues(typeof(InventoryItemType)).Length];

        public event Action Changed;

        private void Start()
        {
            Changed?.Invoke();
            GameEvents.RaiseInventoryChanged();
        }

        public int Count(InventoryItemType type) => _counts[(int)type];

        public void CaptureStacks(List<InventoryStackSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            foreach (InventoryItemType type in Enum.GetValues(typeof(InventoryItemType)))
                destination.Add(new InventoryStackSnapshot
                {
                    id = "inventory." + type.ToString().ToLowerInvariant(),
                    itemType = type,
                    count = Count(type)
                });
        }

        public void RestoreStacks(IReadOnlyList<InventoryStackSnapshot> stacks)
        {
            Array.Clear(_counts, 0, _counts.Length);
            if (stacks != null)
                foreach (InventoryStackSnapshot stack in stacks)
                    if (stack != null && Enum.IsDefined(typeof(InventoryItemType), stack.itemType))
                        _counts[(int)stack.itemType] = Mathf.Max(0, stack.count);

            Changed?.Invoke();
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseFoodChanged(Count(InventoryItemType.Food));
        }

        public void Add(InventoryItemType type, int amount = 1)
        {
            if (amount <= 0) return;
            _counts[(int)type] += amount;
            Changed?.Invoke();
            GameEvents.RaiseInventoryChanged();
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
            GameEvents.RaiseInventoryChanged();
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
                case InventoryItemType.AdvancedEquipment: return cfg.advancedEquipmentSaleValue;
                default: return 0;
            }
        }

        public bool TryCraftEliteEquipment()
        {
            if (Count(InventoryItemType.AdvancedPartA) < 1 ||
                Count(InventoryItemType.AdvancedPartB) < 1)
            {
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastCraftNeedParts);
                return false;
            }

            _counts[(int)InventoryItemType.AdvancedPartA]--;
            _counts[(int)InventoryItemType.AdvancedPartB]--;
            _counts[(int)InventoryItemType.EliteEquipment]++;
            Changed?.Invoke();
            GameEvents.RaiseInventoryChanged();
            ToastView.Instance?.Key(LocalizationTable.Keys.ToastEquipmentCrafted);
            return true;
        }

        public bool TryCraftAdvancedEquipment()
        {
            if (Count(InventoryItemType.EliteEquipment) < 1 ||
                Count(InventoryItemType.SpecialEnemyPart) < 1)
            {
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastCraftNeedAdvancedParts);
                return false;
            }

            _counts[(int)InventoryItemType.EliteEquipment]--;
            _counts[(int)InventoryItemType.SpecialEnemyPart]--;
            _counts[(int)InventoryItemType.AdvancedEquipment]++;
            Changed?.Invoke();
            GameEvents.RaiseInventoryChanged();
            ToastView.Instance?.Key(LocalizationTable.Keys.ToastAdvancedEquipmentCrafted);
            return true;
        }
    }
}
