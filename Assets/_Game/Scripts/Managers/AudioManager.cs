using UnityEngine;
using TCC.Core;
using TCC.Data;

namespace TCC.Managers
{
    /// <summary>
    /// Plays music and SFX. It is deliberately a pure listener: it subscribes to
    /// gameplay facts on <see cref="GameEvents"/> and turns them into sound, so no
    /// gameplay script ever holds an AudioClip or calls the audio system directly.
    /// Music and SFX volumes are independent, persisted, and driven by the Settings UI.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        private const string PrefMusic = "tcc.vol.music";
        private const string PrefSfx = "tcc.vol.sfx";

        [SerializeField] private AudioLibrary _library;
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        [Range(0f, 1f)] public float masterVolume = 1f;

        [SerializeField, Range(0f, 1f)] private float _musicVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

        public float MusicVolume => _musicVolume;
        public float SfxVolume => _sfxVolume;

        protected override void OnAwake()
        {
            EnsureSources();
            if (PlayerPrefs.HasKey(PrefMusic)) _musicVolume = PlayerPrefs.GetFloat(PrefMusic);
            if (PlayerPrefs.HasKey(PrefSfx)) _sfxVolume = PlayerPrefs.GetFloat(PrefSfx);
        }

        private void OnEnable()
        {
            GameEvents.EggLaid += OnEggLaid;
            GameEvents.EggCollected += OnEggCollected;
            GameEvents.CreatureBorn += OnBorn;
            GameEvents.CreatureDied += OnDied;
        }

        private void OnDisable()
        {
            GameEvents.EggLaid -= OnEggLaid;
            GameEvents.EggCollected -= OnEggCollected;
            GameEvents.CreatureBorn -= OnBorn;
            GameEvents.CreatureDied -= OnDied;
        }

        private void Start()
        {
            PlayMusic();
        }

        private void EnsureSources()
        {
            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
                _musicSource.loop = true;
                _musicSource.playOnAwake = false;
            }
            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
            }
        }

        public void PlayMusic()
        {
            if (_library == null || _library.music == null || _musicSource == null) return;
            _musicSource.clip = _library.music;
            _musicSource.volume = _library.musicVolume * _musicVolume * masterVolume;
            _musicSource.Play();
        }

        public void PlaySfx(string id)
        {
            if (_library == null || _sfxSource == null) return;
            foreach (var e in _library.sfx)
            {
                if (e.id == id)
                {
                    if (e.clip != null)
                        _sfxSource.PlayOneShot(e.clip, e.volume * _sfxVolume * masterVolume);
                    return;
                }
            }
        }

        // ---- volume controls (Settings UI) ------------------------------
        public void SetMusicVolume(float v)
        {
            _musicVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(PrefMusic, _musicVolume);
            if (_musicSource != null && _library != null)
                _musicSource.volume = _library.musicVolume * _musicVolume * masterVolume;
        }

        public void SetSfxVolume(float v)
        {
            _sfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(PrefSfx, _sfxVolume);
        }

        // ---- event handlers ----
        private void OnEggLaid(Vector2 _) => PlaySfx(AudioLibrary.Ids.EggLay);
        private void OnEggCollected(int _, Vector2 __) => PlaySfx(AudioLibrary.Ids.EggCollect);
        private void OnBorn(Vector2 _) => PlaySfx(AudioLibrary.Ids.Birth);
        private void OnDied(Vector2 _) => PlaySfx(AudioLibrary.Ids.Death);
    }
}
