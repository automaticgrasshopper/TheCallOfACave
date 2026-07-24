using System;
using UnityEngine;

namespace TCC.Persistence
{
    /// <summary>
    /// Canonical JSON boundary for the current profile schema.
    /// File-system persistence belongs to the Day 3 repository.
    /// </summary>
    public static class ProfileJson
    {
        public static string Serialize(PlayerProfile profile, bool prettyPrint = false)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            profile.EnsureValid();
            return JsonUtility.ToJson(profile, prettyPrint);
        }

        public static PlayerProfile Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Profile JSON cannot be blank.", nameof(json));

            PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(json);
            if (profile == null)
                throw new InvalidOperationException("Profile JSON produced no model.");

            profile.EnsureValid();
            return profile;
        }
    }
}
