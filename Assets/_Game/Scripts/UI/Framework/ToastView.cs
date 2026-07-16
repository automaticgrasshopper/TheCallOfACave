using System.Collections;
using TMPro;
using UnityEngine;
using TCC.Managers;

namespace TCC.UI
{
    /// <summary>
    /// Transient notification that fades in, holds, and fades out. Call
    /// <see cref="Message"/> with literal text or <see cref="Key"/> with a
    /// localization key.
    /// </summary>
    public class ToastView : UIPanel<ToastView>
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _holdSeconds = 1.6f;

        private Coroutine _lifeRoutine;

        public void Message(string text, float hold = -1f)
        {
            if (_text != null) _text.text = text;
            Show();
            if (_lifeRoutine != null) StopCoroutine(_lifeRoutine);
            _lifeRoutine = StartCoroutine(Life(hold < 0f ? _holdSeconds : hold));
        }

        public void Key(string localizationKey, float hold = -1f)
        {
            string s = LocalizationManager.Exists
                ? LocalizationManager.Instance.Get(localizationKey)
                : localizationKey;
            Message(s, hold);
        }

        private IEnumerator Life(float hold)
        {
            yield return new WaitForSecondsRealtime(_fadeDuration + hold);
            Hide();
            _lifeRoutine = null;
        }
    }
}
