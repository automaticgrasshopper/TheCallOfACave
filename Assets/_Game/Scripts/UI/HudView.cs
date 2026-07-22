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
        public void SetSessionTime(string s) { if (_sessionTimeText != null) _sessionTimeText.text = s; }

        private void EnsureSessionTimer()
        {
            if (_sessionTimeText != null || _moneyText == null) return;
            var card = _moneyText.transform.parent as RectTransform;
            if (card == null) return;

            var badge = new GameObject("Session Time Badge", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            badge.transform.SetParent(card, false);
            var badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-14f, -12f);
            badgeRect.sizeDelta = new Vector2(142f, 72f);
            badge.GetComponent<Image>().color = new Color(.035f, .075f, .078f, .98f);
            var outline = badge.GetComponent<Outline>();
            outline.effectColor = new Color(.25f, .72f, .68f, .75f);
            outline.effectDistance = new Vector2(1f, -1f);

            var label = new GameObject("Session Time", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(badge.transform, false);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 2f);
            labelRect.offsetMax = new Vector2(-8f, -2f);
            _sessionTimeText = label.GetComponent<TextMeshProUGUI>();
            _sessionTimeText.font = _moneyText.font;
            _sessionTimeText.fontSize = 17f;
            _sessionTimeText.lineSpacing = 8f;
            _sessionTimeText.alignment = TextAlignmentOptions.MidlineLeft;
            _sessionTimeText.color = new Color(.72f, .95f, .88f, 1f);
            _sessionTimeText.raycastTarget = false;

            var moneyRect = _moneyText.rectTransform;
            moneyRect.anchoredPosition = new Vector2(-70f, moneyRect.anchoredPosition.y);
            moneyRect.sizeDelta = new Vector2(178f, moneyRect.sizeDelta.y);
            if (_populationText != null)
            {
                var populationRect = _populationText.rectTransform;
                populationRect.anchoredPosition = new Vector2(-70f, populationRect.anchoredPosition.y);
                populationRect.sizeDelta = new Vector2(178f, populationRect.sizeDelta.y);
                _populationText.fontSize = 18f;
            }
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
