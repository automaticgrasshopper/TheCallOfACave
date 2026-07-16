using System;
using System.Collections.Generic;
using UnityEngine;
using TCC.Core;

namespace TCC.Data
{
    /// <summary>
    /// The single source of truth for on-screen text. Every localizable string is
    /// a key with one value per language. Built as an asset so translators (and
    /// the user) edit values without touching code, and so new languages are just
    /// a new column on <see cref="Entry"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "TCC/Localization Table", fileName = "LocalizationTable")]
    public class LocalizationTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string key;
            [TextArea] public string zh;
            [TextArea] public string en;

            public string Get(Language lang)
            {
                switch (lang)
                {
                    case Language.English: return string.IsNullOrEmpty(en) ? zh : en;
                    default: return zh;
                }
            }
        }

        public List<Entry> entries = new List<Entry>();

        private Dictionary<string, Entry> _lookup;

        public void BuildLookup()
        {
            _lookup = new Dictionary<string, Entry>(entries.Count);
            foreach (var e in entries)
            {
                if (!string.IsNullOrEmpty(e.key) && !_lookup.ContainsKey(e.key))
                    _lookup.Add(e.key, e);
            }
        }

        public string Get(string key, Language lang)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(key, out var e) ? e.Get(lang) : $"#{key}";
        }

        /// <summary>Well-known keys, so call sites reference constants not literals.</summary>
        public static class Keys
        {
            public const string GameTitle = "game.title";
            public const string Money = "hud.money";
            public const string Population = "hud.population";
            public const string BuyFood = "btn.buy_food";
            public const string HudBuy = "hud.buy";
            public const string LanguageToggle = "btn.language";
            public const string Pause = "btn.pause";
            public const string Resume = "btn.resume";
            public const string HintFeed = "hint.feed";
            public const string GameOver = "state.gameover";

            // Menu
            public const string MenuStart = "menu.start";
            public const string MenuRestart = "menu.restart";
            public const string MenuContinue = "menu.continue";
            public const string MenuSettings = "menu.settings";
            public const string MenuQuit = "menu.quit";

            // Settings
            public const string SettingsTitle = "settings.title";
            public const string SettingsFullscreen = "settings.fullscreen";
            public const string SettingsResolution = "settings.resolution";
            public const string SettingsMusic = "settings.music";
            public const string SettingsSfx = "settings.sfx";
            public const string SettingsLanguage = "settings.language";
            public const string SettingsBack = "settings.back";

            // Alert
            public const string AlertConfirm = "alert.confirm";
            public const string AlertCancel = "alert.cancel";
            public const string AlertQuitTitle = "alert.quit_title";
            public const string AlertQuitMsg = "alert.quit_msg";
        }
    }
}
