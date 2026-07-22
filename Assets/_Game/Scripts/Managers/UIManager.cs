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
        private int _food;
        private int _displayedSessionSecond = -1;

        private void Update()
        {
            if (_hud == null || !GameManager.Exists) return;
            int second = Mathf.FloorToInt(GameManager.Instance.SessionSeconds);
            if (second == _displayedSessionSecond) return;
            _displayedSessionSecond = second;
            RefreshSessionTime();
        }

        private void OnEnable()
        {
            GameEvents.MoneyChanged += OnMoneyChanged;
            GameEvents.FoodChanged += OnFoodChanged;
            GameEvents.PopulationChanged += OnPopulationChanged;
            GameEvents.LanguageChanged += OnLanguageChanged;
            GameEvents.GameStateChanged += OnGameStateChanged;
            GameEvents.EggLaid += OnEggLaid;
            GameEvents.EggCollected += OnEggCollected;
            GameEvents.CreatureBorn += OnCreatureBorn;
            GameEvents.CreatureDied += OnCreatureDied;
        }

        private void OnDisable()
        {
            GameEvents.MoneyChanged -= OnMoneyChanged;
            GameEvents.FoodChanged -= OnFoodChanged;
            GameEvents.PopulationChanged -= OnPopulationChanged;
            GameEvents.LanguageChanged -= OnLanguageChanged;
            GameEvents.GameStateChanged -= OnGameStateChanged;
            GameEvents.EggLaid -= OnEggLaid;
            GameEvents.EggCollected -= OnEggCollected;
            GameEvents.CreatureBorn -= OnCreatureBorn;
            GameEvents.CreatureDied -= OnCreatureDied;
        }

        private void Start()
        {
            if (_hud != null)
            {
                _hud.BindLanguageButton(OnLanguagePressed);
                _hud.BindPauseButton(OnPausePressed);
                _hud.BindBuyButton(OnBuyPressed);
                _hud.BindBuyFoodButton(OnBuyFoodPressed);
            }
            if (_hud != null && GameManager.Exists)
                ApplyGameState(GameManager.Instance.State);
            RefreshDynamicLabels();
        }

        private void OnFoodChanged(int food) { _food = food; RefreshDynamicLabels(); }

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
            ApplyGameState(state);
        }

        private void ApplyGameState(GameState state)
        {
            if (_hud == null) return;
            _hud.SetVisible(state == GameState.Playing || state == GameState.Paused);
            _hud.SetPaused(state == GameState.Paused);
        }

        private void OnEggLaid(Vector2 _) => ToastView.Instance?.Key(LocalizationTable.Keys.ToastEggLaid);
        private void OnEggCollected(int value, Vector2 _)
        {
            if (!ToastView.Exists || !LocalizationManager.Exists) return;
            ToastView.Instance.Message(string.Format(
                LocalizationManager.Instance.Get(LocalizationTable.Keys.ToastEggSold), value));
        }
        private void OnCreatureBorn(Vector2 _) => ToastView.Instance?.Key(LocalizationTable.Keys.ToastBugBorn);
        private void OnCreatureDied(Vector2 _) => ToastView.Instance?.Key(LocalizationTable.Keys.ToastBugDied);

        private void RefreshDynamicLabels()
        {
            if (_hud == null || !LocalizationManager.Exists) return;
            var loc = LocalizationManager.Instance;
            _hud.SetMoney(string.Format(loc.Get(LocalizationTable.Keys.Money), _money));
            _hud.SetPopulation(string.Format(loc.Get(LocalizationTable.Keys.Population), _infants, _adults));
            _hud.SetFood(string.Format(loc.Get(LocalizationTable.Keys.Food), _food));
            RefreshSessionTime();
        }

        private void RefreshSessionTime()
        {
            if (_hud == null || !LocalizationManager.Exists || !GameManager.Exists) return;
            string duration = GameManager.FormatSessionTime(GameManager.Instance.SessionSeconds);
            _hud.SetSessionTime(string.Format(
                LocalizationManager.Instance.Get(LocalizationTable.Keys.SessionTime),
                duration, GameManager.Instance.FormatColonyYear()));
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

        private void OnBuyFoodPressed()
        {
            if (AudioManager.Exists) AudioManager.Instance.PlaySfx(AudioLibrary.Ids.Click);
            if (EconomyManager.Exists) EconomyManager.Instance.TryBuyFood();
        }
    }
}
