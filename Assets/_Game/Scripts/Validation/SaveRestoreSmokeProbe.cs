using System;
using System.Linq;
using UnityEngine;
using TCC.Core;
using TCC.Gameplay;
using TCC.Managers;
using TCC.Persistence;

namespace TCC.Validation
{
    public sealed class SaveRestoreSmokeProbe : MonoBehaviour
    {
        private string _mode;
        private string _repositoryRoot;
        private float _elapsed;
        private bool _ran;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartWhenRequested()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!args.Contains("-tccSaveRestoreSmoke")) return;

            GameManager.BootIntoPlay = true;
            var host = new GameObject("[Day 5 Save Restore Probe]");
            DontDestroyOnLoad(host);
            host.AddComponent<SaveRestoreSmokeProbe>();
        }

        private void Awake()
        {
            _mode = ReadArgument("-tccSaveRestoreMode");
            _repositoryRoot = ReadArgument("-tccSaveRoot");
        }

        private void Update()
        {
            if (_ran) return;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < 2f) return;
            _ran = true;

            try
            {
                if (_mode == "write") WriteSnapshot();
                else if (_mode == "read") ReadAndValidateSnapshot();
                else throw new InvalidOperationException("Unknown save/restore smoke mode.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[TCC SaveRestore] FAIL: " + exception);
                Application.Quit(2);
            }
        }

        private void WriteSnapshot()
        {
            ValidateManagers();
            var repository = new ProfileRepository(_repositoryRoot);
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PlayerProfile profile = PlayerProfile.Create("保存测试者", "恢复测试者", now);
            repository.SaveProfile(profile);
            SaveGameController controller =
                SaveGameController.StartSession(profile.ProfileId, repository, false);
            Equal(60f, SaveGameController.AutoSaveIntervalSeconds, "autosave interval");

            GameEvents.RaiseMoneyEarned(5000);
            BuildFacility(FacilityType.Factory, new Vector2(-5.5f, 2f));
            Equal("facility-built", controller.LastSaveReason, "build save trigger");
            SimulationManager.Instance.SpawnJuvenile(new Vector2(-5.8f, -2.5f));
            InventoryManager.Instance.Add(InventoryItemType.RefinedComponent, 4);
            GameManager.Instance.RestoreSessionSeconds(123.5d);
            controller.SaveNow("cross-process-test");

            WorldSnapshot saved = repository.LoadWorldSnapshot(profile.ProfileId).Snapshot;
            if (saved.creatures.Count != SimulationManager.Instance.CreatureCount ||
                saved.facilities.Count != BuildingPlacementManager.Instance.BuiltFacilityCount)
                throw new InvalidOperationException("Saved entity counts do not match the live world.");

            Debug.Log(
                $"[TCC SaveRestore] WRITE PASS: profile={profile.ProfileId}, " +
                $"money={saved.money}, creatures={saved.creatures.Count}, " +
                $"eggs={saved.eggs.Count}, facilities={saved.facilities.Count}, " +
                $"session={saved.sessionSeconds:0.0}.");
            Application.Quit(0);
        }

        private void ReadAndValidateSnapshot()
        {
            ValidateManagers();
            var repository = new ProfileRepository(_repositoryRoot);
            ProfileIndex index = repository.LoadIndex();
            if (index.Profiles.Count != 1)
                throw new InvalidOperationException("Expected one saved profile.");

            string profileId = index.Profiles[0].ProfileId;
            WorldSnapshot expected = repository.LoadWorldSnapshot(profileId).Snapshot;
            SaveGameController.StartSession(profileId, repository, true);

            Equal(expected.money, EconomyManager.Instance.Money, "money");
            Equal(expected.creatures.Count, SimulationManager.Instance.CreatureCount, "creatures");
            Equal(expected.eggs.Count, SimulationManager.Instance.EggCount, "eggs");
            Equal(
                expected.facilities.Count,
                BuildingPlacementManager.Instance.BuiltFacilityCount,
                "facilities");
            InventoryStackSnapshot refined = expected.inventory.First(stack =>
                stack.itemType == InventoryItemType.RefinedComponent);
            Equal(
                refined.count,
                InventoryManager.Instance.Count(InventoryItemType.RefinedComponent),
                "inventory");
            if (GameManager.Instance.SessionSeconds < expected.sessionSeconds ||
                GameManager.Instance.SessionSeconds > expected.sessionSeconds + 2f)
                throw new InvalidOperationException("Session time was not restored.");

            Debug.Log(
                $"[TCC SaveRestore] READ PASS: profile={profileId}, " +
                $"money={EconomyManager.Instance.Money}, " +
                $"creatures={SimulationManager.Instance.CreatureCount}, " +
                $"eggs={SimulationManager.Instance.EggCount}, " +
                $"facilities={BuildingPlacementManager.Instance.BuiltFacilityCount}, " +
                $"session={GameManager.Instance.SessionSeconds:0.0}.");
            Application.Quit(0);
        }

        private static void BuildFacility(FacilityType type, Vector2 worldPosition)
        {
            Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            BuildingPlacementManager placement = BuildingPlacementManager.Instance;
            if (!placement.BeginPlacement(type, screenPosition) ||
                !placement.FinishPlacement(screenPosition))
                throw new InvalidOperationException($"Unable to place test {type}.");
        }

        private static void ValidateManagers()
        {
            if (!GameManager.Exists || !SimulationManager.Exists ||
                !EconomyManager.Exists || !InventoryManager.Exists ||
                !BuildingPlacementManager.Exists || Camera.main == null)
                throw new InvalidOperationException("Required runtime managers are missing.");
        }

        private static string ReadArgument(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, key);
            if (index < 0 || index + 1 >= args.Length)
                throw new InvalidOperationException($"Missing command-line argument {key}.");
            return args[index + 1];
        }

        private static void Equal<T>(T expected, T actual, string field)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    $"{field} mismatch: expected '{expected}', got '{actual}'.");
        }
    }
}
