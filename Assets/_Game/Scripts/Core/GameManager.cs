using UnityEngine;
using TCC.UI;

namespace TCC.Core
{
    /// <summary>
    /// Owns the high-level game flow and is the scene entry point. It does NOT
    /// implement gameplay itself; it just holds the current <see cref="GameState"/>
    /// and broadcasts transitions through <see cref="GameEvents"/>. The other
    /// managers live as sibling components and react to those events.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private GameState _state = GameState.Boot;

        /// <summary>
        /// Set before a scene reload to skip the title menu and drop straight into
        /// play (used by "Restart"). Survives the reload because it is static.
        /// </summary>
        public static bool BootIntoPlay;

        public GameState State => _state;
        public float SessionSeconds { get; private set; }

        private void Update()
        {
            if (_state == GameState.Playing)
                SessionSeconds += Time.unscaledDeltaTime;
        }

        public static string FormatSessionTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int hours = total / 3600;
            int minutes = (total % 3600) / 60;
            int remainingSeconds = total % 60;
            return hours > 0
                ? string.Format("{0}:{1:00}:{2:00}", hours, minutes, remainingSeconds)
                : string.Format("{0:00}:{1:00}", minutes, remainingSeconds);
        }

        public int ColonyYear => Mathf.Clamp(1 + Mathf.FloorToInt(SessionSeconds / 5f), 1, 99999);

        public string FormatColonyYear() => ColonyYear.ToString("D4");

        protected override void OnAwake()
        {
            // Managers configure themselves in their own Awake. We only kick the
            // flow once everything in the scene is alive.
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("TCC.PlayGameplay", false))
            {
                UnityEditor.SessionState.SetBool("TCC.PlayGameplay", false);
                BootIntoPlay = true;
            }
#endif
            // The loss overlay is still backward-compatible with older scenes.
            // The art director itself is now a normal, scene-wired component.
            GameOverView.Ensure();
            if (BootIntoPlay)
            {
                BootIntoPlay = false;
                SetState(GameState.Playing);
                if (TitleMenuView.Exists) TitleMenuView.Instance.Hide();
            }
            else
            {
                // Boot into the title menu with the simulation frozen behind it.
                Time.timeScale = 0f;
                GameEvents.RaiseGameStateChanged(_state);
            }
        }

        public void SetState(GameState next)
        {
            if (_state == next) return;
            _state = next;
            Time.timeScale = next == GameState.Playing ? 1f : 0f;
            GameEvents.RaiseGameStateChanged(next);
        }

        public void TogglePause()
        {
            SetState(_state == GameState.Paused ? GameState.Playing : GameState.Paused);
        }
    }
}
