using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.UI;

namespace TCC.Managers
{
    /// <summary>
    /// Binds the HUD to the rest of the game. It listens for money/population
    /// facts and refreshes dynamic labels, and forwards button presses to the
    /// right manager. Static button captions localize themselves via LocalizedText;
    /// this only handles the values that change at runtime.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [SerializeField] private HudView _hud;

        private int _money;
        private int _infants;
        private int _adults;

        private void OnEnable()
        {
            GameEvents.MoneyChanged += OnMoneyChanged;
            GameEvents.PopulationChanged += OnPopulationChanged;
            GameEvents.LanguageChanged += OnLanguageChanged;
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.MoneyChanged -= OnMoneyChanged;
            GameEvents.PopulationChanged -= OnPopulationChanged;
            GameEvents.LanguageChanged -= OnLanguageChanged;
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        private void Start()
        {
            if (_hud != null)
            {
                _hud.BindLanguageButton(OnLanguagePressed);
                _hud.BindPauseButton(OnPausePressed);
                _hud.BindBuyButton(OnBuyPressed);
            }
            RefreshDynamicLabels();
        }

        private void OnMoneyChanged(int money)
        {
            _money = money;
            RefreshDynamicLabels();
        }

        private void OnPopulationChanged(int infants, int adults)
        {
            _infants = infants;
            _adults = adults;
            RefreshDynamicLabels();
        }

        private void OnLanguageChanged(Language _) => RefreshDynamicLabels();

        private void OnGameStateChanged(GameState state)
        {
            if (_hud != null) _hud.SetPaused(state == GameState.Paused);
        }

        private void RefreshDynamicLabels()
        {
            if (_hud == null || !LocalizationManager.Exists) return;
            var loc = LocalizationManager.Instance;
            _hud.SetMoney(string.Format(loc.Get(LocalizationTable.Keys.Money), _money));
            _hud.SetPopulation(string.Format(loc.Get(LocalizationTable.Keys.Population), _infants, _adults));
        }

        private void OnLanguagePressed()
        {
            if (AudioManager.Exists) AudioManager.Instance.PlaySfx(AudioLibrary.Ids.Click);
            if (LocalizationManager.Exists) LocalizationManager.Instance.ToggleLanguage();
        }

        private void OnPausePressed()
        {
            if (AudioManager.Exists) AudioManager.Instance.PlaySfx(AudioLibrary.Ids.Click);
            if (GameManager.Exists) GameManager.Instance.TogglePause();
        }

        private void OnBuyPressed()
        {
            if (AudioManager.Exists) AudioManager.Instance.PlaySfx(AudioLibrary.Ids.Click);
            if (SimulationManager.Exists) SimulationManager.Instance.TryBuyJuvenile();
        }
    }
}
