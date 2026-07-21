using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
            if (InventoryManager.Exists) InventoryManager.Instance.Changed += Refresh;
            Refresh();
        }

        private void Start()
        {
            if (InventoryManager.Exists)
            {
                InventoryManager.Instance.Changed -= Refresh;
                InventoryManager.Instance.Changed += Refresh;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (InventoryManager.Exists) InventoryManager.Instance.Changed -= Refresh;
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
                _slots[i]?.SetItem(type, count, icon, occupied);
            }
        }
    }
}
