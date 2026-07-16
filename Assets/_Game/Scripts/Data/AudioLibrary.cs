using System;
using UnityEngine;

namespace TCC.Data
{
    /// <summary>
    /// Named SFX + a music track. AudioManager looks clips up by id so gameplay
    /// only ever references a string constant, never an AudioClip directly.
    /// Clips are left empty for now — drop files in and the pipeline is live.
    /// </summary>
    [CreateAssetMenu(menuName = "TCC/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [Header("Music")]
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.6f;

        [Header("SFX (looked up by id)")]
        public Entry[] sfx = new[]
        {
            new Entry { id = Ids.Feed,        volume = 0.8f },
            new Entry { id = Ids.EggLay,      volume = 0.8f },
            new Entry { id = Ids.EggCollect,  volume = 0.9f },
            new Entry { id = Ids.Birth,       volume = 0.7f },
            new Entry { id = Ids.Death,       volume = 0.7f },
            new Entry { id = Ids.Click,       volume = 0.8f },
        };

        /// <summary>Canonical SFX ids so callers don't pass raw strings.</summary>
        public static class Ids
        {
            public const string Feed = "feed";
            public const string EggLay = "egg_lay";
            public const string EggCollect = "egg_collect";
            public const string Birth = "birth";
            public const string Death = "death";
            public const string Click = "click";
        }
    }
}
