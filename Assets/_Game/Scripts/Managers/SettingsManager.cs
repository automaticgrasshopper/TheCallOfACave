using UnityEngine;
using TCC.Core;

namespace TCC.Managers
{
    /// <summary>
    /// Owns screen/display settings (window mode + resolution): loads them on boot,
    /// applies them, and persists changes made from the Settings UI. Audio volume
    /// lives in <see cref="AudioManager"/> and language in
    /// <see cref="LocalizationManager"/>; this keeps each concern independent.
    /// </summary>
    public class SettingsManager : Singleton<SettingsManager>
    {
        private const string PrefFullscreen = "tcc.screen.fullscreen";
        private const string PrefResolution = "tcc.screen.resindex";

        /// <summary>Two common 16:9 presets, referenced by index from the dropdown.</summary>
        public static readonly Vector2Int[] Resolutions =
        {
            new Vector2Int(1920, 1080),
            new Vector2Int(1280, 720),
        };

        public bool Fullscreen { get; private set; } = true;
        public int ResolutionIndex { get; private set; } = 0;

        protected override void OnAwake()
        {
            Fullscreen = PlayerPrefs.GetInt(PrefFullscreen, 1) == 1;
            ResolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefResolution, 0), 0, Resolutions.Length - 1);
            ApplyScreen();
        }

        public void SetFullscreen(bool value)
        {
            Fullscreen = value;
            PlayerPrefs.SetInt(PrefFullscreen, value ? 1 : 0);
            ApplyScreen();
        }

        public void SetResolutionIndex(int index)
        {
            ResolutionIndex = Mathf.Clamp(index, 0, Resolutions.Length - 1);
            PlayerPrefs.SetInt(PrefResolution, ResolutionIndex);
            ApplyScreen();
        }

        private void ApplyScreen()
        {
            var r = Resolutions[ResolutionIndex];
            Screen.SetResolution(r.x, r.y,
                Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        public static string[] ResolutionLabels()
        {
            var labels = new string[Resolutions.Length];
            for (int i = 0; i < Resolutions.Length; i++)
                labels[i] = $"{Resolutions[i].x} × {Resolutions[i].y}";
            return labels;
        }
    }
}
