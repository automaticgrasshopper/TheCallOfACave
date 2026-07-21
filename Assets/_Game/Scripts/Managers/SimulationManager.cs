using System.Collections.Generic;
using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Gameplay;
using TCC.UI;

namespace TCC.Managers
{
    public class SimulationManager : Singleton<SimulationManager>
    {
        [Header("Config")]
        [SerializeField] private SimulationConfig _config;
        [Header("Entity prefabs")]
        [SerializeField] private Creature _creaturePrefab;
        [SerializeField] private Egg _eggPrefab;
        [SerializeField] private MetalPart _metalPartPrefab;
        [SerializeField] private ContaminationSource _contaminationPrefab;
        [SerializeField] private EnemyRobot _enemyPrefab;
        [SerializeField] private MedicalDoctor _doctorPrefab;
        [Header("Scene refs")]
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private Vector2 _activityMin = new Vector2(-9.2f, -5f);
        [SerializeField] private Vector2 _activityMax = new Vector2(1f, 5f);
        [SerializeField] private Vector2 _worldMin = new Vector2(-9.2f, -5f);
        [SerializeField] private Vector2 _worldMax = new Vector2(9.2f, 5f);
        [SerializeField] private Transform _birthCenter;
        [SerializeField] private float _birthRadius = 1.7f;
        [SerializeField] private LaborZone _labor;
        [SerializeField] private BarracksZone _barracks;

        private readonly List<Creature> _creatures = new List<Creature>(64);
        private readonly List<Egg> _eggs = new List<Egg>(32);
        private readonly List<EnemyRobot> _enemies = new List<EnemyRobot>(16);
        private readonly List<ContaminationSource> _contaminations = new List<ContaminationSource>(32);
        private readonly List<MedicalDoctor> _doctors = new List<MedicalDoctor>(16);
        private ColonyFacility[] _facilities;
        private float _invasionTimer;
        private bool _invasionStarted;

        public SimulationConfig Config => _config;
        public LaborZone Labor => _labor;
        public BarracksZone Barracks => _barracks;
        public Vector2 BirthCenterPosition => BirthCenter;
        public IReadOnlyList<Creature> Creatures => _creatures;
        public int CreatureCount => _creatures.Count;
        public int EggCount => _eggs.Count;

        public void RegisterBarracks(BarracksZone barracks) => _barracks = barracks;

        private void Start()
        {
            _facilities = FindObjectsOfType<ColonyFacility>(true);
            SpawnInitialPopulation();
            BroadcastPopulation();
        }

        private void Update()
        {
            if (_invasionStarted || _config == null) return;
            _invasionTimer += Time.deltaTime;
            if (_invasionTimer < _config.firstInvasionSeconds) return;
            _invasionStarted = true;
            SpawnEnemy(new Vector2(8.2f, Random.Range(-2.8f, 2.8f)));
            ToastView.Instance?.Key(LocalizationTable.Keys.ToastInvasion);
        }

        public Vector2 ClampToActivity(Vector2 point, float margin = .3f)
            => new Vector2(Mathf.Clamp(point.x, _activityMin.x + margin, _activityMax.x - margin),
                Mathf.Clamp(point.y, _activityMin.y + margin, _activityMax.y - margin));

        public Vector2 ClampWorld(Vector2 point, float margin = .25f)
            => new Vector2(Mathf.Clamp(point.x, _worldMin.x + margin, _worldMax.x - margin),
                Mathf.Clamp(point.y, _worldMin.y + margin, _worldMax.y - margin));

        private Vector2 BirthCenter => _birthCenter != null ? (Vector2)_birthCenter.position : new Vector2(-6.8f, -3.2f);
        public Vector2 RandomInBirth() => ClampToActivity(BirthCenter + Random.insideUnitCircle * (_birthRadius * .72f));

        private void SpawnInitialPopulation()
        {
            for (int i = 0; i < _config.startInfants; i++) SpawnCreature(RandomInBirth(), 0f, false);
            for (int i = 0; i < _config.startAdults; i++) SpawnCreature(RandomInBirth(), _config.juvenileSeconds / _config.totalLifespanSeconds, false);
            for (int i = 0; i < _config.startEggs; i++) SpawnEgg(RandomInBirth(), false);
        }

        public Creature SpawnJuvenile(Vector2 pos) => SpawnCreature(pos, 0f, true);

        private Creature SpawnCreature(Vector2 pos, float ageFraction, bool notify)
        {
            if (_creaturePrefab == null || _creatures.Count >= _config.maxCreatures) return null;
            var creature = Instantiate(_creaturePrefab, pos, Quaternion.identity, _worldRoot);
            creature.Init(this, _config, ageFraction);
            _creatures.Add(creature);
            if (notify) GameEvents.RaiseCreatureBorn(pos);
            BroadcastPopulation();
            return creature;
        }

        private void SpawnEgg(Vector2 pos, bool notify)
        {
            if (_eggPrefab == null || _eggs.Count >= _config.maxEggs) return;
            var egg = Instantiate(_eggPrefab, pos, Quaternion.identity, _worldRoot);
            egg.Init(this, _config);
            _eggs.Add(egg);
            if (notify) GameEvents.RaiseEggLaid(pos);
        }

        public void LayEgg(Vector2 pos) => SpawnEgg(ClampToActivity(pos + Random.insideUnitCircle * .35f), true);

        public void TryBuyJuvenile()
        {
            int cost = EconomyManager.Exists ? EconomyManager.Instance.BuyJuvenileCost : 0;
            GameEvents.RaiseSpendRequested(cost, ok =>
            {
                if (ok)
                {
                    SpawnJuvenile(RandomInBirth());
                    ToastView.Instance?.Key(LocalizationTable.Keys.ToastBugBought);
                }
                else ToastView.Instance?.Key(LocalizationTable.Keys.ToastInsufficientFunds);
            });
        }

        public ColonyFacility FindFacilityAt(Vector2 point)
        {
            if (_facilities == null) _facilities = FindObjectsOfType<ColonyFacility>(true);
            foreach (var facility in _facilities)
                if (facility != null && facility.Contains(point)) return facility;
            return null;
        }

        public Vector2 PushOutsideFacilities(Vector2 point, ColonyFacility allowed)
        {
            if (_facilities == null) _facilities = FindObjectsOfType<ColonyFacility>(true);
            foreach (var facility in _facilities)
            {
                if (facility == null || facility == allowed || !facility.IsBuilt) continue;
                Vector2 delta = point - facility.Center;
                float radius = facility.Radius + .22f;
                if (delta.sqrMagnitude >= radius * radius) continue;
                if (delta.sqrMagnitude < .001f) delta = Vector2.down;
                point = facility.Center + delta.normalized * radius;
            }
            return ClampWorld(point);
        }

        public void SpawnMetalPart(Vector2 position)
        {
            var part = _metalPartPrefab != null
                ? Instantiate(_metalPartPrefab, position, Quaternion.identity, _worldRoot)
                : CreateEntity<MetalPart>("Metal Part", "Art/metal_part", .55f, position);
            part.Init(_config.factoryPartValue);
            ToastView.Instance?.Key(LocalizationTable.Keys.ToastPartReady);
        }

        public void SpawnContamination(Vector2 position)
        {
            var source = _contaminationPrefab != null
                ? Instantiate(_contaminationPrefab, position, Quaternion.identity, _worldRoot)
                : CreateEntity<ContaminationSource>("Contamination", "Art/ColonyV2/contamination_oil", 1.35f, position);
            _contaminations.Add(source);
        }

        public void SpawnDoctor(Vector2 position, ColonyFacility home)
        {
            var doctor = _doctorPrefab != null
                ? Instantiate(_doctorPrefab, position, Quaternion.identity, _worldRoot)
                : CreateEntity<MedicalDoctor>("Medical Doctor", "Art/ColonyV2/bug_doctor", 1.05f, position);
            doctor.Init(this, home);
            _doctors.Add(doctor);
        }

        private void SpawnEnemy(Vector2 position)
        {
            var enemy = _enemyPrefab != null
                ? Instantiate(_enemyPrefab, position, Quaternion.identity, _worldRoot)
                : CreateEntity<EnemyRobot>("Scavenger Robot", "Art/ColonyV2/enemy_robot", 1.15f, position);
            enemy.Init(this);
            _enemies.Add(enemy);
        }

        private T CreateEntity<T>(string name, string resource, float width, Vector2 position) where T : Component
        {
            var go = new GameObject(name, typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(T));
            go.transform.SetParent(_worldRoot, false);
            go.transform.position = position;
            var texture = Resources.Load<Texture2D>(resource);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), texture.width / width);
                var renderer = go.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 8;
            }
            go.GetComponent<CircleCollider2D>().radius = .42f;
            return go.GetComponent<T>();
        }

        public EnemyRobot ClosestEnemy(Vector2 point)
        {
            _enemies.RemoveAll(e => e == null || !e.Alive);
            EnemyRobot best = null; float distance = float.MaxValue;
            foreach (var enemy in _enemies)
            {
                float d = (enemy.Position - point).sqrMagnitude;
                if (d < distance) { distance = d; best = enemy; }
            }
            return best;
        }

        public Creature ClosestSoldier(Vector2 point)
        {
            Creature best = null; float distance = float.MaxValue;
            foreach (var creature in _creatures)
            {
                if (creature == null || !creature.IsSoldier) continue;
                float d = (creature.Position - point).sqrMagnitude;
                if (d < distance) { distance = d; best = creature; }
            }
            return best;
        }

        public ContaminationSource ClosestContamination(Vector2 point)
        {
            _contaminations.RemoveAll(c => c == null);
            ContaminationSource best = null; float distance = float.MaxValue;
            foreach (var source in _contaminations)
            {
                float d = (source.Position - point).sqrMagnitude;
                if (d < distance) { distance = d; best = source; }
            }
            return best;
        }

        public void RemoveEnemy(EnemyRobot enemy) => _enemies.Remove(enemy);
        public void NotifyStageChanged() => BroadcastPopulation();
        public void RemoveEgg(Egg egg) => _eggs.Remove(egg);

        public void RemoveCreature(Creature creature)
        {
            if (!_creatures.Remove(creature)) return;
            BroadcastPopulation();
            if (_creatures.Count == 0 && GameManager.Exists && GameManager.Instance.State == GameState.Playing)
                GameManager.Instance.SetState(GameState.GameOver);
        }

        private void BroadcastPopulation()
        {
            int infants = 0, adults = 0;
            foreach (var creature in _creatures)
            {
                if (creature == null) continue;
                if (creature.Stage == CreatureStage.Infant) infants++;
                else adults++;
            }
            GameEvents.RaisePopulationChanged(infants, adults);
        }
    }
}
