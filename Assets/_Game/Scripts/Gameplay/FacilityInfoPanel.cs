using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TCC.Managers;

namespace TCC.Gameplay
{
    /// <summary>Readable screen-space facility card shown while the pointer is over a facility.</summary>
    public class FacilityInfoPanel : MonoBehaviour
    {
        private ColonyFacility _facility;
        private GameObject _overlayRoot;
        private Canvas _canvas;
        private RectTransform _panelRect;
        private TextMeshProUGUI _text;
        private RectTransform _healthFill;
        private Image _healthFillImage;

        public void Init(ColonyFacility facility)
        {
            _facility = facility;
            EnsureVisuals();
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            EnsureVisuals();
            if (_canvas != null) _canvas.enabled = visible;
        }

        private void LateUpdate()
        {
            if (_facility == null || _canvas == null || !_canvas.enabled) return;
            _text.text = _facility.InfoText;
            float health = _facility.StructureHealthNormalized;
            _healthFill.anchorMax = new Vector2(Mathf.Max(.001f, health), 1f);
            _healthFillImage.color = health < .25f ? new Color(.92f, .2f, .14f, 1f)
                : health < .55f ? new Color(.95f, .68f, .16f, 1f)
                : new Color(.27f, .8f, .38f, 1f);
            PositionNearPointer();
        }

        private void PositionNearPointer()
        {
            var canvasRect = _overlayRoot.transform as RectTransform;
            if (canvasRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, Input.mousePosition, null, out Vector2 local)) return;

            Vector2 size = canvasRect.rect.size;
            Vector2 desired = local + new Vector2(248f, 106f);
            desired.x = Mathf.Clamp(desired.x, -size.x * .5f + 240f, size.x * .5f - 240f);
            desired.y = Mathf.Clamp(desired.y, -size.y * .5f + 90f, size.y * .5f - 90f);
            _panelRect.anchoredPosition = desired;
        }

        private void EnsureVisuals()
        {
            if (_overlayRoot != null) return;
            _overlayRoot = new GameObject("Facility Info Overlay", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            _canvas = _overlayRoot.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 675;
            var scaler = _overlayRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;

            var panel = new GameObject("Facility Info Card", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            panel.transform.SetParent(_overlayRoot.transform, false);
            _panelRect = (RectTransform)panel.transform;
            _panelRect.anchorMin = _panelRect.anchorMax = new Vector2(.5f, .5f);
            _panelRect.pivot = new Vector2(.5f, .5f);
            _panelRect.sizeDelta = new Vector2(520f, 218f);
            panel.GetComponent<Image>().color = new Color(.012f, .027f, .03f, .97f);
            var outline = panel.GetComponent<Outline>();
            outline.effectColor = new Color(.84f, .59f, .22f, .96f);
            outline.effectDistance = new Vector2(3f, -3f);

            var textObject = new GameObject("Facility Info Text", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panel.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(32f, 52f);
            textRect.offsetMax = new Vector2(-28f, -18f);
            _text = textObject.GetComponent<TextMeshProUGUI>();
            if (LocalizationManager.Exists && LocalizationManager.Instance.Font != null)
                _text.font = LocalizationManager.Instance.Font;
            _text.fontSize = 28f;
            _text.enableAutoSizing = true;
            _text.fontSizeMin = 22f;
            _text.fontSizeMax = 28f;
            _text.lineSpacing = 4f;
            _text.alignment = TextAlignmentOptions.MidlineLeft;
            _text.color = new Color(.92f, .97f, .91f, 1f);
            _text.enableWordWrapping = true;
            _text.overflowMode = TextOverflowModes.Ellipsis;
            _text.raycastTarget = false;

            var healthBack = new GameObject("Durability Back", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            healthBack.transform.SetParent(panel.transform, false);
            var backRect = (RectTransform)healthBack.transform;
            backRect.anchorMin = new Vector2(0f, 0f);
            backRect.anchorMax = new Vector2(1f, 0f);
            backRect.pivot = new Vector2(.5f, 0f);
            backRect.offsetMin = new Vector2(32f, 22f);
            backRect.offsetMax = new Vector2(-28f, 42f);
            healthBack.GetComponent<Image>().color = new Color(.08f, .1f, .1f, 1f);
            healthBack.GetComponent<Image>().raycastTarget = false;

            var healthFill = new GameObject("Durability Fill", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            healthFill.transform.SetParent(healthBack.transform, false);
            _healthFill = (RectTransform)healthFill.transform;
            _healthFill.anchorMin = Vector2.zero;
            _healthFill.anchorMax = Vector2.one;
            _healthFill.offsetMin = new Vector2(3f, 3f);
            _healthFill.offsetMax = new Vector2(-3f, -3f);
            _healthFillImage = healthFill.GetComponent<Image>();
            _healthFillImage.raycastTarget = false;
        }

        private void OnDestroy()
        {
            if (_overlayRoot != null) Destroy(_overlayRoot);
        }
    }
}
