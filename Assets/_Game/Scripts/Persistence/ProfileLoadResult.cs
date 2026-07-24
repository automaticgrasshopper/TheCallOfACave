namespace TCC.Persistence
{
    public sealed class ProfileLoadResult
    {
        public PlayerProfile Profile { get; }
        public bool UsedBackup { get; }
        public string PrimaryError { get; }

        internal ProfileLoadResult(PlayerProfile profile, bool usedBackup, string primaryError)
        {
            Profile = profile;
            UsedBackup = usedBackup;
            PrimaryError = primaryError;
        }
    }
}
