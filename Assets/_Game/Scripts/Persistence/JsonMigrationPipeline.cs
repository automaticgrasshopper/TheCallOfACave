using System;
using System.Collections.Generic;
using UnityEngine;

namespace TCC.Persistence
{
    public interface IJsonMigrationStep
    {
        int FromVersion { get; }
        int ToVersion { get; }
        string Migrate(string json);
    }

    public enum JsonMigrationStatus
    {
        Empty,
        Current,
        Migrated,
        MigrationRequired,
        UnsupportedFutureVersion,
        Corrupt
    }

    public sealed class JsonMigrationResult
    {
        public JsonMigrationStatus Status { get; }
        public int SourceVersion { get; }
        public int TargetVersion { get; }
        public string Json { get; }
        public string Error { get; }

        internal JsonMigrationResult(
            JsonMigrationStatus status,
            int sourceVersion,
            int targetVersion,
            string json,
            string error)
        {
            Status = status;
            SourceVersion = sourceVersion;
            TargetVersion = targetVersion;
            Json = json;
            Error = error;
        }
    }

    public sealed class JsonMigrationPipeline
    {
        [Serializable]
        private sealed class VersionHeader
        {
            public int schemaVersion;
            public int _schemaVersion;
        }

        private readonly int _currentVersion;
        private readonly Dictionary<int, IJsonMigrationStep> _steps =
            new Dictionary<int, IJsonMigrationStep>();

        public JsonMigrationPipeline(
            int currentVersion,
            params IJsonMigrationStep[] steps)
        {
            if (currentVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(currentVersion));

            _currentVersion = currentVersion;
            if (steps == null) return;
            foreach (IJsonMigrationStep step in steps)
            {
                if (step == null || step.FromVersion <= 0 ||
                    step.ToVersion != step.FromVersion + 1 ||
                    _steps.ContainsKey(step.FromVersion))
                    throw new ArgumentException("Migration steps must be unique, consecutive upgrades.");
                _steps.Add(step.FromVersion, step);
            }
        }

        public JsonMigrationResult MigrateToCurrent(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Result(JsonMigrationStatus.Empty, 0, null, null);

            int sourceVersion;
            try
            {
                var header = JsonUtility.FromJson<VersionHeader>(json);
                if (header == null)
                    throw new InvalidOperationException("Version header is missing.");
                sourceVersion = Math.Max(header.schemaVersion, header._schemaVersion);
                if (sourceVersion <= 0)
                    return Result(
                        JsonMigrationStatus.MigrationRequired,
                        sourceVersion,
                        json,
                        "Document predates the supported v1 schema.");
            }
            catch (Exception exception)
            {
                return Result(JsonMigrationStatus.Corrupt, 0, json, exception.Message);
            }

            if (sourceVersion == _currentVersion)
                return Result(JsonMigrationStatus.Current, sourceVersion, json, null);
            if (sourceVersion > _currentVersion)
                return Result(
                    JsonMigrationStatus.UnsupportedFutureVersion,
                    sourceVersion,
                    json,
                    "Document was created by a newer game version.");

            string migratedJson = json;
            int version = sourceVersion;
            while (version < _currentVersion)
            {
                if (!_steps.TryGetValue(version, out IJsonMigrationStep step))
                    return Result(
                        JsonMigrationStatus.MigrationRequired,
                        sourceVersion,
                        migratedJson,
                        $"No migration step is registered for v{version} to v{version + 1}.");
                try
                {
                    migratedJson = step.Migrate(migratedJson);
                    if (string.IsNullOrWhiteSpace(migratedJson))
                        throw new InvalidOperationException("Migration produced blank JSON.");
                    version = step.ToVersion;
                }
                catch (Exception exception)
                {
                    return Result(
                        JsonMigrationStatus.Corrupt,
                        sourceVersion,
                        migratedJson,
                        exception.Message);
                }
            }

            return Result(JsonMigrationStatus.Migrated, sourceVersion, migratedJson, null);
        }

        private JsonMigrationResult Result(
            JsonMigrationStatus status,
            int sourceVersion,
            string json,
            string error)
        {
            return new JsonMigrationResult(
                status,
                sourceVersion,
                _currentVersion,
                json,
                error);
        }
    }
}
