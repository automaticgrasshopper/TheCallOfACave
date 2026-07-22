using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Managers;
using TCC.UI;

namespace TCC.Gameplay
{
    /// <summary>
    /// A colony egg. Drag it anywhere to relocate its hatch point, or release it
    /// over the coin display to sell it before incubation completes.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Egg : MonoBehaviour
    {
        private SimulationManager _sim;
        private SimulationConfig _cfg;
        private float _hatch;
        private float _phase;
        private Vector3 _baseScale;
        private bool _consumed;
        private bool _dragging;
        private static Sprite _pixelEggSprite;
        private static bool _pixelEggLoaded;

        public void Init(SimulationManager sim, SimulationConfig cfg)
        {
            _sim = sim;
            _cfg = cfg;
            if (PixelEggSprite != null)
            {
                var renderer = GetComponent<SpriteRenderer>();
                renderer.sprite = PixelEggSprite;
            }
            _hatch = cfg.eggHatchSeconds;
            _phase = Random.value * Mathf.PI * 2f;
            _baseScale = transform.localScale;
        }

        private static Sprite PixelEggSprite
        {
            get
            {
                if (_pixelEggLoaded) return _pixelEggSprite;
                _pixelEggLoaded = true;
                var texture = Resources.Load<Texture2D>("Art/egg_pixel");
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Point;
                    _pixelEggSprite = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), texture.width / .65f);
                }
                return _pixelEggSprite;
            }
        }

        private void Update()
        {
            // juice: wobble that speeds up as hatching nears.
            float t = 1f - Mathf.Clamp01(_hatch / Mathf.Max(0.01f, _cfg.eggHatchSeconds));
            float freq = Mathf.Lerp(3f, 12f, t);
            float amp = Mathf.Lerp(0.03f, 0.12f, t);
            float wob = Mathf.Sin(Time.time * freq + _phase) * amp;
            transform.localScale = _baseScale + new Vector3(wob, -wob, 0f) * _baseScale.x;

            _hatch -= Time.deltaTime;
            if (_hatch <= 0f && !_dragging) Hatch();
        }

        private void OnMouseDown()
        {
            if (_consumed) return;
            if (GameManager.Exists && GameManager.Instance.State != GameState.Playing) return;
            _dragging = true;
        }

        private void OnMouseDrag()
        {
            if (!_dragging || Camera.main == null) return;
            Vector3 screen = new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                -Camera.main.transform.position.z);
            Vector2 world = Camera.main.ScreenToWorldPoint(screen);
            if (_sim != null) world = _sim.ClampWorld(world);
            transform.position = new Vector3(world.x, world.y, 0f);
        }

        private void OnMouseUp()
        {
            if (!_dragging || _consumed) return;
            _dragging = false;
            if (MoneyDropTarget.ContainsScreenPoint(Input.mousePosition))
            {
                _consumed = true;
                int value = EconomyManager.Exists ? EconomyManager.Instance.EggSellValue : 0;
                GameEvents.RaiseEggCollected(value, transform.position);
                if (_sim != null) _sim.RemoveEgg(this);
                Destroy(gameObject);
                return;
            }
            if (_hatch <= 0f) Hatch();
        }

        private void Hatch()
        {
            if (_consumed) return;
            _consumed = true;
            if (_sim != null)
            {
                _sim.SpawnJuvenile(transform.position);
                _sim.RemoveEgg(this);
            }
            Destroy(gameObject);
        }
    }
}
