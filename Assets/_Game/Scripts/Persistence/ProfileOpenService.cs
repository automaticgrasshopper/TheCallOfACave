using System;
using System.IO;
using System.Text;

namespace TCC.Persistence
{
    public enum ProfileOpenStatus
    {
        Empty,
        Loaded,
        RecoveredFromBackup,
        MigrationRequired,
        UnsupportedFutureVersion,
        Corrupt
    }

    public sealed class ProfileOpenResult
    {
        public ProfileOpenStatus Status { get; }
        public PlayerProfile Profile { get; }
        public int SourceVersion { get; }
        public string Message { get; }

        internal ProfileOpenResult(
            ProfileOpenStatus status,
            PlayerProfile profile,
            int sourceVersion,
            string message)
        {
            Status = status;
            Profile = profile;
            SourceVersion = sourceVersion;
            Message = message;
        }
    }

    /// <summary>
    /// Converts repository exceptions and version mismatches into explicit menu-facing results.
    /// </summary>
    public sealed class ProfileOpenService
    {
        private readonly ProfileRepository _repository;
        private readonly JsonMigrationPipeline _migration;

        public ProfileOpenService(ProfileRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _migration = new JsonMigrationPipeline(PlayerProfile.CurrentSchemaVersion);
        }

        public ProfileOpenResult OpenMostRecent()
        {
            ProfileIndex index;
            try
            {
                index = _repository.LoadIndex();
            }
            catch (Exception exception)
            {
                return Result(ProfileOpenStatus.Corrupt, null, 0, exception.Message);
            }

            if (index.Profiles.Count == 0)
                return Result(ProfileOpenStatus.Empty, null, 0, "No profiles exist.");

            string profileId = index.Profiles[0].ProfileId;
            JsonMigrationResult primary = Inspect(_repository.GetProfilePath(profileId));
            if (primary.Status == JsonMigrationStatus.Current)
            {
                try
                {
                    return Result(
                        ProfileOpenStatus.Loaded,
                        ProfileJson.Deserialize(primary.Json),
                        primary.SourceVersion,
                        null);
                }
                catch (Exception exception)
                {
                    primary = Corrupt(primary, exception.Message);
                }
            }

            if (primary.Status == JsonMigrationStatus.MigrationRequired)
                return Result(
                    ProfileOpenStatus.MigrationRequired,
                    null,
                    primary.SourceVersion,
                    primary.Error);
            if (primary.Status == JsonMigrationStatus.UnsupportedFutureVersion)
                return Result(
                    ProfileOpenStatus.UnsupportedFutureVersion,
                    null,
                    primary.SourceVersion,
                    primary.Error);

            JsonMigrationResult backup = Inspect(_repository.GetProfileBackupPath(profileId));
            if (backup.Status == JsonMigrationStatus.Current)
            {
                try
                {
                    return Result(
                        ProfileOpenStatus.RecoveredFromBackup,
                        ProfileJson.Deserialize(backup.Json),
                        backup.SourceVersion,
                        primary.Error);
                }
                catch (Exception exception)
                {
                    backup = Corrupt(backup, exception.Message);
                }
            }

            return Result(
                ProfileOpenStatus.Corrupt,
                null,
                Math.Max(primary.SourceVersion, backup.SourceVersion),
                $"Primary: {primary.Error ?? primary.Status.ToString()}; " +
                $"backup: {backup.Error ?? backup.Status.ToString()}.");
        }

        private JsonMigrationResult Inspect(string path)
        {
            if (!File.Exists(path))
                return _migration.MigrateToCurrent(null);
            try
            {
                return _migration.MigrateToCurrent(File.ReadAllText(path, Encoding.UTF8));
            }
            catch (Exception exception)
            {
                return Corrupt(null, exception.Message);
            }
        }

        private static JsonMigrationResult Corrupt(
            JsonMigrationResult previous,
            string error)
        {
            return new JsonMigrationResult(
                JsonMigrationStatus.Corrupt,
                previous?.SourceVersion ?? 0,
                previous?.TargetVersion ?? PlayerProfile.CurrentSchemaVersion,
                previous?.Json,
                error);
        }

        private static ProfileOpenResult Result(
            ProfileOpenStatus status,
            PlayerProfile profile,
            int sourceVersion,
            string message)
        {
            return new ProfileOpenResult(status, profile, sourceVersion, message);
        }
    }
}
