using TMPro;
using UnityEngine;
using TCC.Core;
using TCC.Managers;

namespace TCC.UI
{
    /// <summary>
    /// Drop this on any TMP label that shows a static, translatable caption. It
    /// holds a key, applies the shared font, and re-resolves itself whenever the
    /// language changes — so adding a language never touches UI wiring.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key;

        private TMP_Text _text;

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            GameEvents.LanguageChanged += OnLanguageChanged;
            Refresh();
        }

        // OnEnable can run before LocalizationManager.Awake, in which case its early
        // Refresh no-ops and the label stays blank. Start always runs after every
        // Awake, so the manager is alive here and the caption resolves.
        private void Start() => Refresh();

        private void OnDisable()
        {
            GameEvents.LanguageChanged -= OnLanguageChanged;
        }

        public void SetKey(string key)
        {
            _key = key;
            Refresh();
        }

        private void OnLanguageChanged(Language _) => Refresh();

        private void Refresh()
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
            if (!LocalizationManager.Exists) return;
            var loc = LocalizationManager.Instance;
            if (loc.Font != null) _text.font = loc.Font;
            if (!string.IsNullOrEmpty(_key))
                _text.text = loc.Get(_key);
        }
    }
}
