using System.Collections.Generic;
using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Gameplay;

namespace TCC.Managers
{
    /// <summary>
    /// The beating heart of the colony: it spawns and tracks every bug and egg,
    /// enforces the population caps, and owns the world geometry (the rectangular
    /// activity area, the nursery circle bugs are born into, and the labor circle).
    /// Gameplay entities talk to THIS manager; cross-cutting facts (money, audio, UI)
    /// still go out via <see cref="GameEvents"/>.
    /// </summary>
    public class SimulationManager : Singleton<SimulationManager>
    {
        [Header("Config")]
        [SerializeField] private SimulationConfig _config;

        [Header("Prefabs")]
        [SerializeField] private Creature _creaturePrefab;
        [SerializeField] private Egg _eggPrefab;

        [Header("Scene refs")]
        [Tooltip("Parent for all spawned bugs and eggs.")]
        [SerializeField] private Transform _worldRoot;
        [Tooltip("Bottom-left rectangle the bugs are free to wander (world space).")]
        [SerializeField] private Vector2 _activityMin = new Vector2(-9.2f, -5f);
        [SerializeField] private Vector2 _activityMax = new Vector2(1f, 5f);
        [Tooltip("The nursery circle bugs are born into.")]
        [SerializeField] private Transform _birthCenter;
        [SerializeField] private float _birthRadius = 1.7f;
        [Tooltip("The top-left labor circle where prime bugs work.")]
        [SerializeField] private LaborZone _labor;

        private readonly List<Creature> _creatures = new List<Creature>(64);
        private readonly List<Egg> _eggs = new List<Egg>(32);

        public SimulationConfig Config => _config;
        public LaborZone Labor => _labor;
        public int CreatureCount => _creatures.Count;
        public int EggCount => _eggs.Count;

        private void Start()
        {
            SpawnInitialPopulation();
            BroadcastPopulation();
        }

        // ---- geometry ----------------------------------------------------
        public Vector2 ClampToActivity(Vector2 world, float margin = 0.3f)
        {
            return new Vector2(
                Mathf.Clamp(world.x, _activityMin.x + margin, _activityMax.x - margin),
                Mathf.Clamp(world.y, _activityMin.y + margin, _activityMax.y - margin));
        }

        private Vector2 BirthCenter => _birthCenter != null ? (Vector2)_birthCenter.position : Vector2.zero;

        public Vector2 RandomInBirth()
            => ClampToActivity(BirthCenter + Random.insideUnitCircle * (_birthRadius * 0.8f));

        private Vector2 RandomActivityPoint(float margin = 0.6f)
            => new Vector2(
                Random.Range(_activityMin.x + margin, _activityMax.x - margin),
                Random.Range(_activityMin.y + margin, _activityMax.y - margin));

        // ---- spawning ----------------------------------------------------
        private void SpawnInitialPopulation()
        {
            for (int i = 0; i < _config.startInfants; i++)
                SpawnCreature(RandomInBirth(), 0f, notifyBirth: false);
            for (int i = 0; i < _config.startAdults; i++)
                SpawnCreature(RandomActivityPoint(), Random.Range(0.25f, 0.6f), notifyBirth: false);
        }

        public Creature SpawnJuvenile(Vector2 pos) => SpawnCreature(pos, 0f, notifyBirth: true);

        private Creature SpawnCreature(Vector2 pos, float ageFraction, bool notifyBirth)
        {
            if (_creaturePrefab == null || _creatures.Count >= _config.maxCreatures) return null;
            var c = Instantiate(_creaturePrefab, pos, Quaternion.identity, _worldRoot);
            c.Init(this, _config, ageFraction);
            _creatures.Add(c);
            if (notifyBirth) GameEvents.RaiseCreatureBorn(pos);
            BroadcastPopulation();
            return c;
        }

        public void LayEgg(Vector2 pos)
        {
            if (_eggPrefab == null || _eggs.Count >= _config.maxEggs) return;
            var e = Instantiate(_eggPrefab, pos, Quaternion.identity, _worldRoot);
            e.Init(this, _config);
            _eggs.Add(e);
            GameEvents.RaiseEggLaid(pos);
        }

        /// <summary>Buy one juvenile into the nursery, if the wallet allows.</summary>
        public void TryBuyJuvenile()
        {
            int cost = EconomyManager.Exists ? EconomyManager.Instance.BuyJuvenileCost : 0;
            GameEvents.RaiseSpendRequested(cost, ok =>
            {
                if (ok) SpawnJuvenile(RandomInBirth());
            });
        }

        /// <summary>Called by a creature when it ages/changes stage so the HUD count updates.</summary>
        public void NotifyStageChanged() => BroadcastPopulation();

        public void RemoveEgg(Egg e) => _eggs.Remove(e);

        public void RemoveCreature(Creature c)
        {
            if (_creatures.Remove(c)) BroadcastPopulation();
        }

        // ---- population UI ----------------------------------------------
        private void BroadcastPopulation()
        {
            int infants = 0, adults = 0;
            for (int i = 0; i < _creatures.Count; i++)
            {
                var c = _creatures[i];
                if (c == null) continue;
                if (c.Stage == CreatureStage.Infant) infants++;
                else adults++;
            }
            GameEvents.RaisePopulationChanged(infants, adults);
        }
    }
}
