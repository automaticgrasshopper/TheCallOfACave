using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TCC.Core;
using TCC.Data;
using TCC.Managers;

namespace TCC.UI
{
    /// <summary>
    /// The title / pause menu: Start-or-Restart (by game state), Continue, Settings,
    /// and Quit (with a confirm alert). Doubles as the pause screen — Esc opens it
    /// (pausing the sim) and closes it (resuming). It only drives flow through
    /// GameManager and the other views; it holds no gameplay logic itself.
    /// </summary>
    public class TitleMenuView : UIPanel<TitleMenuView>
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private LocalizedText _startLabel;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        protected override void OnInit()
        {
            if (_startButton != null) _startButton.onClick.AddListener(OnStart);
            if (_continueButton != null) _continueButton.onClick.AddListener(OnContinue);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettings);
            if (_quitButton != null) _quitButton.onClick.AddListener(OnQuit);
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        protected override void OnDestroy()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
            base.OnDestroy();
        }

        // At boot GameManager freezes the sim and announces Boot; that's our cue to
        // present the start menu. Other transitions are driven by the buttons / Esc.
        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Boot) Show();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            // A modal alert must be answered before anything else backs out.
            if (AlertView.Exists && AlertView.Instance.IsVisible) return;
            if (SettingsView.Exists && SettingsView.Instance.IsVisible)
            {
                SettingsView.Instance.Hide();
                return;
            }
            // The game hasn't started yet — Esc can't dismiss the start menu.
            var state = GameManager.Exists ? GameManager.Instance.State : GameState.Boot;
            if (state == GameState.Boot) return;
            if (IsVisible) OnContinue();
            else OpenMenu();
        }

        private void OpenMenu()
        {
            if (GameManager.Exists) GameManager.Instance.SetState(GameState.Paused);
            Show();
        }

        protected override void OnShow()
        {
            var state = GameManager.Exists ? GameManager.Instance.State : GameState.Boot;
            bool inProgress = state == GameState.Playing || state == GameState.Paused;

            if (_startLabel != null)
                _startLabel.SetKey(inProgress ? LocalizationTable.Keys.MenuRestart
                                              : LocalizationTable.Keys.MenuStart);
            if (_continueButton != null)
                _continueButton.interactable = state == GameState.Paused;
        }

        private void Click() { if (AudioManager.Exists) AudioManager.Instance.PlaySfx(AudioLibrary.Ids.Click); }

        private void OnStart()
        {
            Click();
            var state = GameManager.Exists ? GameManager.Instance.State : GameState.Boot;
            if (state == GameState.Boot)
            {
                // First run: nothing to reset, just begin the simulation.
                Hide();
                if (GameManager.Exists) GameManager.Instance.SetState(GameState.Playing);
                return;
            }
            // Mid-game restart: reload the scene, then boot straight into play.
            GameManager.BootIntoPlay = true;
            Time.timeScale = 1f;
            if (BlockView.Exists)
                BlockView.Instance.Transition(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnContinue()
        {
            Click();
            Hide();
            if (GameManager.Exists) GameManager.Instance.SetState(GameState.Playing);
        }

        private void OnSettings()
        {
            Click();
            if (SettingsView.Exists) SettingsView.Instance.Show();
        }

        private void OnQuit()
        {
            Click();
            if (AlertView.Exists)
                AlertView.Instance.Show(LocalizationTable.Keys.AlertQuitTitle,
                                        LocalizationTable.Keys.AlertQuitMsg, QuitApp);
            else
                QuitApp();
        }

        private void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
