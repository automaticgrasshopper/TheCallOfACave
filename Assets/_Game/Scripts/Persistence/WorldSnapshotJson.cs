using System;
using UnityEngine;

namespace TCC.Persistence
{
    public static class WorldSnapshotJson
    {
        public static string Serialize(WorldSnapshot snapshot, bool prettyPrint = false)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            snapshot.EnsureValid();
            return JsonUtility.ToJson(snapshot, prettyPrint);
        }

        public static WorldSnapshot Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("World snapshot JSON cannot be blank.", nameof(json));

            WorldSnapshot snapshot = JsonUtility.FromJson<WorldSnapshot>(json);
            if (snapshot == null)
                throw new InvalidOperationException("World snapshot JSON produced no model.");

            snapshot.EnsureValid();
            return snapshot;
        }
    }
}
