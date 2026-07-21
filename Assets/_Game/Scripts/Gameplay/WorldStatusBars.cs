using UnityEngine;

namespace TCC.Gameplay
{
    /// <summary>Small sprite-based status bars that live with an entity prefab.</summary>
    public class WorldStatusBars : MonoBehaviour
    {
        private SpriteRenderer _healthBack, _healthFill, _ageBack, _ageFill, _combatBack, _combatFill;
        private static Sprite _pixel;

        public void Set(float health, float age, float combat = -1f)
        {
            Ensure();
            SetBar(_healthFill, Mathf.Clamp01(health), HealthColor(health));
            SetBar(_ageFill, Mathf.Clamp01(age), new Color(.35f, .72f, .82f, 1f));
            bool showCombat = combat >= 0f;
            _combatBack.enabled = showCombat;
            _combatFill.enabled = showCombat;
            if (showCombat) SetBar(_combatFill, Mathf.Clamp01(combat), new Color(.85f, .28f, .24f, 1f));
        }

        private static Color HealthColor(float value)
            => value < .2f ? new Color(.9f, .18f, .14f, 1f)
             : value < .5f ? new Color(.92f, .68f, .16f, 1f)
             : new Color(.25f, .76f, .36f, 1f);

        private void Ensure()
        {
            if (_healthFill != null) return;
            _healthBack = Part("Health Back", new Vector3(0f, .59f, 0f), new Color(.02f, .025f, .025f, .92f), 40);
            _healthFill = Part("Health Fill", new Vector3(0f, .59f, -.01f), Color.green, 41);
            _ageBack = Part("Age Back", new Vector3(0f, .49f, 0f), new Color(.02f, .025f, .025f, .92f), 40);
            _ageFill = Part("Age Fill", new Vector3(0f, .49f, -.01f), Color.cyan, 41);
            _combatBack = Part("Combat Back", new Vector3(0f, .69f, 0f), new Color(.02f, .025f, .025f, .92f), 40);
            _combatFill = Part("Combat Fill", new Vector3(0f, .69f, -.01f), Color.red, 41);
        }

        private SpriteRenderer Part(string name, Vector3 pos, Color color, int order)
        {
            var child = transform.Find(name);
            var go = child != null ? child.gameObject : new GameObject(name);
            if (child == null) go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(.62f, .055f, 1f);
            var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
            sr.sprite = Pixel;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private static void SetBar(SpriteRenderer fill, float value, Color color)
        {
            fill.color = color;
            fill.transform.localScale = new Vector3(Mathf.Max(.001f, value) * .58f, .032f, 1f);
            fill.transform.localPosition = new Vector3(-.29f + value * .29f, fill.transform.localPosition.y, -.01f);
        }

        private static Sprite Pixel
        {
            get
            {
                if (_pixel != null) return _pixel;
                var texture = Texture2D.whiteTexture;
                // Normalize any Unity built-in white texture to exactly one world unit.
                _pixel = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(.5f, .5f), texture.width);
                _pixel.name = "Runtime Status Pixel";
                return _pixel;
            }
        }
    }
}
