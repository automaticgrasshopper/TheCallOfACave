using System;
using UnityEditor;
using UnityEngine;
using TCC.Gameplay;
using TCC.Persistence;

namespace TCC.EditorTools
{
    public static class WorldSnapshotValidation
    {
        [MenuItem("TCC/Validation/Run World Snapshot Tests")]
        public static void RunFromMenu()
        {
            RunAssertions();
            Debug.Log("[TCC Snapshot] PASS: world snapshot round trip preserved entities and values.");
        }

        public static void RunBatch()
        {
            try
            {
                RunAssertions();
                Debug.Log("[TCC Snapshot] PASS: world snapshot round trip preserved entities and values.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[TCC Snapshot] FAIL: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(2);
                throw;
            }
        }

        private static void RunAssertions()
        {
            string profileId = Guid.NewGuid().ToString("N");
            string facilityId = Guid.NewGuid().ToString("N");
            string workerId = Guid.NewGuid().ToString("N");
            var snapshot = WorldSnapshot.Create(profileId, 1784995200000L);
            snapshot.money = 2468;
            snapshot.colonyYear = 27;
            snapshot.sessionSeconds = 132.75d;
            snapshot.randomSeed = 73531;

            snapshot.creatures.Add(new CreatureSnapshot
            {
                id = workerId,
                position = new WorldPosition(-4.5f, 1.25f),
                stage = CreatureStage.Adult,
                role = CreatureRole.FactoryWorker,
                facilityId = facilityId,
                ageSeconds = 45f,
                lifespanSeconds = 180f,
                elderStartAgeSeconds = 145f,
                productionEfficiency = 1.08f,
                health = 83f,
                hunger = 71f,
                combatHealth = 40f,
                layTimerSeconds = 12f,
                code = "QA-401"
            });
            snapshot.creatures.Add(new CreatureSnapshot
            {
                id = Guid.NewGuid().ToString("N"),
                position = new WorldPosition(-2f, -1f),
                stage = CreatureStage.Adult,
                role = CreatureRole.Soldier,
                ageSeconds = 62f,
                lifespanSeconds = 170f,
                elderStartAgeSeconds = 140f,
                productionEfficiency = .94f,
                health = 92f,
                hunger = 66f,
                combatHealth = 115f,
                layTimerSeconds = 18f,
                eliteSoldier = true,
                code = "QB-402"
            });
            snapshot.eggs.Add(new EggSnapshot
            {
                id = Guid.NewGuid().ToString("N"),
                position = new WorldPosition(-6f, -2.5f),
                hatchRemainingSeconds = 9.5f,
                hatchDurationSeconds = 24f
            });
            snapshot.inventory.Add(new InventoryStackSnapshot
            {
                id = "inventory.food",
                itemType = InventoryItemType.Food,
                count = 7
            });
            snapshot.inventory.Add(new InventoryStackSnapshot
            {
                id = "inventory.refined-component",
                itemType = InventoryItemType.RefinedComponent,
                count = 3
            });
            snapshot.worldItems.Add(new WorldItemSnapshot
            {
                id = Guid.NewGuid().ToString("N"),
                kind = WorldItemKind.Food,
                itemType = InventoryItemType.Food,
                amount = 1,
                position = new WorldPosition(-3.25f, .5f),
                facilityId = facilityId
            });
            snapshot.facilities.Add(new FacilitySnapshot
            {
                id = facilityId,
                facilityType = FacilityType.Factory,
                position = new WorldPosition(-4f, 2f),
                level = 2,
                structureHealth = 105f,
                academyTimerSeconds = -1f,
                occupants =
                {
                    new FacilityOccupantSnapshot
                    {
                        creatureId = workerId,
                        taskTimerSeconds = 6.25f
                    }
                }
            });
            snapshot.timers.Add(new PersistentTimerSnapshot
            {
                id = "simulation.invasion",
                elapsedSeconds = 118.5d,
                counter = 2
            });

            string json = WorldSnapshotJson.Serialize(snapshot, true);
            WorldSnapshot restored = WorldSnapshotJson.Deserialize(json);

            Equal(snapshot.snapshotId, restored.snapshotId, "snapshot ID");
            Equal(profileId, restored.profileId, "profile ID");
            Equal(2468, restored.money, "money");
            Equal(27, restored.colonyYear, "colony year");
            Equal(132.75d, restored.sessionSeconds, "session seconds");
            Equal(73531, restored.randomSeed, "random seed");
            Equal(2, restored.creatures.Count, "creature count");
            Equal(1, restored.eggs.Count, "egg count");
            Equal(2, restored.inventory.Count, "inventory count");
            Equal(1, restored.worldItems.Count, "world item count");
            Equal(1, restored.facilities.Count, "facility count");
            Equal(1, restored.timers.Count, "timer count");
            Equal(workerId, restored.facilities[0].occupants[0].creatureId, "occupant link");
            Equal(105f, restored.facilities[0].structureHealth, "facility health");
            Equal(9.5f, restored.eggs[0].hatchRemainingSeconds, "egg timer");
            Equal(83f, restored.creatures[0].health, "creature health");
            Equal(3, restored.inventory[1].count, "inventory value");
        }

        private static void Equal<T>(T expected, T actual, string field)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    $"{field} mismatch: expected '{expected}', got '{actual}'.");
        }
    }
}
