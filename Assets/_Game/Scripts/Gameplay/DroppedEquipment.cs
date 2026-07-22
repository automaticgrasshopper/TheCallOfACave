using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Managers;
using TCC.UI;

namespace TCC.Gameplay
{
    /// <summary>An intentionally inert equipped-item drop; clicking returns it to the backpack.</summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class DroppedEquipment : MonoBehaviour
    {
        private float _phase;
        private bool _collected;
        private InventoryItemType _type = InventoryItemType.EliteEquipment;

        public void Init(InventoryItemType type)
        {
            _type = type;
            _phase = Random.value * Mathf.PI * 2f;
            if (_type == InventoryItemType.AdvancedEquipment)
                GetComponent<SpriteRenderer>().color = new Color(.72f, .5f, 1f, 1f);
        }

        private void Update()
        {
            transform.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Sin(Time.time * 2.5f + _phase) * 4f);
        }

        private void OnMouseDown()
        {
            if (_collected || !InventoryManager.Exists) return;
            if (GameManager.Exists && GameManager.Instance.State != GameState.Playing) return;
            _collected = true;
            InventoryManager.Instance.Add(_type);
            ToastView.Instance?.Key(LocalizationTable.Keys.ToastEquipmentPickedUp);
            Destroy(gameObject);
        }
    }
}
