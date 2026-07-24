using System;
using TCC.Core;
using TCC.Managers;

namespace TCC.Persistence
{
    public static class WorldStateService
    {
        public static WorldSnapshot Capture(string profileId, long capturedAtUnixMilliseconds)
        {
            if (!GameManager.Exists || !EconomyManager.Exists ||
                !InventoryManager.Exists || !SimulationManager.Exists)
                throw new InvalidOperationException("Required runtime managers are not available.");

            WorldSnapshot snapshot = WorldSnapshot.Create(profileId, capturedAtUnixMilliseconds);
            snapshot.money = EconomyManager.Instance.Money;
            snapshot.sessionSeconds = GameManager.Instance.SessionSeconds;
            snapshot.colonyYear = GameManager.Instance.ColonyYear;
            InventoryManager.Instance.CaptureStacks(snapshot.inventory);
            SimulationManager.Instance.CaptureSnapshot(snapshot);
            snapshot.EnsureValid();
            return snapshot;
        }

        public static void Restore(WorldSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            snapshot.EnsureValid();
            if (!GameManager.Exists || !EconomyManager.Exists ||
                !InventoryManager.Exists || !SimulationManager.Exists)
                throw new InvalidOperationException("Required runtime managers are not available.");

            SimulationManager.Instance.RestoreSnapshot(snapshot);
            InventoryManager.Instance.RestoreStacks(snapshot.inventory);
            EconomyManager.Instance.RestoreMoney(snapshot.money);
            GameManager.Instance.RestoreSessionSeconds(snapshot.sessionSeconds);
        }
    }
}
