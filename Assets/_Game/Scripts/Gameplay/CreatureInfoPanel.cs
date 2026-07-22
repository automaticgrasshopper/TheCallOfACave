using TMPro;
using UnityEngine;
using TCC.Managers;

namespace TCC.Gameplay
{
    /// <summary>Reusable world-space identity-card prefab shown on bug hover.</summary>
    public class CreatureInfoPanel : MonoBehaviour
    {
        private Creature _creature;
        private TextMeshPro _text;
        private SpriteRenderer _back;
        private static Sprite _pixel;

        public void Init(Creature creature)
        {
            _creature = creature;
            EnsureVisuals();
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            EnsureVisuals();
            _back.enabled = visible;
            _text.enabled = visible;
        }

        private void LateUpdate()
        {
            if (_creature == null) return;
            EnsureVisuals();
            _text.text = _creature.InfoText;
            // Creature sprites wobble and scale; keep the card stable and readable.
            transform.localRotation = Quaternion.Inverse(transform.parent.localRotation);
            Vector3 parentScale = transform.parent.localScale;
            transform.localScale = new Vector3(1f / Mathf.Max(.01f, parentScale.x),
                1f / Mathf.Max(.01f, parentScale.y), 1f);
        }

        private void EnsureVisuals()
        {
            if (_text != null) return;
            var backGo = new GameObject("Info Card Back", typeof(SpriteRenderer));
            backGo.transform.SetParent(transform, false);
            backGo.transform.localScale = new Vector3(2.1f, 1.18f, 1f);
            _back = backGo.GetComponent<SpriteRenderer>();
            _back.sprite = Pixel;
            _back.color = new Color(.015f, .025f, .028f, .88f);
            _back.sortingOrder = 46;

            var textGo = new GameObject("Info Card Text", typeof(TextMeshPro));
            textGo.transform.SetParent(transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -.01f);
            _text = textGo.GetComponent<TextMeshPro>();
            _text.alignment = TextAlignmentOptions.MidlineLeft;
            _text.fontSize = 1.45f;
            _text.enableAutoSizing = false;
            _text.lineSpacing = 4f;
            _text.rectTransform.sizeDelta = new Vector2(1.88f, 1.05f);
            _text.color = new Color(.9f, .95f, .86f, 1f);
            _text.sortingOrder = 47;
            if (LocalizationManager.Exists && LocalizationManager.Instance.Font != null)
                _text.font = LocalizationManager.Instance.Font;
        }

        private static Sprite Pixel
        {
            get
            {
                if (_pixel != null) return _pixel;
                var texture = Texture2D.whiteTexture;
                _pixel = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(.5f, .5f), texture.width);
                _pixel.name = "Runtime Info Card Pixel";
                return _pixel;
            }
        }
    }
}
