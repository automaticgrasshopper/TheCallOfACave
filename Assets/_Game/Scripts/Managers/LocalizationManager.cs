using TMPro;
using UnityEngine;
using TCC.Core;
using TCC.Data;

namespace TCC.Managers
{
    /// <summary>
    /// Central text/language authority. Holds the current language, resolves keys
    /// against the <see cref="LocalizationTable"/>, and owns the TMP font that all
    /// localized labels use. Language changes are broadcast so every
    /// <c>LocalizedText</c> refreshes itself — no direct references needed.
    /// </summary>
    public class LocalizationManager : Singleton<LocalizationManager>
    {
        private const string PrefKey = "tcc.language";

        [SerializeField] private LocalizationTable _table;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Language _language = Language.ChineseSimplified;

        public Language Current => _language;
        public TMP_FontAsset Font => _font;

        protected override void OnAwake()
        {
            // Persisted choice wins; otherwise follow the machine's language,
            // defaulting to English for anything non-Chinese.
            if (PlayerPrefs.HasKey(PrefKey))
                _language = (Language)PlayerPrefs.GetInt(PrefKey);
            else
                _language = DetectSystemLanguage();
            if (_table != null) _table.BuildLookup();
        }

        private static Language DetectSystemLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return Language.ChineseSimplified;
                default:
                    return Language.English;
            }
        }

        // ---- dropdown mapping (Settings UI) -----------------------------
        // Dropdown row order: 0 = English, 1 = 简体中文.
        public static Language IndexToLanguage(int index)
            => index == 1 ? Language.ChineseSimplified : Language.English;

        public static int LanguageToIndex(Language lang)
            => lang == Language.ChineseSimplified ? 1 : 0;

        public int CurrentIndex => LanguageToIndex(_language);

        public string Get(string key)
            => _table != null ? _table.Get(key, _language) : $"#{key}";

        public void SetLanguage(Language lang)
        {
            if (_language == lang) return;
            _language = lang;
            PlayerPrefs.SetInt(PrefKey, (int)lang);
            GameEvents.RaiseLanguageChanged(lang);
        }

        /// <summary>Convenience for a single toggle button (zh &lt;-&gt; en).</summary>
        public void ToggleLanguage()
        {
            SetLanguage(_language == Language.ChineseSimplified
                ? Language.English
                : Language.ChineseSimplified);
        }
    }
}
