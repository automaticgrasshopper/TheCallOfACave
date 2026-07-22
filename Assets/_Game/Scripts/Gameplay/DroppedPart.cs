using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Managers;
using TCC.UI;

namespace TCC.Gameplay
{
    /// <summary>An enemy-dropped crafting part that enters the backpack when clicked.</summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class DroppedPart : MonoBehaviour
    {
        private InventoryItemType _type;
        private bool _collected;
        private float _phase;

        public void Init(InventoryItemType type)
        {
            _type = type;
            _phase = Random.value * Mathf.PI * 2f;
            if (_type == InventoryItemType.SpecialEnemyPart)
                GetComponent<SpriteRenderer>().color = new Color(.78f, .46f, 1f, 1f);
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * 4f + _phase) * .06f;
            transform.localScale = Vector3.one * pulse;
        }

        private void OnMouseDown()
        {
            if (_collected || !InventoryManager.Exists) return;
            if (GameManager.Exists && GameManager.Instance.State != GameState.Playing) return;
            _collected = true;
            InventoryManager.Instance.Add(_type);
            ToastView.Instance?.Key(_type == InventoryItemType.SpecialEnemyPart
                ? LocalizationTable.Keys.ToastSpecialPartPickedUp
                : LocalizationTable.Keys.ToastBasicPartPickedUp);
            Destroy(gameObject);
        }
    }
}
