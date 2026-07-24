using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TCC.Persistence;

namespace TCC.EditorTools
{
    public static class ProfileMigrationValidation
    {
        private sealed class V1ToV2TestStep : IJsonMigrationStep
        {
            public int FromVersion => 1;
            public int ToVersion => 2;

            public string Migrate(string json)
            {
                return json.Replace("\"schemaVersion\":1", "\"schemaVersion\":2");
            }
        }

        [MenuItem("TCC/Validation/Run Profile Migration Tests")]
        public static void RunFromMenu()
        {
            RunAssertions();
            Debug.Log("[TCC Migration] PASS: empty, current, legacy and corrupt paths.");
        }

        public static void RunBatch()
        {
            try
            {
                RunAssertions();
                Debug.Log("[TCC Migration] PASS: empty, current, legacy and corrupt paths.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[TCC Migration] FAIL: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(2);
                throw;
            }
        }

        private static void RunAssertions()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "tcc-profile-migration-" + Guid.NewGuid().ToString("N"));
            try
            {
                ValidateEmpty(Path.Combine(root, "empty"));
                ValidateCurrent(Path.Combine(root, "current"));
                ValidateLegacy(Path.Combine(root, "legacy"));
                ValidateCorruptRecovery(Path.Combine(root, "corrupt"));
                ValidateFutureMigrationEntry();
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        private static void ValidateEmpty(string path)
        {
            var service = new ProfileOpenService(new ProfileRepository(path));
            Equal(ProfileOpenStatus.Empty, service.OpenMostRecent().Status, "empty profile");
        }

        private static void ValidateCurrent(string path)
        {
            var repository = new ProfileRepository(path);
            PlayerProfile profile = CreateProfile();
            repository.SaveProfile(profile);
            ProfileOpenResult result = new ProfileOpenService(repository).OpenMostRecent();
            Equal(ProfileOpenStatus.Loaded, result.Status, "current profile");
            Equal(profile.ProfileId, result.Profile.ProfileId, "current profile ID");
        }

        private static void ValidateLegacy(string path)
        {
            var repository = new ProfileRepository(path);
            PlayerProfile profile = CreateProfile();
            repository.SaveProfile(profile);
            string profilePath = repository.GetProfilePath(profile.ProfileId);
            string legacyJson = File.ReadAllText(profilePath)
                .Replace("\"_schemaVersion\": 1", "\"_schemaVersion\": 0");
            File.WriteAllText(profilePath, legacyJson);

            ProfileOpenResult result = new ProfileOpenService(repository).OpenMostRecent();
            Equal(ProfileOpenStatus.MigrationRequired, result.Status, "legacy profile");
            Equal(0, result.SourceVersion, "legacy source version");
        }

        private static void ValidateCorruptRecovery(string path)
        {
            var repository = new ProfileRepository(path);
            PlayerProfile profile = CreateProfile();
            repository.SaveProfile(profile);
            profile.RecordPlaySession(90d, profile.UpdatedAtUnixMilliseconds + 1000L);
            repository.SaveProfile(profile);
            File.WriteAllText(repository.GetProfilePath(profile.ProfileId), "{ broken profile");

            ProfileOpenResult result = new ProfileOpenService(repository).OpenMostRecent();
            Equal(
                ProfileOpenStatus.RecoveredFromBackup,
                result.Status,
                "corrupt profile");
            Equal(0d, result.Profile.TotalPlaySeconds, "recovered backup value");
        }

        private static void ValidateFutureMigrationEntry()
        {
            var pipeline = new JsonMigrationPipeline(2, new V1ToV2TestStep());
            JsonMigrationResult result =
                pipeline.MigrateToCurrent("{\"schemaVersion\":1,\"payload\":\"kept\"}");
            Equal(JsonMigrationStatus.Migrated, result.Status, "v1 to v2 status");
            Equal(1, result.SourceVersion, "v1 source");
            Equal(2, result.TargetVersion, "v2 target");
            if (!result.Json.Contains("\"schemaVersion\":2") ||
                !result.Json.Contains("\"payload\":\"kept\""))
                throw new InvalidOperationException("Migration did not preserve the payload.");
        }

        private static PlayerProfile CreateProfile()
        {
            return PlayerProfile.Create(
                "迁移测试者",
                "未来接收者",
                1785081600000L);
        }

        private static void Equal<T>(T expected, T actual, string field)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    $"{field} mismatch: expected '{expected}', got '{actual}'.");
        }
    }
}
