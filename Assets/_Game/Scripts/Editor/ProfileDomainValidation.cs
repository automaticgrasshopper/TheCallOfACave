using System;
using UnityEditor;
using UnityEngine;
using TCC.Persistence;

namespace TCC.EditorTools
{
    public static class ProfileDomainValidation
    {
        private const long CreatedAt = 1784822400000L;
        private const long TutorialAt = CreatedAt + 1000L;
        private const long EraAt = CreatedAt + 2000L;
        private const long PlayedAt = CreatedAt + 3000L;

        [MenuItem("TCC/Validation/Run Profile Domain Tests")]
        public static void RunFromMenu()
        {
            RunAssertions();
            Debug.Log("[TCC Profile] PASS: profile domain serialization round trip.");
        }

        public static void RunBatch()
        {
            try
            {
                RunAssertions();
                Debug.Log("[TCC Profile] PASS: profile domain serialization round trip.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[TCC Profile] FAIL: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(2);
                throw;
            }
        }

        private static void RunAssertions()
        {
            PlayerProfile original = PlayerProfile.Create("林博士 🧪", "Álvaro 🌱", CreatedAt);
            string stableId = original.ProfileId;

            original.SetTutorialProgress(TutorialProgress.Completed, TutorialAt);
            original.AdvanceToEra(ProfileEra.Third, EraAt);
            original.RecordPlaySession(7325.75d, PlayedAt);

            string json = ProfileJson.Serialize(original, true);
            PlayerProfile restored = ProfileJson.Deserialize(json);

            Equal(PlayerProfile.CurrentSchemaVersion, restored.SchemaVersion, "schemaVersion");
            Equal(stableId, restored.ProfileId, "profileId");
            Equal("林博士 🧪", restored.PlayerName, "playerName");
            Equal("Álvaro 🌱", restored.ResearchRecipientName, "researchRecipientName");
            Equal(7325.75d, restored.TotalPlaySeconds, "totalPlaySeconds");
            Equal(ProfileEra.Third, restored.CurrentEra, "currentEra");
            Equal(TutorialProgress.Completed, restored.TutorialProgress, "tutorialProgress");
            Equal(true, restored.HasCompletedTutorial, "hasCompletedTutorial");
            Equal(CreatedAt, restored.CreatedAtUnixMilliseconds, "createdAt");
            Equal(PlayedAt, restored.UpdatedAtUnixMilliseconds, "updatedAt");
            Equal(PlayedAt, restored.LastPlayedAtUnixMilliseconds, "lastPlayedAt");

            ExpectFailure(
                () => restored.AdvanceToEra(ProfileEra.Second, PlayedAt + 1L),
                "era regression");
            ExpectFailure(
                () => restored.SetTutorialProgress(TutorialProgress.NotStarted, PlayedAt + 1L),
                "tutorial regression");
            ExpectFailure(
                () => ProfileJson.Deserialize("{}"),
                "invalid serialized profile");
        }

        private static void Equal<T>(T expected, T actual, string field)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    $"{field} changed during round trip: expected '{expected}', got '{actual}'.");
        }

        private static void ExpectFailure(Action action, string scenario)
        {
            try
            {
                action();
            }
            catch
            {
                return;
            }

            throw new InvalidOperationException($"Expected validation failure for {scenario}.");
        }
    }
}
