using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TCC.Persistence;

namespace TCC.EditorTools
{
    public static class ProfileRepositoryValidation
    {
        private const long CreatedAt = 1784908800000L;

        [MenuItem("TCC/Validation/Run Profile Repository Tests")]
        public static void RunFromMenu()
        {
            RunAssertions();
            Debug.Log("[TCC Repository] PASS: atomic save, index, backup and corruption fallback.");
        }

        public static void RunBatch()
        {
            try
            {
                RunAssertions();
                Debug.Log("[TCC Repository] PASS: atomic save, index, backup and corruption fallback.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[TCC Repository] FAIL: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(2);
                throw;
            }
        }

        private static void RunAssertions()
        {
            string testRoot = Path.Combine(
                Path.GetTempPath(),
                "tcc-profile-repository-" + Guid.NewGuid().ToString("N"));

            try
            {
                var repository = new ProfileRepository(testRoot);
                PlayerProfile profile = PlayerProfile.Create("仓库测试者", "备份接收者", CreatedAt);

                ProfileIndex emptyIndex = repository.LoadIndex();
                Equal(0, emptyIndex.Profiles.Count, "empty index count");

                repository.SaveProfile(profile);
                ProfileIndex firstIndex = repository.LoadIndex();
                Equal(1, firstIndex.Profiles.Count, "first index count");
                Equal(profile.ProfileId, firstIndex.SelectedProfileId, "selected profile");

                profile.SetTutorialProgress(TutorialProgress.Completed, CreatedAt + 1000L);
                profile.AdvanceToEra(ProfileEra.Second, CreatedAt + 2000L);
                profile.RecordPlaySession(125.5d, CreatedAt + 3000L);
                repository.SaveProfile(profile);

                ProfileLoadResult current = repository.LoadProfile(profile.ProfileId);
                Equal(false, current.UsedBackup, "primary load source");
                Equal(125.5d, current.Profile.TotalPlaySeconds, "current play time");
                Equal(ProfileEra.Second, current.Profile.CurrentEra, "current era");
                Equal(true, File.Exists(repository.GetProfileBackupPath(profile.ProfileId)), "profile backup");
                Equal(true, File.Exists(repository.IndexBackupPath), "index backup");

                string corruptProfileJson = "{ this is intentionally corrupt";
                File.WriteAllText(repository.GetProfilePath(profile.ProfileId), corruptProfileJson);
                ProfileLoadResult recovered = repository.LoadProfile(profile.ProfileId);
                Equal(true, recovered.UsedBackup, "fallback load source");
                Equal(0d, recovered.Profile.TotalPlaySeconds, "backup play time");
                Equal(ProfileEra.First, recovered.Profile.CurrentEra, "backup era");
                Equal(
                    corruptProfileJson,
                    File.ReadAllText(repository.GetProfilePath(profile.ProfileId)),
                    "corrupt primary preservation");

                string corruptIndexJson = "{ broken index";
                File.WriteAllText(repository.IndexPath, corruptIndexJson);
                ProfileIndex recoveredIndex = repository.LoadIndex();
                Equal(1, recoveredIndex.Profiles.Count, "backup index count");
                Equal(profile.ProfileId, recoveredIndex.Profiles[0].ProfileId, "backup index profile");
                Equal(
                    corruptIndexJson,
                    File.ReadAllText(repository.IndexPath),
                    "corrupt index preservation");

                int temporaryFiles = Directory
                    .EnumerateFiles(testRoot, "*.tmp.*", SearchOption.AllDirectories)
                    .Count();
                Equal(0, temporaryFiles, "temporary file cleanup");

                string expectedDefault = Path.Combine(Application.persistentDataPath, "profiles");
                Equal(expectedDefault, ProfileRepository.DefaultRootDirectory, "persistent data root");
            }
            finally
            {
                if (Directory.Exists(testRoot))
                    Directory.Delete(testRoot, true);
            }
        }

        private static void Equal<T>(T expected, T actual, string field)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    $"{field} mismatch: expected '{expected}', got '{actual}'.");
        }
    }
}
