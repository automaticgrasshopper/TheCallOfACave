using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TCC.Core;
using TCC.Gameplay;
using TCC.Managers;

namespace TCC.UI
{
    /// <summary>Scene-authored 3-column cargo grid. Items stack, while extra rows scroll.</summary>
    public class InventoryView : MonoBehaviour
    {
        [SerializeField] private InventorySlotView[] _slots;
        [SerializeField] private Sprite[] _icons;

        private void OnEnable()
        {
            GameEvents.InventoryChanged += Refresh;
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.InventoryChanged -= Refresh;
        }

        public void Configure(InventorySlotView[] slots, Sprite[] icons)
        {
            _slots = slots;
            _icons = icons;
            Refresh();
        }

        private void Refresh()
        {
            if (_slots == null) return;
            int itemKinds = Enum.GetValues(typeof(InventoryItemType)).Length;
            for (int i = 0; i < _slots.Length; i++)
            {
                bool occupied = i < itemKinds && InventoryManager.Exists &&
                    InventoryManager.Instance.Count((InventoryItemType)i) > 0;
                InventoryItemType type = i < itemKinds ? (InventoryItemType)i : InventoryItemType.Food;
                int count = occupied ? InventoryManager.Instance.Count(type) : 0;
                Sprite icon = _icons != null && i < _icons.Length ? _icons[i] : null;
                if (icon == null && i < itemKinds) icon = LoadFallbackIcon(type);
                _slots[i]?.SetItem(type, count, icon, occupied);
            }
        }

        private static Sprite LoadFallbackIcon(InventoryItemType type)
        {
            string path = type == InventoryItemType.Food ? "Art/Inventory/food_ration"
                : type == InventoryItemType.MetalScrap ? "Art/Inventory/metal_scrap"
                : type == InventoryItemType.RefinedComponent ? "Art/Inventory/refined_component"
                : type == InventoryItemType.EliteEquipment ? "Art/Inventory/elite_equipment"
                : type == InventoryItemType.AdvancedEquipment ? "Art/Inventory/elite_equipment"
                : "Art/Inventory/refined_component";
            return Resources.Load<Sprite>(path);
        }
    }
}
