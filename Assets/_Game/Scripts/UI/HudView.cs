using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TCC.UI
{
    /// <summary>
    /// A thin, dumb view over the HUD widgets. It exposes setters/binders and owns
    /// no game logic — UIManager pushes values in and hooks the buttons. Keeping it
    /// passive means the layout can be re-arranged in the editor freely.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        private Canvas _canvas;

        [Header("Dynamic labels")]
        [SerializeField] private TMP_Text _moneyText;
        [SerializeField] private TMP_Text _populationText;
        [SerializeField] private TMP_Text _foodText;

        [Header("Buttons")]
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _buyFoodButton;
        [SerializeField] private Button _languageButton;
        [SerializeField] private Button _pauseButton;

        [Header("Pause caption (localized child handles text)")]
        [SerializeField] private LocalizedText _pauseLabel;

        [Header("Pause label keys")]
        [SerializeField] private string _pauseKey = "btn.pause";
        [SerializeField] private string _resumeKey = "btn.resume";

        private void Awake() => _canvas = GetComponent<Canvas>();

        public void SetVisible(bool visible)
        {
            if (_canvas == null) _canvas = GetComponent<Canvas>();
            if (_canvas != null) _canvas.enabled = visible;
        }

        public void SetMoney(string s) { if (_moneyText != null) _moneyText.text = s; }
        public void SetPopulation(string s) { if (_populationText != null) _populationText.text = s; }
        public void SetFood(string s) { if (_foodText != null) _foodText.text = s; }

        public void BindBuyButton(Action onClick)
        {
            if (_buyButton == null) return;
            _buyButton.onClick.RemoveAllListeners();
            _buyButton.onClick.AddListener(() => onClick?.Invoke());
        }

        public void BindBuyFoodButton(Action onClick)
        {
            if (_buyFoodButton == null) return;
            _buyFoodButton.onClick.RemoveAllListeners();
            _buyFoodButton.onClick.AddListener(() => onClick?.Invoke());
        }

        public void BindLanguageButton(Action onClick)
        {
            if (_languageButton == null) return;
            _languageButton.onClick.RemoveAllListeners();
            _languageButton.onClick.AddListener(() => onClick?.Invoke());
        }

        public void BindPauseButton(Action onClick)
        {
            if (_pauseButton == null) return;
            _pauseButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.AddListener(() => onClick?.Invoke());
        }

        public void SetPaused(bool paused)
        {
            if (_pauseLabel != null)
                _pauseLabel.SetKey(paused ? _resumeKey : _pauseKey);
        }
    }
}
