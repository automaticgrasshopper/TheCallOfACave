using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TCC.Managers;

namespace TCC.UI
{
    /// <summary>
    /// Centered confirm/cancel modal. Callers pass localization keys plus optional
    /// callbacks; the view resolves the text and routes the button presses. It owns
    /// its own dim backdrop so it reads as modal without needing the Block curtain.
    /// </summary>
    public class AlertView : UIPanel<AlertView>
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _message;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private Action _onConfirm;
        private Action _onCancel;

        protected override void OnInit()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(HandleConfirm);
            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(HandleCancel);
        }

        /// <param name="titleKey">Localization key for the heading.</param>
        /// <param name="messageKey">Localization key for the body.</param>
        public void Show(string titleKey, string messageKey, Action onConfirm, Action onCancel = null)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            var loc = LocalizationManager.Exists ? LocalizationManager.Instance : null;
            if (_title != null) _title.text = loc != null ? loc.Get(titleKey) : titleKey;
            if (_message != null) _message.text = loc != null ? loc.Get(messageKey) : messageKey;
            Show();
        }

        private void HandleConfirm()
        {
            if (AudioManager.Exists) AudioManager.Instance.PlaySfx(Data.AudioLibrary.Ids.Click);
            var cb = _onConfirm;
            Hide();
            cb?.Invoke();
        }

        private void HandleCancel()
        {
            if (AudioManager.Exists) AudioManager.Instance.PlaySfx(Data.AudioLibrary.Ids.Click);
            var cb = _onCancel;
            Hide();
            cb?.Invoke();
        }
    }
}
