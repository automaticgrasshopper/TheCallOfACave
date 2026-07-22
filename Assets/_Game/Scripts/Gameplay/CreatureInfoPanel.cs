using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TCC.Managers;

namespace TCC.Gameplay
{
    /// <summary>Reusable 1920x1080-safe screen-space identity card shown on bug hover.</summary>
    public class CreatureInfoPanel : MonoBehaviour
    {
        private Creature _creature;
        private GameObject _overlayRoot;
        private Canvas _canvas;
        private RectTransform _panelRect;
        private TextMeshProUGUI _text;

        public void Init(Creature creature)
        {
            _creature = creature;
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
            if (_creature == null || _canvas == null || !_canvas.enabled) return;
            _text.text = _creature.InfoText;
            PositionNearPointer();
        }

        private void PositionNearPointer()
        {
            var canvasRect = _overlayRoot.transform as RectTransform;
            if (canvasRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, Input.mousePosition, null, out Vector2 local)) return;

            Vector2 size = canvasRect.rect.size;
            Vector2 desired = local + new Vector2(238f, 128f);
            desired.x = Mathf.Clamp(desired.x, -size.x * .5f + 230f, size.x * .5f - 230f);
            desired.y = Mathf.Clamp(desired.y, -size.y * .5f + 116f, size.y * .5f - 116f);
            _panelRect.anchoredPosition = desired;
        }

        private void EnsureVisuals()
        {
            if (_overlayRoot != null) return;
            _overlayRoot = new GameObject("Creature Info Overlay", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            _canvas = _overlayRoot.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 680;
            var scaler = _overlayRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;

            var panel = new GameObject("Info Card", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            panel.transform.SetParent(_overlayRoot.transform, false);
            _panelRect = (RectTransform)panel.transform;
            _panelRect.anchorMin = _panelRect.anchorMax = new Vector2(.5f, .5f);
            _panelRect.pivot = new Vector2(.5f, .5f);
            _panelRect.sizeDelta = new Vector2(460f, 224f);
            panel.GetComponent<Image>().color = new Color(.012f, .027f, .03f, .97f);
            var outline = panel.GetComponent<Outline>();
            outline.effectColor = new Color(.24f, .78f, .72f, .95f);
            outline.effectDistance = new Vector2(3f, -3f);

            var textObject = new GameObject("Info Text", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panel.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(34f, 24f);
            textRect.offsetMax = new Vector2(-30f, -22f);
            _text = textObject.GetComponent<TextMeshProUGUI>();
            if (LocalizationManager.Exists && LocalizationManager.Instance.Font != null)
                _text.font = LocalizationManager.Instance.Font;
            _text.fontSize = 30f;
            _text.enableAutoSizing = true;
            _text.fontSizeMin = 24f;
            _text.fontSizeMax = 30f;
            _text.lineSpacing = 5f;
            _text.alignment = TextAlignmentOptions.MidlineLeft;
            _text.color = new Color(.9f, .97f, .91f, 1f);
            _text.enableWordWrapping = true;
            _text.overflowMode = TextOverflowModes.Ellipsis;
            _text.raycastTarget = false;
        }

        private void OnDestroy()
        {
            if (_overlayRoot != null) Destroy(_overlayRoot);
        }
    }
}
