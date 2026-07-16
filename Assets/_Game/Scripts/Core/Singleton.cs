using UnityEngine;

namespace TCC.Core
{
    /// <summary>
    /// Lightweight MonoBehaviour singleton base. Managers derive from this so
    /// gameplay code can reach a single instance without wiring hard references
    /// everywhere. Managers still talk to each other through <see cref="GameEvents"/>
    /// rather than by calling one another directly.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        public static bool Exists => Instance != null;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;
            OnAwake();
        }

        /// <summary>Override instead of Awake so the singleton guard always runs first.</summary>
        protected virtual void OnAwake() { }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
