using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Managers;

namespace TCC.Gameplay
{
    /// <summary>
    /// A cave bug. It is an autonomous agent driven purely by age: it wanders the
    /// activity area, ages juvenile -> prime -> elder on a lifespan timer, lays eggs
    /// while prime, and dies of old age. The player can pick it up and drop it into
    /// the labor circle, where a prime bug "works" (the <see cref="LaborZone"/> pays
    /// out); dragging it back out sets it wandering again. Visuals are procedural
    /// (squash bob) per the project's "juice over keyframes" taste.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class Creature : MonoBehaviour
    {
        [Header("Stage sprites (set on prefab)")]
        [SerializeField] private Sprite _infantSprite;
        [SerializeField] private Sprite _adultSprite;
        [SerializeField] private Sprite _elderSprite;

        private SpriteRenderer _sr;
        private SimulationManager _sim;
        private SimulationConfig _cfg;

        private CreatureStage _stage;
        private float _age;
        private float _lifespan;
        private float _juvenileEnd;   // age at which prime begins
        private float _primeEnd;      // age at which elder begins
        private float _layTimer;
        private float _wobblePhase;

        private Vector2 _wanderDir;
        private float _wanderTimer;

        private bool _dragging;
        private bool _working;        // parked inside the labor circle

        public CreatureStage Stage => _stage;
        public bool IsWorking => _working && _stage == CreatureStage.Adult;

        /// <param name="ageFraction">Starting age as a fraction of the rolled lifespan
        /// (0 = newborn juvenile; ~0.3 = an established adult for the opening pop).</param>
        public void Init(SimulationManager sim, SimulationConfig cfg, float ageFraction)
        {
            _sim = sim;
            _cfg = cfg;
            _sr = GetComponent<SpriteRenderer>();
            _lifespan = Random.Range(cfg.lifespanMin, cfg.lifespanMax);
            _juvenileEnd = _lifespan * cfg.juvenileFraction;
            _primeEnd = _lifespan * Mathf.Clamp01(cfg.juvenileFraction + cfg.primeFraction);
            _age = _lifespan * Mathf.Clamp01(ageFraction);
            _wobblePhase = Random.value * Mathf.PI * 2f;
            PickWander();
            EnterStage(StageForAge(_age), initial: true);
        }

        private void Update()
        {
            if (_cfg == null || _dragging) return;
            float dt = Time.deltaTime;

            _age += dt;
            if (_age >= _lifespan) { Die(); return; }

            var target = StageForAge(_age);
            if (target != _stage) EnterStage(target);

            if (!_working) UpdateMovement(dt);
            if (_stage == CreatureStage.Adult) UpdateBreeding(dt);
            UpdateVisuals();
        }

        private CreatureStage StageForAge(float age)
            => age < _juvenileEnd ? CreatureStage.Infant
             : age < _primeEnd ? CreatureStage.Adult
             : CreatureStage.Elder;

        // ---- movement ----------------------------------------------------
        private void UpdateMovement(float dt)
        {
            _wanderTimer -= dt;
            if (_wanderTimer <= 0f) PickWander();

            // Everything crawls; infants are the slowest, elders a touch slower than prime.
            float stageMul = _stage == CreatureStage.Infant ? 0.5f
                           : _stage == CreatureStage.Adult ? 0.8f
                           : 0.6f;
            float speed = _cfg.moveSpeed * stageMul;
            Vector2 next = (Vector2)transform.position + _wanderDir * speed * dt;
            next = _sim.ClampToActivity(next);
            transform.position = next;

            if (Mathf.Abs(_wanderDir.x) > 0.01f)
                _sr.flipX = _wanderDir.x < 0f;
        }

        private void PickWander()
        {
            _wanderDir = Random.insideUnitCircle.normalized;
            _wanderTimer = _cfg.wanderChangeInterval * Random.Range(0.6f, 1.4f);
        }

        // ---- breeding ----------------------------------------------------
        private void UpdateBreeding(float dt)
        {
            _layTimer += dt;
            if (_layTimer >= _cfg.eggLayIntervalSeconds)
            {
                _layTimer = 0f;
                _sim.LayEgg(transform.position);
            }
        }

        // ---- drag & labor ------------------------------------------------
        private bool CanInteract => !(GameManager.Exists && GameManager.Instance.State != GameState.Playing);

        private void OnMouseDown()
        {
            if (!CanInteract) return;
            _dragging = true;
            if (_working) SetWorking(false); // lift it out of the labor pool while held
        }

        private void OnMouseDrag()
        {
            if (!_dragging) return;
            transform.position = MouseWorld();
        }

        private void OnMouseUp()
        {
            if (!_dragging) return;
            _dragging = false;

            var zone = _sim != null ? _sim.Labor : null;
            Vector2 pos = transform.position;
            if (zone != null && zone.Contains(pos) && zone.TryPark(this))
            {
                SetWorking(true); // dropped into the circle — start working
            }
            else
            {
                SetWorking(false);
                transform.position = _sim.ClampToActivity(pos); // snap back into bounds
                PickWander();
            }
        }

        private void SetWorking(bool on)
        {
            if (_working == on) return;
            _working = on;
            if (!on && _sim != null && _sim.Labor != null) _sim.Labor.Unpark(this);
        }

        private Vector2 MouseWorld()
        {
            var cam = Camera.main;
            if (cam == null) return transform.position;
            Vector3 w = cam.ScreenToWorldPoint(Input.mousePosition);
            return new Vector2(w.x, w.y);
        }

        // ---- stage / visuals ---------------------------------------------
        private void EnterStage(CreatureStage stage, bool initial = false)
        {
            _stage = stage;
            _layTimer = 0f;
            _sr.sprite = stage == CreatureStage.Infant ? _infantSprite
                       : stage == CreatureStage.Adult ? _adultSprite
                       : _elderSprite;
            if (!initial && _sim != null) _sim.NotifyStageChanged();
        }

        private void UpdateVisuals()
        {
            float target = _stage == CreatureStage.Infant ? _cfg.infantScale
                          : _stage == CreatureStage.Adult ? _cfg.primeScale
                          : _cfg.elderScale;

            // A busy worker bounces a little quicker to read as "doing something".
            float rate = IsWorking ? 9f : 6f;
            float bob = Mathf.Sin(Time.time * rate + _wobblePhase) * 0.05f;
            transform.localScale = new Vector3(target * (1f - bob), target * (1f + bob), 1f);
        }

        private void Die()
        {
            if (_working && _sim != null && _sim.Labor != null) _sim.Labor.Unpark(this);
            GameEvents.RaiseCreatureDied(transform.position);
            if (_sim != null) _sim.RemoveCreature(this);
            Destroy(gameObject);
        }
    }
}
