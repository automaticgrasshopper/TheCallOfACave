using UnityEngine;
using TCC.Managers;

namespace TCC.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public class EnemyRobot : MonoBehaviour
    {
        private const float AttackVisualDuration = .42f;

        private SimulationManager _sim;
        private float _health;
        private float _maxHealth;
        private float _attackTimer;
        private float _attackVisualTimer;
        private float _hitTimer;
        private float _animationPhase;
        private Creature _creatureTarget;
        private ColonyFacility _facilityTarget;
        private SpriteRenderer _renderer;
        private SpriteRenderer _healthBack;
        private SpriteRenderer _healthFill;
        private Sprite[] _walkFrames;
        private Sprite[] _attackFrames;
        [SerializeField] private bool _heavy;

        private static Sprite[] _normalWalkFrames;
        private static Sprite[] _normalAttackFrames;
        private static Sprite[] _heavyWalkFrames;
        private static Sprite[] _heavyAttackFrames;
        private static Sprite _healthPixel;

        public Vector2 Position => transform.position;
        public bool Alive => _health > 0f;
        public bool IsHeavy => _heavy;
        public bool IsTargeting(ColonyFacility facility)
            => Alive && facility != null && _facilityTarget == facility;

        public void Init(SimulationManager sim)
        {
            _sim = sim;
            _renderer = GetComponent<SpriteRenderer>();
            _maxHealth = _heavy ? sim.Config.heavyEnemyMaxHealth : sim.Config.enemyMaxHealth;
            _health = _maxHealth;
            _animationPhase = Random.value * 10f;
            LoadAnimationFrames();
            EnsureHealthBar();
            RefreshHealthBar();
        }

        private void Update()
        {
            if (_sim == null || !Alive) return;
            _attackVisualTimer = Mathf.Max(0f, _attackVisualTimer - Time.deltaTime);
            _hitTimer = Mathf.Max(0f, _hitTimer - Time.deltaTime);
            SelectNearestTarget();
            Vector2 destination = _creatureTarget != null ? _creatureTarget.Position
                : _facilityTarget != null ? _facilityTarget.Center : new Vector2(-8f, 0f);
            Vector2 delta = destination - Position;
            float reach = _facilityTarget != null ? Mathf.Max(.7f, _facilityTarget.Radius * .72f) : .7f;
            bool moving = delta.sqrMagnitude > reach * reach;
            if (moving)
            {
                float speed = _heavy ? .48f : .72f;
                transform.position = _sim.ClampWorld(Position + delta.normalized * speed * Time.deltaTime);
                _renderer.flipX = delta.x < 0f;
            }
            else if (_creatureTarget != null || _facilityTarget != null)
            {
                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0f)
                {
                    _attackTimer = _sim.Config.attackInterval;
                    _attackVisualTimer = AttackVisualDuration;
                    if (_creatureTarget != null)
                    {
                        float damage = _heavy ? _sim.Config.heavyEnemyDamage : _sim.Config.enemyDamage;
                        _creatureTarget.TakeCombatDamage(damage, delta.normalized);
                    }
                    else
                    {
                        int min = _heavy ? _sim.Config.heavyEnemyFacilityDamageMin
                            : _sim.Config.enemyFacilityDamageMin;
                        int max = _heavy ? _sim.Config.heavyEnemyFacilityDamageMax
                            : _sim.Config.enemyFacilityDamageMax;
                        _facilityTarget.TakeDamage(Random.Range(min, max + 1));
                    }
                }
            }
            UpdateVisuals(moving);
            RefreshHealthBar();
        }

        private void SelectNearestTarget()
        {
            var creature = _sim.ClosestCreature(Position);
            var facility = _sim.ClosestFacility(Position);
            float creatureDistance = creature != null ? (creature.Position - Position).sqrMagnitude : float.MaxValue;
            float facilityDistance = facility != null
                ? Mathf.Max(0f, (facility.Center - Position).magnitude - facility.Radius)
                : float.MaxValue;
            facilityDistance *= facilityDistance;
            _creatureTarget = creatureDistance <= facilityDistance ? creature : null;
            _facilityTarget = _creatureTarget == null ? facility : null;
        }

        public void TakeDamage(float amount)
        {
            if (!Alive || amount <= 0f) return;
            _health = Mathf.Max(0f, _health - amount);
            _hitTimer = .16f;
            RefreshHealthBar();
            if (_health > 0f) return;
            _sim?.SpawnEnemyPart(Position, _heavy);
            _sim?.RemoveEnemy(this);
            Destroy(gameObject);
        }

        private void LoadAnimationFrames()
        {
            if (_heavy)
            {
                if (_heavyWalkFrames == null || _heavyAttackFrames == null)
                    LoadSheet("Art/Enemies/enemy_heavy_anim", 3.45f,
                        out _heavyWalkFrames, out _heavyAttackFrames);
                _walkFrames = _heavyWalkFrames;
                _attackFrames = _heavyAttackFrames;
            }
            else
            {
                if (_normalWalkFrames == null || _normalAttackFrames == null)
                    LoadSheet("Art/Enemies/enemy_robot_anim", 1.15f,
                        out _normalWalkFrames, out _normalAttackFrames);
                _walkFrames = _normalWalkFrames;
                _attackFrames = _normalAttackFrames;
            }
        }

        private static void LoadSheet(string path, float worldWidth,
            out Sprite[] walkFrames, out Sprite[] attackFrames)
        {
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                walkFrames = new Sprite[0];
                attackFrames = new Sprite[0];
                return;
            }

            texture.filterMode = FilterMode.Point;
            int frameWidth = texture.width / 4;
            int frameHeight = texture.height / 2;
            float pixelsPerUnit = frameWidth / worldWidth;
            walkFrames = new Sprite[4];
            attackFrames = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                walkFrames[i] = Sprite.Create(texture,
                    new Rect(i * frameWidth, frameHeight, frameWidth, frameHeight),
                    new Vector2(.5f, .5f), pixelsPerUnit);
                attackFrames[i] = Sprite.Create(texture,
                    new Rect(i * frameWidth, 0, frameWidth, frameHeight),
                    new Vector2(.5f, .5f), pixelsPerUnit);
            }
        }

        private void UpdateVisuals(bool moving)
        {
            if (_renderer == null) return;
            if (_attackVisualTimer > 0f && _attackFrames != null && _attackFrames.Length > 0)
            {
                float progress = 1f - _attackVisualTimer / AttackVisualDuration;
                int index = Mathf.Clamp(Mathf.FloorToInt(progress * _attackFrames.Length),
                    0, _attackFrames.Length - 1);
                _renderer.sprite = _attackFrames[index];
            }
            else if (_walkFrames != null && _walkFrames.Length > 0)
            {
                int index = moving
                    ? Mathf.FloorToInt((Time.time + _animationPhase) * (_heavy ? 6f : 9f))
                        % _walkFrames.Length
                    : 0;
                _renderer.sprite = _walkFrames[index];
            }
            _renderer.color = _hitTimer > 0f
                ? new Color(1f, .28f, .2f, 1f)
                : Color.white;
        }

        private void EnsureHealthBar()
        {
            if (_healthFill != null) return;
            _healthBack = MakeHealthBarPart("Enemy Health Back",
                new Color(.015f, .025f, .025f, .96f), 27);
            _healthFill = MakeHealthBarPart("Enemy Health Fill",
                new Color(.27f, .8f, .38f, 1f), 28);
        }

        private SpriteRenderer MakeHealthBarPart(string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = HealthPixel;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void RefreshHealthBar()
        {
            EnsureHealthBar();
            float value = _maxHealth > 0f ? Mathf.Clamp01(_health / _maxHealth) : 0f;
            float width = _heavy ? 2.35f : .92f;
            float height = _heavy ? .12f : .085f;
            float y = _heavy ? 1.36f : .78f;
            _healthBack.enabled = Alive;
            _healthFill.enabled = Alive;
            _healthBack.transform.localPosition = new Vector3(0f, y, 0f);
            _healthBack.transform.localScale = new Vector3(width, height, 1f);
            _healthFill.transform.localPosition =
                new Vector3(-width * .5f + width * value * .5f, y, -.01f);
            _healthFill.transform.localScale =
                new Vector3(Mathf.Max(.001f, width * value), height * .62f, 1f);
            _healthFill.color = value < .25f ? new Color(.92f, .2f, .14f, 1f)
                : value < .55f ? new Color(.95f, .68f, .16f, 1f)
                : new Color(.27f, .8f, .38f, 1f);
        }

        private static Sprite HealthPixel
        {
            get
            {
                if (_healthPixel != null) return _healthPixel;
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.name = "Enemy Health Pixel";
                texture.filterMode = FilterMode.Point;
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _healthPixel = Sprite.Create(texture, new Rect(0, 0, 1, 1),
                    new Vector2(.5f, .5f), 1f);
                return _healthPixel;
            }
        }
    }
}
