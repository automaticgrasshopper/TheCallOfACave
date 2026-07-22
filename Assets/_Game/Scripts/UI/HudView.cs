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
        [SerializeField] private TMP_Text _sessionTimeText;
        [SerializeField] private TMP_Text _colonyYearText;

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

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            EnsureSessionTimer();
        }

        public void SetVisible(bool visible)
        {
            if (_canvas == null) _canvas = GetComponent<Canvas>();
            if (_canvas != null) _canvas.enabled = visible;
        }

        public void SetMoney(string s) { if (_moneyText != null) _moneyText.text = s; }
        public void SetPopulation(string s) { if (_populationText != null) _populationText.text = s; }
        public void SetFood(string s) { if (_foodText != null) _foodText.text = s; }
        public void SetSessionClock(string time, string year)
        {
            if (_sessionTimeText != null) _sessionTimeText.text = "TIME: " + time;
            if (_colonyYearText != null) _colonyYearText.text = "YEAR: " + year;
        }

        private void EnsureSessionTimer()
        {
            if (_sessionTimeText != null || _moneyText == null) return;
            var canvasRoot = _canvas != null ? _canvas.transform as RectTransform : transform as RectTransform;
            if (canvasRoot == null) return;

            var badge = new GameObject("Session Clock", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            badge.transform.SetParent(canvasRoot, false);
            var badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(.5f, 1f);
            badgeRect.pivot = new Vector2(.5f, 1f);
            badgeRect.anchoredPosition = new Vector2(0f, -18f);
            badgeRect.sizeDelta = new Vector2(360f, 112f);
            badge.GetComponent<Image>().color = new Color(.012f, .035f, .038f, .94f);
            var outline = badge.GetComponent<Outline>();
            outline.effectColor = new Color(.25f, .72f, .68f, .75f);
            outline.effectDistance = new Vector2(2f, -2f);

            _sessionTimeText = ClockLine(badge.transform, "Time", new Vector2(0f, -10f));
            _colonyYearText = ClockLine(badge.transform, "Year", new Vector2(0f, -57f));
        }

        private TMP_Text ClockLine(Transform parent, string name, Vector2 position)
        {
            var line = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            line.transform.SetParent(parent, false);
            var rect = (RectTransform)line.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(320f, 43f);
            var text = line.GetComponent<TextMeshProUGUI>();
            text.font = _moneyText.font;
            text.fontSize = 28f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.76f, .98f, .91f, 1f);
            text.raycastTarget = false;
            return text;
        }

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
