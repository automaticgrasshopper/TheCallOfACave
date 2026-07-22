using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TCC.Core;
using TCC.Data;
using TCC.Gameplay;
using TCC.Managers;

namespace TCC.UI
{
    /// <summary>Turns the three reserved equipment sockets into a two-part crafting station.</summary>
    public class EquipmentSynthesisView : MonoBehaviour
    {
        private TMP_Text _hint;
        private TMP_Text _partALabel;
        private TMP_Text _partBLabel;
        private TMP_Text _craftLabel;
        private TMP_Text _partACount;
        private TMP_Text _partBCount;
        private Image _partAIcon;
        private Image _partBIcon;
        private Button _craftButton;
        private TMP_Text _equipmentLabel;
        private TMP_Text _specialPartLabel;
        private TMP_Text _advancedCraftLabel;
        private TMP_Text _equipmentCount;
        private TMP_Text _specialPartCount;
        private Image _equipmentIcon;
        private Image _specialPartIcon;
        private Button _advancedCraftButton;
        private bool _initialized;

        private void OnEnable()
        {
            EnsureVisuals();
            GameEvents.InventoryChanged += Refresh;
            GameEvents.LanguageChanged += OnLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.InventoryChanged -= Refresh;
            GameEvents.LanguageChanged -= OnLanguageChanged;
        }

        private void EnsureVisuals()
        {
            if (_initialized) return;
            _initialized = true;

            _hint = transform.Find("Equipment Hint")?.GetComponent<TMP_Text>();
            DisableLocalization(_hint);

            var slotA = transform.Find("Armor Module Slot");
            var slotB = transform.Find("Weapon Module Slot");
            var craft = transform.Find("Core Module Slot");
            if (slotA == null || slotB == null || craft == null) return;

            _partALabel = SetupPartSlot(slotA, "Art/Inventory/refined_component",
                new Color(.42f, .88f, 1f, 1f), out _partAIcon, out _partACount);
            _partBLabel = SetupPartSlot(slotB, "Art/Inventory/refined_component",
                new Color(1f, .68f, .3f, 1f), out _partBIcon, out _partBCount);

            var equipmentSlot = Instantiate(slotA.gameObject, transform).transform;
            var specialSlot = Instantiate(slotB.gameObject, transform).transform;
            var advancedCraft = Instantiate(craft.gameObject, transform).transform;
            equipmentSlot.name = "Tier 2 Equipment Slot";
            specialSlot.name = "Tier 2 Special Part Slot";
            advancedCraft.name = "Tier 2 Craft Slot";
            SetRow(equipmentSlot, -112f, 126f);
            SetRow(specialSlot, 0f, 126f);
            SetRow(advancedCraft, 112f, 126f);

            _equipmentLabel = SetupPartSlot(equipmentSlot, "Art/Inventory/elite_equipment",
                new Color(1f, .86f, .42f, 1f), out _equipmentIcon, out _equipmentCount);
            _specialPartLabel = SetupPartSlot(specialSlot, "Art/Inventory/refined_component",
                new Color(.78f, .48f, 1f, 1f), out _specialPartIcon, out _specialPartCount);
            _craftLabel = craft.Find("Label")?.GetComponent<TMP_Text>();
            DisableLocalization(_craftLabel);
            var craftIcon = craft.Find("Socket")?.GetComponent<Image>();
            if (craftIcon != null)
            {
                craftIcon.sprite = Resources.Load<Sprite>("Art/Inventory/elite_equipment");
                craftIcon.color = new Color(1f, .86f, .42f, .9f);
            }
            _craftButton = craft.GetComponent<Button>();
            if (_craftButton == null) _craftButton = craft.gameObject.AddComponent<Button>();
            _craftButton.targetGraphic = craft.GetComponent<Image>();
            _craftButton.onClick.RemoveAllListeners();
            _craftButton.onClick.AddListener(Craft);

            _advancedCraftLabel = advancedCraft.Find("Label")?.GetComponent<TMP_Text>();
            DisableLocalization(_advancedCraftLabel);
            var advancedIcon = advancedCraft.Find("Socket")?.GetComponent<Image>();
            if (advancedIcon != null)
            {
                advancedIcon.sprite = Resources.Load<Sprite>("Art/Inventory/elite_equipment");
                advancedIcon.color = new Color(.78f, .48f, 1f, .9f);
            }
            _advancedCraftButton = advancedCraft.GetComponent<Button>();
            if (_advancedCraftButton == null)
                _advancedCraftButton = advancedCraft.gameObject.AddComponent<Button>();
            _advancedCraftButton.targetGraphic = advancedCraft.GetComponent<Image>();
            _advancedCraftButton.onClick.RemoveAllListeners();
            _advancedCraftButton.onClick.AddListener(CraftAdvanced);
        }

        private static void SetRow(Transform slot, float x, float y)
        {
            var rect = (RectTransform)slot;
            rect.anchoredPosition = new Vector2(x, y);
        }

        private static TMP_Text SetupPartSlot(Transform slot, string resource, Color tint,
            out Image icon, out TMP_Text count)
        {
            icon = slot.Find("Socket")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = Resources.Load<Sprite>(resource);
                icon.color = new Color(tint.r, tint.g, tint.b, .24f);
            }

            var label = slot.Find("Label")?.GetComponent<TMP_Text>();
            DisableLocalization(label);
            var countTransform = slot.Find("Part Count");
            GameObject countObject;
            if (countTransform != null) countObject = countTransform.gameObject;
            else
            {
                countObject = new GameObject("Part Count", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                countObject.transform.SetParent(slot, false);
            }
            var rect = (RectTransform)countObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-8f, -7f);
            rect.sizeDelta = new Vector2(54f, 28f);
            count = countObject.GetComponent<TextMeshProUGUI>();
            if (LocalizationManager.Exists && LocalizationManager.Instance.Font != null)
                count.font = LocalizationManager.Instance.Font;
            count.fontSize = 19f;
            count.alignment = TextAlignmentOptions.TopRight;
            count.color = tint;
            count.raycastTarget = false;
            return label;
        }

        private static void DisableLocalization(TMP_Text text)
        {
            if (text == null) return;
            var localized = text.GetComponent<LocalizedText>();
            if (localized != null) localized.enabled = false;
        }

        private void Craft()
        {
            if (InventoryManager.Exists) InventoryManager.Instance.TryCraftEliteEquipment();
        }

        private void CraftAdvanced()
        {
            if (InventoryManager.Exists) InventoryManager.Instance.TryCraftAdvancedEquipment();
        }

        private void OnLanguageChanged(Language _) => Refresh();

        private void Refresh()
        {
            if (!_initialized) EnsureVisuals();
            int countA = InventoryManager.Exists
                ? InventoryManager.Instance.Count(InventoryItemType.AdvancedPartA) : 0;
            int countB = InventoryManager.Exists
                ? InventoryManager.Instance.Count(InventoryItemType.AdvancedPartB) : 0;
            int equipmentCount = InventoryManager.Exists
                ? InventoryManager.Instance.Count(InventoryItemType.EliteEquipment) : 0;
            int specialCount = InventoryManager.Exists
                ? InventoryManager.Instance.Count(InventoryItemType.SpecialEnemyPart) : 0;

            if (_partACount != null) _partACount.text = "×" + countA;
            if (_partBCount != null) _partBCount.text = "×" + countB;
            if (_partAIcon != null)
                _partAIcon.color = new Color(.42f, .88f, 1f, countA > 0 ? .88f : .2f);
            if (_partBIcon != null)
                _partBIcon.color = new Color(1f, .68f, .3f, countB > 0 ? .88f : .2f);
            if (_craftButton != null) _craftButton.interactable = countA > 0 && countB > 0;
            if (_equipmentCount != null) _equipmentCount.text = "×" + equipmentCount;
            if (_specialPartCount != null) _specialPartCount.text = "×" + specialCount;
            if (_equipmentIcon != null)
                _equipmentIcon.color = new Color(1f, .86f, .42f, equipmentCount > 0 ? .88f : .2f);
            if (_specialPartIcon != null)
                _specialPartIcon.color = new Color(.78f, .48f, 1f, specialCount > 0 ? .88f : .2f);
            if (_advancedCraftButton != null)
                _advancedCraftButton.interactable = equipmentCount > 0 && specialCount > 0;

            if (!LocalizationManager.Exists) return;
            var loc = LocalizationManager.Instance;
            if (_hint != null) _hint.text = loc.Get(LocalizationTable.Keys.HudEquipmentSynthesis);
            if (_partALabel != null) _partALabel.text = loc.Get(LocalizationTable.Keys.ItemAdvancedPartA);
            if (_partBLabel != null) _partBLabel.text = loc.Get(LocalizationTable.Keys.ItemAdvancedPartB);
            if (_craftLabel != null) _craftLabel.text = loc.Get(LocalizationTable.Keys.HudCraftEquipment);
            if (_equipmentLabel != null) _equipmentLabel.text = loc.Get(LocalizationTable.Keys.ItemEliteEquipment);
            if (_specialPartLabel != null) _specialPartLabel.text = loc.Get(LocalizationTable.Keys.ItemSpecialEnemyPart);
            if (_advancedCraftLabel != null)
                _advancedCraftLabel.text = loc.Get(LocalizationTable.Keys.HudCraftAdvancedEquipment);
        }
    }
}
