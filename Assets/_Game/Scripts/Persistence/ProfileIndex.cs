using System;
using System.Collections.Generic;
using UnityEngine;

namespace TCC.Persistence
{
    [Serializable]
    public sealed class ProfileIndexEntry
    {
        [SerializeField] private string _profileId;
        [SerializeField] private string _playerName;
        [SerializeField] private double _totalPlaySeconds;
        [SerializeField] private ProfileEra _currentEra;
        [SerializeField] private long _lastPlayedAtUnixMilliseconds;

        public string ProfileId => _profileId;
        public string PlayerName => _playerName;
        public double TotalPlaySeconds => _totalPlaySeconds;
        public ProfileEra CurrentEra => _currentEra;
        public long LastPlayedAtUnixMilliseconds => _lastPlayedAtUnixMilliseconds;

        internal static ProfileIndexEntry FromProfile(PlayerProfile profile)
        {
            return new ProfileIndexEntry
            {
                _profileId = profile.ProfileId,
                _playerName = profile.PlayerName,
                _totalPlaySeconds = profile.TotalPlaySeconds,
                _currentEra = profile.CurrentEra,
                _lastPlayedAtUnixMilliseconds = profile.LastPlayedAtUnixMilliseconds
            };
        }

        internal void EnsureValid()
        {
            if (!Guid.TryParseExact(_profileId, "N", out _))
                throw new InvalidOperationException("Indexed profile ID is missing or malformed.");
            if (string.IsNullOrWhiteSpace(_playerName))
                throw new InvalidOperationException("Indexed player name is blank.");
            if (double.IsNaN(_totalPlaySeconds) ||
                double.IsInfinity(_totalPlaySeconds) ||
                _totalPlaySeconds < 0d)
                throw new InvalidOperationException("Indexed play time is invalid.");
            if (!Enum.IsDefined(typeof(ProfileEra), _currentEra))
                throw new InvalidOperationException("Indexed era is invalid.");
            if (_lastPlayedAtUnixMilliseconds <= 0)
                throw new InvalidOperationException("Indexed last-played timestamp is invalid.");
        }
    }

    [Serializable]
    public sealed class ProfileIndex
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private int _schemaVersion;
        [SerializeField] private string _selectedProfileId;
        [SerializeField] private List<ProfileIndexEntry> _profiles;

        public int SchemaVersion => _schemaVersion;
        public string SelectedProfileId => _selectedProfileId;
        public IReadOnlyList<ProfileIndexEntry> Profiles => _profiles;

        private ProfileIndex()
        {
        }

        internal static ProfileIndex CreateEmpty()
        {
            return new ProfileIndex
            {
                _schemaVersion = CurrentSchemaVersion,
                _selectedProfileId = string.Empty,
                _profiles = new List<ProfileIndexEntry>()
            };
        }

        internal void Upsert(PlayerProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            profile.EnsureValid();

            int existingIndex = _profiles.FindIndex(entry => entry.ProfileId == profile.ProfileId);
            ProfileIndexEntry replacement = ProfileIndexEntry.FromProfile(profile);
            if (existingIndex >= 0)
                _profiles[existingIndex] = replacement;
            else
                _profiles.Add(replacement);

            _profiles.Sort((left, right) =>
                right.LastPlayedAtUnixMilliseconds.CompareTo(left.LastPlayedAtUnixMilliseconds));

            if (string.IsNullOrEmpty(_selectedProfileId))
                _selectedProfileId = profile.ProfileId;
        }

        public void Select(string profileId)
        {
            if (_profiles == null || !_profiles.Exists(entry => entry.ProfileId == profileId))
                throw new InvalidOperationException("Cannot select a profile that is not indexed.");

            _selectedProfileId = profileId;
        }

        internal void EnsureValid()
        {
            if (_schemaVersion != CurrentSchemaVersion)
                throw new InvalidOperationException($"Unsupported profile index schema: {_schemaVersion}.");
            if (_profiles == null)
                throw new InvalidOperationException("Profile index entries are missing.");

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            long previousTimestamp = long.MaxValue;
            foreach (ProfileIndexEntry entry in _profiles)
            {
                if (entry == null)
                    throw new InvalidOperationException("Profile index contains a null entry.");

                entry.EnsureValid();
                if (!seenIds.Add(entry.ProfileId))
                    throw new InvalidOperationException("Profile index contains a duplicate ID.");
                if (entry.LastPlayedAtUnixMilliseconds > previousTimestamp)
                    throw new InvalidOperationException("Profile index is not sorted by recent play time.");

                previousTimestamp = entry.LastPlayedAtUnixMilliseconds;
            }

            if (!string.IsNullOrEmpty(_selectedProfileId) && !seenIds.Contains(_selectedProfileId))
                throw new InvalidOperationException("Selected profile is not present in the index.");
        }
    }
}
