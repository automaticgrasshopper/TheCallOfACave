using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Managers;

namespace TCC.Gameplay
{
    /// <summary>
    /// The core economic fork made physical: leave it alone and it hatches into a
    /// new juvenile, or click it to cash out for coins. It raises
    /// <see cref="GameEvents.EggCollected"/> on click and lets the economy decide
    /// the coin value.
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

        public void Init(SimulationManager sim, SimulationConfig cfg)
        {
            _sim = sim;
            _cfg = cfg;
            _hatch = cfg.eggHatchSeconds;
            _phase = Random.value * Mathf.PI * 2f;
            _baseScale = transform.localScale;
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
            if (_hatch <= 0f) Hatch();
        }

        private void OnMouseDown()
        {
            if (_consumed) return;
            if (GameManager.Exists && GameManager.Instance.State != GameState.Playing) return;
            _consumed = true;
            int value = EconomyManager.Exists ? EconomyManager.Instance.EggSellValue : 0;
            GameEvents.RaiseEggCollected(value, transform.position);
            if (_sim != null) _sim.RemoveEgg(this);
            Destroy(gameObject);
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
