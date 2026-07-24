using System;
using UnityEngine;
using TCC.Core;
using TCC.Managers;

namespace TCC.Persistence
{
    /// <summary>
    /// Active-profile save session. It remains dormant until profile UI or a test
    /// explicitly starts a session.
    /// </summary>
    public sealed class SaveGameController : MonoBehaviour
    {
        public const float AutoSaveIntervalSeconds = 60f;

        private static SaveGameController _instance;
        private ProfileRepository _repository;
        private PlayerProfile _profile;
        private float _autoSaveElapsed;
        private double _lastRecordedSessionSeconds;
        private bool _active;
        private bool _saving;

        public static bool HasActiveSession => _instance != null && _instance._active;
        public static SaveGameController Instance => _instance;
        public string ActiveProfileId => _profile?.ProfileId;
        public bool LastRestoreUsedBackup { get; private set; }
        public string LastSaveReason { get; private set; }

        public static SaveGameController StartSession(
            string profileId,
            ProfileRepository repository = null,
            bool restoreWorld = true)
        {
            if (_instance == null)
            {
                var host = new GameObject("[Save Game Controller]");
                DontDestroyOnLoad(host);
                _instance = host.AddComponent<SaveGameController>();
            }

            _instance.Configure(profileId, repository ?? new ProfileRepository(), restoreWorld);
            return _instance;
        }

        private void OnEnable()
        {
            GameEvents.SaveRequested += OnSaveRequested;
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.SaveRequested -= OnSaveRequested;
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            if (!_active || !GameManager.Exists ||
                GameManager.Instance.State != GameState.Playing)
                return;

            _autoSaveElapsed += Time.unscaledDeltaTime;
            if (_autoSaveElapsed < AutoSaveIntervalSeconds) return;
            SaveNow("autosave-60s");
        }

        private void Configure(
            string profileId,
            ProfileRepository repository,
            bool restoreWorld)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            _repository = repository;
            _profile = repository.LoadProfile(profileId).Profile;
            _active = true;
            _autoSaveElapsed = 0f;
            LastRestoreUsedBackup = false;

            if (restoreWorld)
            {
                WorldSnapshotLoadResult result = repository.LoadWorldSnapshot(profileId);
                WorldStateService.Restore(result.Snapshot);
                LastRestoreUsedBackup = result.UsedBackup;
            }

            _lastRecordedSessionSeconds = GameManager.Exists
                ? GameManager.Instance.SessionSeconds
                : 0d;

            if (!restoreWorld)
                SaveNow("session-enter");
        }

        public void SaveNow(string reason)
        {
            if (!_active || _saving) return;
            _saving = true;
            try
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                WorldSnapshot snapshot = WorldStateService.Capture(_profile.ProfileId, now);
                _repository.SaveWorldSnapshot(_profile.ProfileId, snapshot);

                double currentSession = snapshot.sessionSeconds;
                double elapsed = Math.Max(0d, currentSession - _lastRecordedSessionSeconds);
                _profile.RecordPlaySession(elapsed, Math.Max(now, _profile.UpdatedAtUnixMilliseconds));
                _repository.SaveProfile(_profile);
                _lastRecordedSessionSeconds = currentSession;
                _autoSaveElapsed = 0f;
                LastSaveReason = string.IsNullOrEmpty(reason) ? "unspecified" : reason;
                Debug.Log($"[TCC Save] Saved profile {_profile.ProfileId}: {LastSaveReason}.");
            }
            finally
            {
                _saving = false;
            }
        }

        public void StopSession(bool save = true)
        {
            if (save) SaveNow("session-exit");
            _active = false;
            _profile = null;
            _repository = null;
        }

        private void OnSaveRequested(string reason) => SaveNow(reason);

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
                SaveNow("game-over");
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow("application-pause");
        }

        private void OnApplicationQuit()
        {
            if (_active)
                SaveNow("application-quit");
        }
    }
}
