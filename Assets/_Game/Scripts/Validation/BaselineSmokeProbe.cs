using System;
using System.Linq;
using UnityEngine;
using TCC.Core;
using TCC.Gameplay;
using TCC.Managers;

namespace TCC.Validation
{
    /// <summary>
    /// Opt-in, built-player smoke probe for the frozen pre-production baseline.
    /// It is dormant in normal launches and only starts with -tccBaselineSmoke.
    /// </summary>
    public sealed class BaselineSmokeProbe : MonoBehaviour
    {
        private const float DefaultDurationSeconds = 360f;
        private const float SetupDelaySeconds = 2f;
        private const float AdultAssignmentDelaySeconds = 34f;
        private const float SupportIntervalSeconds = 12f;
        private const int MinimumPopulation = 8;

        private float _durationSeconds;
        private float _elapsed;
        private float _nextCheckpoint = 30f;
        private float _nextSupport = SupportIntervalSeconds;
        private bool _setupComplete;
        private bool _rolesAssigned;
        private int _initialMoney;
        private int _initialCreatures;
        private int _initialEggs;
        private int _peakCreatures;
        private int _peakEggs;
        private int _peakEnemies;
        private bool _sawHeavyEnemy;
        private bool _sawFactoryProduct;
        private bool _sawWorker;
        private bool _sawSoldier;
        private int _gameOverRecoveries;
        private int _runtimeErrors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartWhenRequested()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!args.Contains("-tccBaselineSmoke")) return;

            GameManager.BootIntoPlay = true;
            var host = new GameObject("[Day 1 Baseline Smoke Probe]");
            DontDestroyOnLoad(host);
            host.AddComponent<BaselineSmokeProbe>();
        }

        private void Awake()
        {
            _durationSeconds = ReadFloatArgument("-tccSmokeSeconds", DefaultDurationSeconds);
            Application.logMessageReceived += OnLog;
            Debug.Log($"[TCC Baseline] Starting {_durationSeconds:0}-second built-player smoke run.");
        }

        private void OnDestroy() => Application.logMessageReceived -= OnLog;

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;

            if (!_setupComplete && _elapsed >= SetupDelaySeconds)
            {
                if (!ValidateSceneWiring()) return;
                if (!CaptureInitialState()) return;
                try
                {
                    ExerciseOpeningLoop();
                }
                catch (Exception exception)
                {
                    Fail("Opening-loop setup failed: " + exception.Message);
                    return;
                }
                _setupComplete = true;
            }

            if (!_setupComplete) return;

            KeepColonyObservable();
            ObserveLoop();

            if (!_rolesAssigned && _elapsed >= AdultAssignmentDelaySeconds)
            {
                AssignFacilityRoles();
                _rolesAssigned = true;
            }

            if (_elapsed >= _nextCheckpoint)
            {
                LogCheckpoint();
                _nextCheckpoint += 30f;
            }

            if (_elapsed >= _durationSeconds) Finish();
        }

        private bool ValidateSceneWiring()
        {
            string missing = string.Join(", ", new[]
            {
                GameManager.Exists ? null : nameof(GameManager),
                SimulationManager.Exists ? null : nameof(SimulationManager),
                EconomyManager.Exists ? null : nameof(EconomyManager),
                InventoryManager.Exists ? null : nameof(InventoryManager),
                BuildingPlacementManager.Exists ? null : nameof(BuildingPlacementManager),
                UIManager.Exists ? null : nameof(UIManager)
            }.Where(value => value != null));

            if (!string.IsNullOrEmpty(missing))
            {
                Fail("Missing required scene managers: " + missing);
                return false;
            }

            if (GameManager.Instance.State != GameState.Playing)
            {
                Fail("Main scene did not enter Playing state.");
                return false;
            }

            return true;
        }

        private bool CaptureInitialState()
        {
            _initialMoney = EconomyManager.Instance.Money;
            _initialCreatures = SimulationManager.Instance.CreatureCount;
            _initialEggs = SimulationManager.Instance.EggCount;

            if (_initialMoney != 300 || _initialCreatures != 3 || _initialEggs != 2)
            {
                Fail($"Opening state changed: money={_initialMoney}, creatures={_initialCreatures}, eggs={_initialEggs}.");
                return false;
            }

            Debug.Log("[TCC Baseline] Opening state verified: 300 coins, 3 larvae, 2 eggs.");
            return true;
        }

        private void ExerciseOpeningLoop()
        {
            GameEvents.RaiseMoneyEarned(5000);
            BuildFacility(FacilityType.Factory, new Vector2(-5.5f, 2f));
            BuildFacility(FacilityType.Barracks, new Vector2(-1f, 2f));
        }

        private static void BuildFacility(FacilityType type, Vector2 worldPosition)
        {
            Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            var placement = BuildingPlacementManager.Instance;
            if (!placement.BeginPlacement(type, screenPosition) ||
                !placement.FinishPlacement(screenPosition))
                throw new InvalidOperationException($"Unable to place baseline {type} at {worldPosition}.");
        }

        private void AssignFacilityRoles()
        {
            var freeAdults = SimulationManager.Instance.Creatures
                .Where(creature => creature != null && creature.IsFreeAdult)
                .ToList();
            var factory = FindObjectsOfType<ColonyFacility>()
                .FirstOrDefault(facility => facility.Type == FacilityType.Factory && facility.IsBuilt);
            var barracks = FindObjectsOfType<ColonyFacility>()
                .FirstOrDefault(facility => facility.Type == FacilityType.Barracks && facility.IsBuilt);

            if (factory != null && freeAdults.Count > 0)
            {
                factory.TryAssign(freeAdults[0]);
                freeAdults.RemoveAt(0);
            }
            if (barracks != null && freeAdults.Count > 0)
                barracks.TryAssign(freeAdults[0]);
        }

        private void KeepColonyObservable()
        {
            if (_elapsed < _nextSupport) return;
            _nextSupport += SupportIntervalSeconds;

            var simulation = SimulationManager.Instance;
            while (simulation.CreatureCount < MinimumPopulation)
                simulation.SpawnJuvenile(simulation.RandomInBirth());

            if (GameManager.Instance.State == GameState.GameOver)
            {
                _gameOverRecoveries++;
                GameManager.Instance.SetState(GameState.Playing);
            }
        }

        private void ObserveLoop()
        {
            var simulation = SimulationManager.Instance;
            _peakCreatures = Mathf.Max(_peakCreatures, simulation.CreatureCount);
            _peakEggs = Mathf.Max(_peakEggs, simulation.EggCount);

            var enemies = FindObjectsOfType<EnemyRobot>();
            _peakEnemies = Mathf.Max(_peakEnemies, enemies.Length);
            _sawHeavyEnemy |= enemies.Any(enemy => enemy.IsHeavy);
            _sawFactoryProduct |= InventoryManager.Instance.Count(InventoryItemType.MetalScrap) > 0 ||
                InventoryManager.Instance.Count(InventoryItemType.RefinedComponent) > 0 ||
                InventoryManager.Instance.Count(InventoryItemType.AdvancedPartA) > 0 ||
                InventoryManager.Instance.Count(InventoryItemType.AdvancedPartB) > 0;
            _sawWorker |= simulation.Creatures.Any(creature => creature != null && creature.IsWorking);
            _sawSoldier |= simulation.Creatures.Any(creature => creature != null && creature.IsSoldier);
        }

        private void LogCheckpoint()
        {
            Debug.Log($"[TCC Baseline] t={_elapsed:0}s, creatures={SimulationManager.Instance.CreatureCount}, " +
                $"eggs={SimulationManager.Instance.EggCount}, enemies={FindObjectsOfType<EnemyRobot>().Length}, " +
                $"year={GameManager.Instance.ColonyYear}, money={EconomyManager.Instance.Money}.");
        }

        private void Finish()
        {
            bool fullRun = _durationSeconds >= DefaultDurationSeconds - 1f;
            bool passed = _setupComplete &&
                _runtimeErrors == 0 &&
                GameManager.Instance.SessionSeconds >= _durationSeconds * 0.8f &&
                _peakCreatures >= _initialCreatures &&
                _peakEggs >= _initialEggs;

            if (fullRun)
                passed &= _peakEnemies > 0 &&
                    _sawHeavyEnemy &&
                    _sawFactoryProduct &&
                    _sawWorker &&
                    _sawSoldier;

            string summary = $"mode={(fullRun ? "full" : "preflight")}, duration={_elapsed:0}s, " +
                $"session={GameManager.Instance.SessionSeconds:0}s, " +
                $"peakCreatures={_peakCreatures}, peakEggs={_peakEggs}, peakEnemies={_peakEnemies}, " +
                $"heavy={_sawHeavyEnemy}, factoryProduct={_sawFactoryProduct}, worker={_sawWorker}, " +
                $"soldier={_sawSoldier}, gameOverRecoveries={_gameOverRecoveries}, " +
                $"runtimeErrors={_runtimeErrors}";

            if (passed)
            {
                Debug.Log("[TCC Baseline] PASS: " + summary);
                Application.Quit(0);
            }
            else
            {
                Debug.LogError("[TCC Baseline] FAIL: " + summary);
                Application.Quit(2);
            }

            enabled = false;
        }

        private void Fail(string message)
        {
            Debug.LogError("[TCC Baseline] FAIL: " + message);
            Application.Quit(2);
            enabled = false;
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _runtimeErrors++;
        }

        private static float ReadFloatArgument(string key, float fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, key);
            if (index < 0 || index + 1 >= args.Length) return fallback;
            return float.TryParse(args[index + 1], out float value) && value > 0f ? value : fallback;
        }
    }
}
