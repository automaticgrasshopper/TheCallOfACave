namespace TCC.Persistence
{
    public sealed class WorldSnapshotLoadResult
    {
        public WorldSnapshot Snapshot { get; }
        public bool UsedBackup { get; }
        public string PrimaryError { get; }

        internal WorldSnapshotLoadResult(
            WorldSnapshot snapshot,
            bool usedBackup,
            string primaryError)
        {
            Snapshot = snapshot;
            UsedBackup = usedBackup;
            PrimaryError = primaryError;
        }
    }
}
