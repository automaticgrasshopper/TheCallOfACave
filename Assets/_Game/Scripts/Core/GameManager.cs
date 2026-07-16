using UnityEngine;

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

        protected override void OnAwake()
        {
            // Managers configure themselves in their own Awake. We only kick the
            // flow once everything in the scene is alive.
        }

        private void Start()
        {
            if (BootIntoPlay)
            {
                BootIntoPlay = false;
                SetState(GameState.Playing);
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
