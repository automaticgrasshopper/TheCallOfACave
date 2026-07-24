using System;
using System.Collections.Generic;
using UnityEngine;
using TCC.Gameplay;

namespace TCC.Persistence
{
    public enum WorldItemKind
    {
        Food = 1,
        Equipment = 2,
        EnemyPart = 3,
        CoinPart = 4,
        Contamination = 5
    }

    [Serializable]
    public struct WorldPosition
    {
        public float x;
        public float y;

        public WorldPosition(float xValue, float yValue)
        {
            x = xValue;
            y = yValue;
        }

        public Vector2 ToVector2() => new Vector2(x, y);
        public static WorldPosition From(Vector2 value) => new WorldPosition(value.x, value.y);
    }

    [Serializable]
    public sealed class CreatureSnapshot
    {
        public string id;
        public WorldPosition position;
        public CreatureStage stage;
        public CreatureRole role;
        public string facilityId;
        public float ageSeconds;
        public float lifespanSeconds;
        public float elderStartAgeSeconds;
        public float productionEfficiency;
        public float health;
        public float hunger;
        public float combatHealth;
        public float layTimerSeconds;
        public bool infected;
        public bool eliteSoldier;
        public bool advancedSoldier;
        public string code;
    }

    [Serializable]
    public sealed class EggSnapshot
    {
        public string id;
        public WorldPosition position;
        public float hatchRemainingSeconds;
        public float hatchDurationSeconds;
    }

    [Serializable]
    public sealed class InventoryStackSnapshot
    {
        public string id;
        public InventoryItemType itemType;
        public int count;
    }

    [Serializable]
    public sealed class WorldItemSnapshot
    {
        public string id;
        public WorldItemKind kind;
        public InventoryItemType itemType;
        public int amount;
        public WorldPosition position;
        public string facilityId;
    }

    [Serializable]
    public sealed class FacilityOccupantSnapshot
    {
        public string creatureId;
        public float taskTimerSeconds;
    }

    [Serializable]
    public sealed class FacilitySnapshot
    {
        public string id;
        public FacilityType facilityType;
        public WorldPosition position;
        public int level;
        public float structureHealth;
        public float academyTimerSeconds;
        public List<FacilityOccupantSnapshot> occupants = new List<FacilityOccupantSnapshot>();
    }

    [Serializable]
    public sealed class PersistentTimerSnapshot
    {
        public string id;
        public double elapsedSeconds;
        public int counter;
    }

    [Serializable]
    public sealed class WorldSnapshot
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion;
        public string snapshotId;
        public string profileId;
        public long capturedAtUnixMilliseconds;
        public int money;
        public int colonyYear;
        public double sessionSeconds;
        public int randomSeed;
        public List<CreatureSnapshot> creatures;
        public List<EggSnapshot> eggs;
        public List<InventoryStackSnapshot> inventory;
        public List<WorldItemSnapshot> worldItems;
        public List<FacilitySnapshot> facilities;
        public List<PersistentTimerSnapshot> timers;

        public static WorldSnapshot Create(string ownerProfileId, long capturedAt)
        {
            if (!Guid.TryParseExact(ownerProfileId, "N", out _))
                throw new ArgumentException("Owner profile ID is malformed.", nameof(ownerProfileId));
            if (capturedAt <= 0)
                throw new ArgumentOutOfRangeException(nameof(capturedAt));

            return new WorldSnapshot
            {
                schemaVersion = CurrentSchemaVersion,
                snapshotId = Guid.NewGuid().ToString("N"),
                profileId = ownerProfileId,
                capturedAtUnixMilliseconds = capturedAt,
                colonyYear = 1,
                creatures = new List<CreatureSnapshot>(),
                eggs = new List<EggSnapshot>(),
                inventory = new List<InventoryStackSnapshot>(),
                worldItems = new List<WorldItemSnapshot>(),
                facilities = new List<FacilitySnapshot>(),
                timers = new List<PersistentTimerSnapshot>()
            };
        }

        public void EnsureValid()
        {
            if (schemaVersion != CurrentSchemaVersion)
                throw new InvalidOperationException($"Unsupported world snapshot schema: {schemaVersion}.");
            RequireId(snapshotId, "snapshot ID");
            RequireId(profileId, "profile ID");
            if (capturedAtUnixMilliseconds <= 0)
                throw new InvalidOperationException("Snapshot timestamp is invalid.");
            if (money < 0 || colonyYear < 1 || !FiniteNonNegative(sessionSeconds))
                throw new InvalidOperationException("Snapshot economy or time values are invalid.");
            if (randomSeed == 0)
                throw new InvalidOperationException("Snapshot random seed cannot be zero.");
            if (creatures == null || eggs == null || inventory == null ||
                worldItems == null || facilities == null || timers == null)
                throw new InvalidOperationException("Snapshot collections are missing.");

            var entityIds = new HashSet<string>(StringComparer.Ordinal);
            var creatureIds = new HashSet<string>(StringComparer.Ordinal);
            var facilityIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (CreatureSnapshot creature in creatures)
            {
                RequireEntity(creature, creature?.id, entityIds, "creature");
                creatureIds.Add(creature.id);
                ValidatePosition(creature.position, "creature");
                if (!Enum.IsDefined(typeof(CreatureStage), creature.stage) ||
                    !Enum.IsDefined(typeof(CreatureRole), creature.role) ||
                    !FiniteNonNegative(creature.ageSeconds) ||
                    !FinitePositive(creature.lifespanSeconds) ||
                    !FiniteNonNegative(creature.elderStartAgeSeconds) ||
                    !FiniteNonNegative(creature.health) ||
                    !FiniteNonNegative(creature.hunger) ||
                    !FiniteNonNegative(creature.combatHealth) ||
                    !FiniteNonNegative(creature.layTimerSeconds) ||
                    !FiniteNonNegative(creature.productionEfficiency) ||
                    string.IsNullOrWhiteSpace(creature.code))
                    throw new InvalidOperationException("Creature snapshot values are invalid.");
            }

            foreach (EggSnapshot egg in eggs)
            {
                RequireEntity(egg, egg?.id, entityIds, "egg");
                ValidatePosition(egg.position, "egg");
                if (!FiniteNonNegative(egg.hatchRemainingSeconds) ||
                    !FinitePositive(egg.hatchDurationSeconds))
                    throw new InvalidOperationException("Egg snapshot timers are invalid.");
            }

            var inventoryIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (InventoryStackSnapshot stack in inventory)
            {
                if (stack == null || string.IsNullOrWhiteSpace(stack.id) ||
                    !inventoryIds.Add(stack.id) ||
                    !Enum.IsDefined(typeof(InventoryItemType), stack.itemType) ||
                    stack.count < 0)
                    throw new InvalidOperationException("Inventory snapshot is invalid.");
            }

            foreach (WorldItemSnapshot item in worldItems)
            {
                RequireEntity(item, item?.id, entityIds, "world item");
                ValidatePosition(item.position, "world item");
                if (!Enum.IsDefined(typeof(WorldItemKind), item.kind) ||
                    !Enum.IsDefined(typeof(InventoryItemType), item.itemType) ||
                    item.amount < 0)
                    throw new InvalidOperationException("World item snapshot is invalid.");
            }

            foreach (FacilitySnapshot facility in facilities)
            {
                RequireEntity(facility, facility?.id, entityIds, "facility");
                facilityIds.Add(facility.id);
                ValidatePosition(facility.position, "facility");
                if (!Enum.IsDefined(typeof(FacilityType), facility.facilityType) ||
                    facility.level < 0 || facility.level > 3 ||
                    !FiniteNonNegative(facility.structureHealth) ||
                    (!FiniteNonNegative(facility.academyTimerSeconds) &&
                     Math.Abs(facility.academyTimerSeconds + 1f) > .0001f) ||
                    facility.occupants == null)
                    throw new InvalidOperationException("Facility snapshot is invalid.");
            }

            foreach (CreatureSnapshot creature in creatures)
                if (!string.IsNullOrEmpty(creature.facilityId) &&
                    !facilityIds.Contains(creature.facilityId))
                    throw new InvalidOperationException("Creature references an unknown facility.");

            foreach (WorldItemSnapshot item in worldItems)
                if (!string.IsNullOrEmpty(item.facilityId) &&
                    !facilityIds.Contains(item.facilityId))
                    throw new InvalidOperationException("World item references an unknown facility.");

            foreach (FacilitySnapshot facility in facilities)
            {
                var occupants = new HashSet<string>(StringComparer.Ordinal);
                foreach (FacilityOccupantSnapshot occupant in facility.occupants)
                {
                    if (occupant == null || !creatureIds.Contains(occupant.creatureId) ||
                        !occupants.Add(occupant.creatureId) ||
                        !FiniteNonNegative(occupant.taskTimerSeconds))
                        throw new InvalidOperationException("Facility occupant snapshot is invalid.");
                }
            }

            var timerIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (PersistentTimerSnapshot timer in timers)
            {
                if (timer == null || string.IsNullOrWhiteSpace(timer.id) ||
                    !timerIds.Add(timer.id) ||
                    !FiniteNonNegative(timer.elapsedSeconds) ||
                    timer.counter < 0)
                    throw new InvalidOperationException("Persistent timer snapshot is invalid.");
            }
        }

        private static void RequireEntity(
            object entity,
            string id,
            HashSet<string> ids,
            string label)
        {
            if (entity == null)
                throw new InvalidOperationException($"Snapshot contains a null {label}.");
            RequireId(id, label + " ID");
            if (!ids.Add(id))
                throw new InvalidOperationException($"Duplicate persistent entity ID: {id}.");
        }

        private static void RequireId(string value, string label)
        {
            if (!Guid.TryParseExact(value, "N", out _))
                throw new InvalidOperationException($"{label} is missing or malformed.");
        }

        private static void ValidatePosition(WorldPosition position, string label)
        {
            if (!Finite(position.x) || !Finite(position.y))
                throw new InvalidOperationException($"{label} position is invalid.");
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool FiniteNonNegative(float value) => Finite(value) && value >= 0f;
        private static bool FiniteNonNegative(double value) => Finite(value) && value >= 0d;
        private static bool FinitePositive(float value) => Finite(value) && value > 0f;
    }
}
