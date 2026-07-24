using System;
using UnityEngine;

namespace TCC.Persistence
{
    /// <summary>
    /// Versioned, repository-independent identity and progression data for one profile.
    /// World state is intentionally excluded until the Day 4 snapshot contract.
    /// </summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private int _schemaVersion;
        [SerializeField] private string _profileId;
        [SerializeField] private string _playerName;
        [SerializeField] private string _researchRecipientName;
        [SerializeField] private double _totalPlaySeconds;
        [SerializeField] private ProfileEra _currentEra;
        [SerializeField] private TutorialProgress _tutorialProgress;
        [SerializeField] private long _createdAtUnixMilliseconds;
        [SerializeField] private long _updatedAtUnixMilliseconds;
        [SerializeField] private long _lastPlayedAtUnixMilliseconds;

        public int SchemaVersion => _schemaVersion;
        public string ProfileId => _profileId;
        public string PlayerName => _playerName;
        public string ResearchRecipientName => _researchRecipientName;
        public double TotalPlaySeconds => _totalPlaySeconds;
        public ProfileEra CurrentEra => _currentEra;
        public TutorialProgress TutorialProgress => _tutorialProgress;
        public long CreatedAtUnixMilliseconds => _createdAtUnixMilliseconds;
        public long UpdatedAtUnixMilliseconds => _updatedAtUnixMilliseconds;
        public long LastPlayedAtUnixMilliseconds => _lastPlayedAtUnixMilliseconds;
        public bool HasCompletedTutorial => _tutorialProgress == TutorialProgress.Completed;

        private PlayerProfile()
        {
        }

        public static PlayerProfile Create(
            string playerName,
            string researchRecipientName,
            long createdAtUnixMilliseconds)
        {
            RequireName(playerName, nameof(playerName));
            RequireName(researchRecipientName, nameof(researchRecipientName));
            RequireTimestamp(createdAtUnixMilliseconds, nameof(createdAtUnixMilliseconds));

            return new PlayerProfile
            {
                _schemaVersion = CurrentSchemaVersion,
                _profileId = Guid.NewGuid().ToString("N"),
                _playerName = playerName,
                _researchRecipientName = researchRecipientName,
                _totalPlaySeconds = 0d,
                _currentEra = ProfileEra.First,
                _tutorialProgress = TutorialProgress.NotStarted,
                _createdAtUnixMilliseconds = createdAtUnixMilliseconds,
                _updatedAtUnixMilliseconds = createdAtUnixMilliseconds,
                _lastPlayedAtUnixMilliseconds = createdAtUnixMilliseconds
            };
        }

        public void RecordPlaySession(double elapsedSeconds, long playedAtUnixMilliseconds)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));

            _totalPlaySeconds += elapsedSeconds;
            Touch(playedAtUnixMilliseconds);
        }

        public void AdvanceToEra(ProfileEra era, long changedAtUnixMilliseconds)
        {
            if (!Enum.IsDefined(typeof(ProfileEra), era))
                throw new ArgumentOutOfRangeException(nameof(era));
            if (era < _currentEra)
                throw new InvalidOperationException("A profile cannot move back to an earlier era.");

            _currentEra = era;
            Touch(changedAtUnixMilliseconds);
        }

        public void SetTutorialProgress(TutorialProgress progress, long changedAtUnixMilliseconds)
        {
            if (!Enum.IsDefined(typeof(TutorialProgress), progress))
                throw new ArgumentOutOfRangeException(nameof(progress));
            if (progress < _tutorialProgress)
                throw new InvalidOperationException("Tutorial progress cannot move backwards.");

            _tutorialProgress = progress;
            Touch(changedAtUnixMilliseconds);
        }

        internal void EnsureValid()
        {
            if (_schemaVersion != CurrentSchemaVersion)
                throw new InvalidOperationException($"Unsupported profile schema version: {_schemaVersion}.");
            if (!Guid.TryParseExact(_profileId, "N", out _))
                throw new InvalidOperationException("Profile ID is missing or malformed.");

            RequireName(_playerName, nameof(PlayerName));
            RequireName(_researchRecipientName, nameof(ResearchRecipientName));

            if (double.IsNaN(_totalPlaySeconds) ||
                double.IsInfinity(_totalPlaySeconds) ||
                _totalPlaySeconds < 0d)
                throw new InvalidOperationException("Total play time is invalid.");
            if (!Enum.IsDefined(typeof(ProfileEra), _currentEra))
                throw new InvalidOperationException("Profile era is invalid.");
            if (!Enum.IsDefined(typeof(TutorialProgress), _tutorialProgress))
                throw new InvalidOperationException("Tutorial progress is invalid.");

            RequireTimestamp(_createdAtUnixMilliseconds, nameof(CreatedAtUnixMilliseconds));
            RequireTimestamp(_updatedAtUnixMilliseconds, nameof(UpdatedAtUnixMilliseconds));
            RequireTimestamp(_lastPlayedAtUnixMilliseconds, nameof(LastPlayedAtUnixMilliseconds));
            if (_updatedAtUnixMilliseconds < _createdAtUnixMilliseconds ||
                _lastPlayedAtUnixMilliseconds < _createdAtUnixMilliseconds)
                throw new InvalidOperationException("Profile timestamps are out of order.");
        }

        private void Touch(long timestamp)
        {
            RequireTimestamp(timestamp, nameof(timestamp));
            if (timestamp < _createdAtUnixMilliseconds ||
                timestamp < _updatedAtUnixMilliseconds ||
                timestamp < _lastPlayedAtUnixMilliseconds)
                throw new ArgumentOutOfRangeException(nameof(timestamp), "Profile timestamps must be monotonic.");

            _updatedAtUnixMilliseconds = timestamp;
            _lastPlayedAtUnixMilliseconds = timestamp;
        }

        private static void RequireName(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Profile names cannot be blank.", parameterName);
        }

        private static void RequireTimestamp(long value, string parameterName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(parameterName, "Timestamp must be a positive Unix time.");
        }
    }
}
