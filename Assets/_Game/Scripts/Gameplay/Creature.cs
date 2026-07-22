using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Managers;
using TCC.UI;

namespace TCC.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public class Creature : MonoBehaviour
    {
        [Header("Prefab fallback sprites")]
        [SerializeField] private Sprite _infantSprite;
        [SerializeField] private Sprite _adultSprite;
        [SerializeField] private Sprite _elderSprite;

        private SpriteRenderer _sr;
        private SimulationManager _sim;
        private SimulationConfig _cfg;
        private WorldStatusBars _bars;
        private CreatureStage _stage;
        private CreatureRole _role = CreatureRole.Free;
        private CreatureRole _roleBeforeHospital = CreatureRole.Free;
        private ColonyFacility _facility;
        private float _age;
        private float _lifespan;
        private float _health;
        private float _hunger;
        private float _combatHealth;
        private float _layTimer;
        private float _wanderTimer;
        private Vector2 _wanderDir;
        private bool _infected;
        private bool _dragging;
        private bool _dragMoved;
        private Vector2 _dragStart;
        private float _attackTimer;
        private float _hitTimer;
        private Vector2 _knockbackDir;
        private float _wobblePhase;
        private bool _eliteSoldier;
        private string _code;
        private DroppedFood _foodTarget;
        private Vector2 _facilityWanderTarget;
        private CreatureInfoPanel _infoPanel;

        private static Sprite _infant, _adult, _worker, _elder, _soldier;
        private static Sprite[] _adultWalk, _soldierAttack;

        public CreatureStage Stage => _stage;
        public CreatureRole Role => _role;
        public bool IsSoldier => _role == CreatureRole.Soldier;
        public bool IsEliteSoldier => IsSoldier && _eliteSoldier;
        public bool IsFreeAdult => _stage == CreatureStage.Adult && _role == CreatureRole.Free;
        public bool IsWorking => _role == CreatureRole.FactoryWorker;
        public bool IsTraining => _role == CreatureRole.BarracksTrainee;
        public bool IsInfected => _infected;
        public bool IsCritical => _health < _cfg.criticalHealth;
        public float Health01 => _health / _cfg.healthMax;
        public float Hunger01 => _hunger / 100f;
        public Vector2 Position => transform.position;
        public ColonyFacility Facility => _facility;
        public bool Alive => _health > 0f;
        public bool CanEat => Alive && _hunger < 99.5f;

        public string InfoText
        {
            get
            {
                var loc = LocalizationManager.Exists ? LocalizationManager.Instance : null;
                string roleKey = _role == CreatureRole.FactoryWorker ? LocalizationTable.Keys.RoleWorker
                    : _role == CreatureRole.Soldier ? LocalizationTable.Keys.RoleSoldier
                    : _role == CreatureRole.AcademyWorker ? LocalizationTable.Keys.RoleDoctor
                    : _role == CreatureRole.BarracksTrainee ? LocalizationTable.Keys.RoleTrainee
                    : _role == CreatureRole.HospitalPatient ? LocalizationTable.Keys.RolePatient
                    : LocalizationTable.Keys.RoleFree;
                string role = loc != null ? loc.Get(roleKey) : _role.ToString();
                string format = loc != null ? loc.Get(LocalizationTable.Keys.CreatureInfo)
                    : "Bug {0}\nAge {1}%  Hunger {2}%\n{3}";
                return string.Format(format, _code,
                    Mathf.RoundToInt(_age / Mathf.Max(1f, _cfg.totalLifespanSeconds) * 100f),
                    Mathf.RoundToInt((1f - Hunger01) * 100f), role);
            }
        }

        public void Init(SimulationManager sim, SimulationConfig cfg, float ageFraction)
        {
            _sim = sim;
            _cfg = cfg;
            _sr = GetComponent<SpriteRenderer>();
            _bars = GetComponent<WorldStatusBars>() ?? gameObject.AddComponent<WorldStatusBars>();
            _lifespan = cfg.totalLifespanSeconds;
            _age = _lifespan * Mathf.Clamp01(ageFraction);
            _health = cfg.healthMax;
            _hunger = 100f;
            _code = GenerateCode();
            _combatHealth = cfg.soldierMaxHealth;
            _layTimer = Random.Range(cfg.firstEggMinSeconds, cfg.eggLayIntervalSeconds);
            _wobblePhase = Random.value * Mathf.PI * 2f;
            PickWander();
            EnterStage(StageForAge(_age), true);
            var cardPrefab = Resources.Load<CreatureInfoPanel>("UI/CreatureInfoPanel");
            if (cardPrefab != null)
                _infoPanel = Instantiate(cardPrefab, transform);
            else
            {
                var cardObject = new GameObject("Creature Info Panel", typeof(CreatureInfoPanel));
                cardObject.transform.SetParent(transform, false);
                _infoPanel = cardObject.GetComponent<CreatureInfoPanel>();
            }
            _infoPanel.transform.localPosition = new Vector3(0f, 1.28f, 0f);
            _infoPanel.Init(this);
        }

        private static string GenerateCode()
        {
            char first = (char)('A' + Random.Range(0, 26));
            char second = (char)('A' + Random.Range(0, 26));
            return string.Format("{0}{1}-{2:000}", first, second, Random.Range(0, 1000));
        }

        private void Update()
        {
            if (_cfg == null || _dragging) return;
            float dt = Time.deltaTime;
            float ageMultiplier = (_infected ? _cfg.infectedAgeMultiplier : 1f)
                * (IsCritical ? _cfg.criticalAgeMultiplier : 1f)
                * (IsSoldier && IsInCombat() ? _cfg.combatAgeMultiplier : 1f);
            _age += dt * ageMultiplier;
            _hunger = Mathf.Max(0f, _hunger - _cfg.hungerLossPerSecond * dt);
            _health = Mathf.Max(0f, _health - dt * (_cfg.healthLossPerTick / Mathf.Max(.1f, _cfg.healthLossInterval))
                * (_infected ? _cfg.infectedHealthMultiplier : 1f)
                * Mathf.Lerp(.35f, 2.2f, 1f - Hunger01));
            if (_age >= _lifespan || _health <= 0f) { Die(); return; }

            CreatureStage next = StageForAge(_age);
            if (next != _stage) EnterStage(next);

            if (IsFreeAdult)
            {
                _layTimer -= dt;
                if (_layTimer <= 0f)
                {
                    _layTimer += _cfg.eggLayIntervalSeconds;
                    _sim.LayEgg(transform.position);
                }
            }

            bool parked = _role == CreatureRole.FactoryWorker || _role == CreatureRole.BarracksTrainee
                || _role == CreatureRole.HospitalPatient || _role == CreatureRole.AcademyWorker;
            bool acted = UpdateCombat(dt);
            if (!acted) acted = UpdateFoodSeeking(dt);
            if (!acted)
            {
                if (parked && _facility != null) UpdateFacilityMovement(dt);
                else UpdateMovement(dt);
            }
            if (_hitTimer > 0f)
            {
                transform.position = _sim.ClampWorld((Vector2)transform.position + _knockbackDir * 1.4f * dt);
                _hitTimer -= dt;
            }
            _attackTimer = Mathf.Max(0f, _attackTimer - dt);
            UpdateVisuals();
            _bars.Set(Health01, _age / _lifespan, IsSoldier ? _combatHealth / SoldierMaxHealth : -1f);
        }

        private CreatureStage StageForAge(float value)
            => value < _cfg.juvenileSeconds ? CreatureStage.Infant
             : value < _cfg.elderStartSeconds ? CreatureStage.Adult
             : CreatureStage.Elder;

        private void UpdateMovement(float dt)
        {
            _wanderTimer -= dt;
            if (_wanderTimer <= 0f) PickWander();
            float multiplier = _stage == CreatureStage.Infant ? .52f : _stage == CreatureStage.Elder ? .58f : .82f;
            Vector2 next = (Vector2)transform.position + _wanderDir * _cfg.moveSpeed * multiplier * dt;
            // The nursery is only a spawn point. Every unassigned bug can drift
            // across the whole cave, including the future invasion side.
            next = _sim.ClampWorld(next);
            next = _sim.PushOutsideFacilities(next, _facility);
            transform.position = next;
            if (Mathf.Abs(_wanderDir.x) > .01f) _sr.flipX = _wanderDir.x < 0f;
        }

        private bool UpdateCombat(float dt)
        {
            bool canFight = IsSoldier || _stage == CreatureStage.Adult;
            if (!canFight) return false;
            var enemy = _sim.ClosestEnemy(transform.position);
            if (enemy == null) return false;
            Vector2 delta = enemy.Position - (Vector2)transform.position;
            if (_facility != null && delta.sqrMagnitude > (_facility.Radius + .8f) * (_facility.Radius + .8f))
                return false;
            if (delta.sqrMagnitude > .7f * .7f)
            {
                Vector2 next = (Vector2)transform.position + delta.normalized * _cfg.moveSpeed * .95f * dt;
                transform.position = _facility != null ? _facility.ClampToInterior(next) : _sim.ClampWorld(next);
                _sr.flipX = delta.x < 0f;
            }
            else if (_attackTimer <= 0f)
            {
                _attackTimer = _cfg.attackInterval;
                float damage = IsSoldier
                    ? _cfg.soldierDamage * (IsEliteSoldier ? _cfg.eliteSoldierMultiplier : 1f)
                    : _cfg.soldierDamage / Mathf.Max(1f, _cfg.civilianDamageDivisor);
                enemy.TakeDamage(damage);
            }
            return true;
        }

        private bool UpdateFoodSeeking(float dt)
        {
            if (_foodTarget == null || !_foodTarget.CanBeClaimedBy(this))
                _foodTarget = _sim.ClosestFood(this, _cfg.foodSenseRadius);
            if (_foodTarget == null) return false;
            Vector2 delta = _foodTarget.Position - (Vector2)transform.position;
            if (delta.sqrMagnitude <= .3f * .3f)
            {
                _foodTarget.TryConsume(this);
                _foodTarget = null;
                return true;
            }
            Vector2 next = (Vector2)transform.position + delta.normalized * _cfg.moveSpeed * 1.12f * dt;
            transform.position = _facility != null ? _facility.ClampToInterior(next) : _sim.ClampWorld(next);
            _sr.flipX = delta.x < 0f;
            return true;
        }

        private void UpdateFacilityMovement(float dt)
        {
            if ((_facilityWanderTarget - (Vector2)transform.position).sqrMagnitude < .08f * .08f || _wanderTimer <= 0f)
            {
                _facilityWanderTarget = _facility.Center + Random.insideUnitCircle * (_facility.Radius * .38f);
                _wanderTimer = _cfg.wanderChangeInterval * Random.Range(.7f, 1.4f);
            }
            _wanderTimer -= dt;
            Vector2 delta = _facilityWanderTarget - (Vector2)transform.position;
            transform.position = _facility.ClampToInterior((Vector2)transform.position
                + delta.normalized * _cfg.moveSpeed * .38f * dt);
            if (Mathf.Abs(delta.x) > .01f) _sr.flipX = delta.x < 0f;
        }

        private bool IsInCombat()
        {
            var enemy = _sim != null ? _sim.ClosestEnemy(transform.position) : null;
            return enemy != null && (enemy.Position - (Vector2)transform.position).sqrMagnitude < 1.2f * 1.2f;
        }

        private void PickWander()
        {
            _wanderDir = Random.insideUnitCircle.normalized;
            _wanderTimer = _cfg.wanderChangeInterval * Random.Range(.6f, 1.4f);
        }

        private bool CanInteract => !GameManager.Exists || GameManager.Instance.State == GameState.Playing;

        private void OnMouseEnter() => _infoPanel?.SetVisible(true);
        private void OnMouseExit() => _infoPanel?.SetVisible(false);

        private void OnMouseDown()
        {
            if (!CanInteract) return;
            _dragStart = transform.position;
            _dragMoved = false;
            _dragging = _role == CreatureRole.Free || _role == CreatureRole.Soldier || _role == CreatureRole.HospitalPatient;
        }

        private void OnMouseDrag()
        {
            if (!_dragging || Camera.main == null) return;
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(world.x, world.y, 0f);
            _dragMoved |= ((Vector2)transform.position - _dragStart).sqrMagnitude > .08f * .08f;
        }

        private void OnMouseUp()
        {
            if (!CanInteract) return;
            if (!_dragMoved)
            {
                _dragging = false;
                return;
            }
            if (!_dragging) return;
            _dragging = false;
            Vector2 pos = transform.position;

            if (_role == CreatureRole.HospitalPatient && _facility != null && !_facility.Contains(pos))
                _facility.TryRelease(this);

            var destination = _sim.FindFacilityAt(pos);
            if (_role == CreatureRole.Free && _stage == CreatureStage.Adult && destination != null)
            {
                if (destination.TryAssign(this)) return;
            }
            transform.position = _sim.PushOutsideFacilities(_sim.ClampWorld(pos), _facility);
            PickWander();
        }

        public bool EatDroppedFood()
        {
            if (!CanEat) return false;
            _hunger = Mathf.Min(100f, _hunger + _cfg.foodHungerRestore);
            _health = Mathf.Min(_cfg.healthMax, _health + _cfg.foodHealing);
            ToastView.Instance?.Key(LocalizationTable.Keys.ToastFoodUsed);
            return true;
        }

        public bool TryEquipElite()
        {
            if (!IsSoldier || _eliteSoldier)
            {
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastEliteRequired);
                return false;
            }
            float oldMax = SoldierMaxHealth;
            _eliteSoldier = true;
            _combatHealth = Mathf.Min(SoldierMaxHealth,
                _combatHealth + (SoldierMaxHealth - oldMax));
            ToastView.Instance?.Key(LocalizationTable.Keys.ToastEliteEquipped);
            return true;
        }

        private float SoldierMaxHealth => _cfg.soldierMaxHealth *
            (IsEliteSoldier ? _cfg.eliteSoldierMultiplier : 1f);

        public void AssignTo(ColonyFacility facility, CreatureRole role)
        {
            if (role == CreatureRole.HospitalPatient) _roleBeforeHospital = _role;
            _facility = facility;
            _role = role;
            if (role == CreatureRole.FactoryWorker && WorkerSprite != null) _sr.sprite = WorkerSprite;
        }

        public void ReleaseFromFacility()
        {
            if (_role == CreatureRole.HospitalPatient)
                _role = _roleBeforeHospital == CreatureRole.Soldier ? CreatureRole.Soldier : CreatureRole.Free;
            else _role = CreatureRole.Free;
            _facility = null;
            RefreshBaseSprite();
            PickWander();
        }

        public void RetireFromDuty()
        {
            _facility?.RemoveOccupant(this);
            _facility = null;
            _role = CreatureRole.Free;
            _stage = CreatureStage.Elder;
            _eliteSoldier = false;
            RefreshBaseSprite();
            PickWander();
            _sim?.NotifyStageChanged();
        }

        public void EvictAsFreeAdult()
        {
            _facility = null;
            _role = CreatureRole.Free;
            _stage = StageForAge(_age);
            if (_stage == CreatureStage.Soldier) _stage = CreatureStage.Adult;
            _eliteSoldier = false;
            RefreshBaseSprite();
            PickWander();
        }

        public void CompleteFacilitySoldierTraining()
        {
            _facility = null;
            _role = CreatureRole.Soldier;
            _stage = CreatureStage.Adult;
            _combatHealth = _cfg.soldierMaxHealth;
            RefreshBaseSprite();
            _sim.NotifyStageChanged();
        }

        // Backward-compatible calls from the old BarracksZone component.
        public void BeginSoldierTraining() => _role = CreatureRole.BarracksTrainee;
        public void CompleteSoldierTraining() => CompleteFacilitySoldierTraining();

        public void Infect()
        {
            if (_infected) return;
            _infected = true;
            ToastView.Instance?.Key(LocalizationTable.Keys.ToastInfected);
        }

        public void Cure() => _infected = false;
        public void HealCombat(float amount)
        {
            if (IsSoldier) _combatHealth = Mathf.Min(SoldierMaxHealth, _combatHealth + amount);
        }

        public void TakeCombatDamage(float amount, Vector2 fromDirection)
        {
            if (_stage == CreatureStage.Infant || _stage == CreatureStage.Elder)
            {
                Die();
                return;
            }
            if (IsSoldier) _combatHealth -= amount;
            else _health -= amount;
            _hitTimer = .22f;
            _knockbackDir = fromDirection.sqrMagnitude > .01f ? -fromDirection.normalized : Vector2.left;
            if ((IsSoldier && _combatHealth <= 0f) || (!IsSoldier && _health <= 0f)) Die();
        }

        public void PlayAttack() { if (IsSoldier) _attackTimer = .28f; }
        public void PlayHit() { _hitTimer = .22f; _knockbackDir = -_wanderDir; }

        private void EnterStage(CreatureStage stage, bool initial = false)
        {
            _stage = stage;
            if (!initial && stage == CreatureStage.Elder && (_role == CreatureRole.FactoryWorker || IsSoldier))
            {
                RetireFromDuty();
                return;
            }
            RefreshBaseSprite();
            if (!initial) _sim?.NotifyStageChanged();
        }

        private void RefreshBaseSprite()
        {
            if (_role == CreatureRole.FactoryWorker) _sr.sprite = WorkerSprite ?? _adultSprite;
            else if (IsSoldier) _sr.sprite = SoldierSprite ?? _adultSprite;
            else if (_stage == CreatureStage.Infant) _sr.sprite = InfantSprite ?? _infantSprite;
            else if (_stage == CreatureStage.Elder) _sr.sprite = ElderSprite ?? _elderSprite;
            else _sr.sprite = AdultSprite ?? _adultSprite;
        }

        private void UpdateVisuals()
        {
            float scale = _stage == CreatureStage.Infant ? _cfg.infantScale
                : _stage == CreatureStage.Elder ? _cfg.elderScale
                : IsSoldier ? _cfg.primeScale * 1.08f : _cfg.primeScale;
            float bob = Mathf.Sin(Time.time * 9f + _wobblePhase) * .035f;
            transform.localScale = new Vector3(scale * (1f + bob), scale * (1f - bob), 1f);
            transform.localRotation = Quaternion.Euler(0, 0, _hitTimer > 0f ? Mathf.Sin(Time.time * 70f) * 5f : 0f);

            if (IsSoldier && _attackTimer > 0f && SoldierAttackFrames.Length > 0)
            {
                int index = Mathf.Clamp(Mathf.FloorToInt((1f - _attackTimer / _cfg.attackInterval) * SoldierAttackFrames.Length), 0, SoldierAttackFrames.Length - 1);
                _sr.sprite = SoldierAttackFrames[index];
            }
            else if (_role == CreatureRole.Free && _stage == CreatureStage.Adult && AdultWalkFrames.Length > 0)
                _sr.sprite = AdultWalkFrames[Mathf.FloorToInt(Time.time * 8f) % AdultWalkFrames.Length];
            else if (_hitTimer <= 0f) RefreshBaseSprite();

            Color baseColor = _infected ? new Color(.76f, .55f, .9f, 1f)
                : IsEliteSoldier ? new Color(1f, .84f, .42f, 1f) : Color.white;
            _sr.color = _hitTimer > 0f ? new Color(1f, .3f, .25f, 1f) : baseColor;
        }

        private static Sprite LoadSingle(ref Sprite cache, string path, float worldWidth)
        {
            if (cache != null) return cache;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            texture.filterMode = FilterMode.Point;
            cache = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), texture.width / worldWidth);
            return cache;
        }

        private static Sprite InfantSprite => LoadSingle(ref _infant, "Art/bug_infant", .75f);
        private static Sprite WorkerSprite => LoadSingle(ref _worker, "Art/bug_worker", 1f);
        private static Sprite ElderSprite => LoadSingle(ref _elder, "Art/ColonyV2/bug_elder_v2", 1.15f);
        private static Sprite SoldierSprite => LoadSingle(ref _soldier, "Art/bug_soldier", 1.15f);
        private static Sprite AdultSprite => AdultWalkFrames.Length > 0 ? AdultWalkFrames[0] : _adult;

        private static Sprite[] AdultWalkFrames => _adultWalk ?? (_adultWalk = LoadSheet("Art/bug_adult_walk", 4, 1f));
        private static Sprite[] SoldierAttackFrames => _soldierAttack ?? (_soldierAttack = LoadSheet("Art/bug_soldier_attack", 4, 1.15f));

        private static Sprite[] LoadSheet(string path, int count, float worldWidth)
        {
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return new Sprite[0];
            texture.filterMode = FilterMode.Point;
            int width = texture.width / count;
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = Sprite.Create(texture, new Rect(i * width, 0, width, texture.height), new Vector2(.5f, .5f), width / worldWidth);
            return frames;
        }

        private void Die()
        {
            if (_stage == CreatureStage.Elder || IsSoldier) _sim?.SpawnContamination(transform.position);
            GameEvents.RaiseCreatureDied(transform.position);
            _sim?.RemoveCreature(this);
            Destroy(gameObject);
        }
    }
}
