using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TCC.Core;
using TCC.Managers;

namespace TCC.UI
{
    /// <summary>
    /// Settings panel: window mode, resolution, music &amp; sfx volume, and language.
    /// It is a thin binding layer — each control forwards to the manager that owns
    /// that concern (SettingsManager / AudioManager / LocalizationManager), and
    /// reads current values back on open without firing change callbacks.
    /// </summary>
    public class SettingsView : UIPanel<SettingsView>
    {
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private TMP_Dropdown _languageDropdown;
        [SerializeField] private Button _backButton;

        protected override void OnInit()
        {
            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.ClearOptions();
                _resolutionDropdown.AddOptions(new List<string>(SettingsManager.ResolutionLabels()));
            }
            if (_languageDropdown != null)
            {
                _languageDropdown.ClearOptions();
                // Row order must match LocalizationManager.IndexToLanguage: 0=EN, 1=ZH.
                _languageDropdown.AddOptions(new List<string> { "English", "简体中文" });
            }

            if (_fullscreenToggle != null)
                _fullscreenToggle.onValueChanged.AddListener(v =>
                { if (SettingsManager.Exists) SettingsManager.Instance.SetFullscreen(v); });

            if (_resolutionDropdown != null)
                _resolutionDropdown.onValueChanged.AddListener(i =>
                { if (SettingsManager.Exists) SettingsManager.Instance.SetResolutionIndex(i); });

            if (_musicSlider != null)
                _musicSlider.onValueChanged.AddListener(v =>
                { if (AudioManager.Exists) AudioManager.Instance.SetMusicVolume(v); });

            if (_sfxSlider != null)
                _sfxSlider.onValueChanged.AddListener(v =>
                { if (AudioManager.Exists) AudioManager.Instance.SetSfxVolume(v); });

            if (_languageDropdown != null)
                _languageDropdown.onValueChanged.AddListener(i =>
                { if (LocalizationManager.Exists) LocalizationManager.Instance.SetLanguage(LocalizationManager.IndexToLanguage(i)); });

            if (_backButton != null)
                _backButton.onClick.AddListener(() =>
                {
                    if (AudioManager.Exists) AudioManager.Instance.PlaySfx(Data.AudioLibrary.Ids.Click);
                    Hide();
                });
        }

        protected override void OnShow()
        {
            RefreshValues();
        }

        private void RefreshValues()
        {
            if (_fullscreenToggle != null && SettingsManager.Exists)
                _fullscreenToggle.SetIsOnWithoutNotify(SettingsManager.Instance.Fullscreen);
            if (_resolutionDropdown != null && SettingsManager.Exists)
            {
                _resolutionDropdown.SetValueWithoutNotify(SettingsManager.Instance.ResolutionIndex);
                _resolutionDropdown.RefreshShownValue();
            }
            if (_musicSlider != null && AudioManager.Exists)
                _musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            if (_sfxSlider != null && AudioManager.Exists)
                _sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
            if (_languageDropdown != null && LocalizationManager.Exists)
            {
                _languageDropdown.SetValueWithoutNotify(LocalizationManager.Instance.CurrentIndex);
                _languageDropdown.RefreshShownValue();
            }
        }
    }
}
