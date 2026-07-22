using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TCC.Core;
using TCC.Data;
using TCC.Gameplay;
using TCC.Managers;

namespace TCC.UI
{
    public class InventorySlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _count;
        [SerializeField] private Image _background;

        private InventoryItemType _type;
        private bool _occupied;
        private RectTransform _dragGhost;

        public void Configure(Image icon, TMP_Text count, Image background)
        {
            _icon = icon;
            _count = count;
            _background = background;
        }

        public void SetItem(InventoryItemType type, int count, Sprite icon, bool occupied)
        {
            _type = type;
            _occupied = occupied;
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = occupied && icon != null;
                _icon.color = type == InventoryItemType.AdvancedPartA
                    ? new Color(.42f, .88f, 1f, 1f)
                    : type == InventoryItemType.AdvancedPartB
                        ? new Color(1f, .68f, .3f, 1f)
                        : type == InventoryItemType.SpecialEnemyPart || type == InventoryItemType.AdvancedEquipment
                            ? new Color(.78f, .48f, 1f, 1f) : Color.white;
            }
            if (_count != null) _count.text = occupied ? count.ToString() : string.Empty;
            if (_background != null)
                _background.color = occupied ? new Color(.055f, .085f, .09f, .98f) : new Color(.025f, .035f, .04f, .78f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_occupied || !InventoryManager.Exists) return;
            if (_type == InventoryItemType.Food)
            {
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastDragFood);
                return;
            }
            if (_type == InventoryItemType.EliteEquipment || _type == InventoryItemType.AdvancedEquipment)
            {
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastDragEquipment);
                return;
            }
            if (_type == InventoryItemType.SpecialEnemyPart)
            {
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastCraftNeedAdvancedParts);
                return;
            }
            if (_type == InventoryItemType.AdvancedPartA || _type == InventoryItemType.AdvancedPartB)
            {
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastCraftNeedParts);
                return;
            }
            InventoryManager.Instance.TrySellOne(_type);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_occupied || (_type != InventoryItemType.Food &&
                _type != InventoryItemType.EliteEquipment &&
                _type != InventoryItemType.AdvancedEquipment) || _icon == null)
                return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var go = new GameObject("Cargo Drag Ghost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            _dragGhost = (RectTransform)go.transform;
            _dragGhost.sizeDelta = new Vector2(64f, 64f);
            var image = go.GetComponent<Image>();
            image.sprite = _icon.sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, .9f);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragGhost != null) _dragGhost.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragGhost != null) Destroy(_dragGhost.gameObject);
            _dragGhost = null;
            if (!_occupied || !InventoryManager.Exists || Camera.main == null) return;

            if ((_type == InventoryItemType.EliteEquipment || _type == InventoryItemType.AdvancedEquipment) &&
                MoneyDropTarget.ContainsScreenPoint(eventData.position))
            {
                InventoryManager.Instance.TrySellOne(_type);
                return;
            }

            Vector3 screen = new Vector3(eventData.position.x, eventData.position.y,
                -Camera.main.transform.position.z);
            Vector2 world = Camera.main.ScreenToWorldPoint(screen);
            if (_type == InventoryItemType.Food)
            {
                if (SimulationManager.Exists && SimulationManager.Instance.SpawnFood(world))
                    InventoryManager.Instance.TryRemove(_type);
                return;
            }
            var hits = Physics2D.OverlapPointAll(world);
            Creature target = null;
            foreach (var hit in hits)
            {
                target = hit.GetComponent<Creature>();
                if (target != null) break;
            }
            if (target != null)
            {
                bool applied = _type == InventoryItemType.AdvancedEquipment
                    ? target.TryEquipAdvanced() : target.TryEquipElite();
                if (applied) InventoryManager.Instance.TryRemove(_type);
                return;
            }

            if ((_type == InventoryItemType.EliteEquipment || _type == InventoryItemType.AdvancedEquipment) &&
                (eventData.pointerCurrentRaycast.gameObject == null ||
                 !EventSystem.current.IsPointerOverGameObject(eventData.pointerId)) &&
                SimulationManager.Exists && SimulationManager.Instance.SpawnLooseEquipment(world, _type))
            {
                InventoryManager.Instance.TryRemove(_type);
            }
        }
    }
}
