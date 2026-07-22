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
    /// <summary>Drag source for one placeable facility in the RTS build palette.</summary>
    public class BuildCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private FacilityType _type;
        [SerializeField] private string _labelKey;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;

        private bool _dragging;

        private void OnEnable()
        {
            GameEvents.MoneyChanged += OnMoneyChanged;
            GameEvents.LanguageChanged += OnLanguageChanged;
        }

        private void Start()
        {
            if (BuildingPlacementManager.Exists)
                BuildingPlacementManager.Instance.AvailabilityChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.MoneyChanged -= OnMoneyChanged;
            GameEvents.LanguageChanged -= OnLanguageChanged;
            if (BuildingPlacementManager.Exists)
                BuildingPlacementManager.Instance.AvailabilityChanged -= Refresh;
        }

        public void Configure(FacilityType type, string labelKey, TMP_Text label, Image background, Image icon)
        {
            _type = type;
            _labelKey = labelKey;
            _label = label;
            _background = background;
            _icon = icon;
            Refresh();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = BuildingPlacementManager.Exists &&
                BuildingPlacementManager.Instance.BeginPlacement(_type, eventData.position);
            Refresh();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragging && BuildingPlacementManager.Exists)
                BuildingPlacementManager.Instance.UpdatePlacement(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragging && BuildingPlacementManager.Exists)
                BuildingPlacementManager.Instance.FinishPlacement(eventData.position);
            _dragging = false;
            Refresh();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (BuildingPlacementManager.Exists &&
                !BuildingPlacementManager.Instance.IsUnlocked(_type))
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastBuildLocked);
            else
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastDragToBuild);
        }

        private void OnMoneyChanged(int _) => Refresh();
        private void OnLanguageChanged(Language _) => Refresh();

        private void Refresh()
        {
            if (!BuildingPlacementManager.Exists) return;
            int cost = BuildingPlacementManager.Instance.BuildCost(_type);
            bool unlocked = BuildingPlacementManager.Instance.IsUnlocked(_type);
            bool available = BuildingPlacementManager.Instance.CanBuild(_type) &&
                EconomyManager.Exists && EconomyManager.Instance.CanAfford(cost);
            if (_label != null && LocalizationManager.Exists)
                _label.text = unlocked
                    ? string.Format(LocalizationManager.Instance.Get(_labelKey), cost)
                    : LocalizationManager.Instance.Get(LocalizationTable.Keys.BuildLocked);
            if (_background != null)
                _background.color = available ? new Color(.045f, .12f, .12f, .98f)
                    : new Color(.035f, .04f, .045f, .86f);
            if (_icon != null)
                _icon.color = available ? Color.white
                    : unlocked ? new Color(.52f, .55f, .52f, .52f)
                    : new Color(.24f, .28f, .28f, .3f);
        }
    }
}
