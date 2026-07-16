using System.Collections;
using UnityEngine;
using TCC.Core;

namespace TCC.UI
{
    /// <summary>
    /// Base for every overlay panel. The script and its Canvas live on an
    /// always-active container so coroutines never die; "closed" means the Canvas
    /// component is disabled and the CanvasGroup alpha is 0 — the GameObject itself
    /// is never deactivated. Fades run on unscaled time so panels still animate
    /// while the game is paused (timeScale = 0).
    /// </summary>
    public abstract class UIPanel<T> : Singleton<T> where T : UIPanel<T>
    {
        [SerializeField] protected Canvas _canvas;
        [SerializeField] protected CanvasGroup _group;
        [SerializeField] protected float _fadeDuration = 0.18f;
        [Tooltip("If false the panel never blocks raycasts (e.g. a toast).")]
        [SerializeField] protected bool _blocksInput = true;

        private Coroutine _fadeRoutine;

        public bool IsVisible { get; private set; }

        protected override void OnAwake()
        {
            if (_canvas == null) _canvas = GetComponent<Canvas>();
            if (_group == null) _group = GetComponent<CanvasGroup>();
            HideImmediate();
            OnInit();
        }

        /// <summary>Run once after the panel caches its components (still hidden).</summary>
        protected virtual void OnInit() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        public void Show()
        {
            bool wasVisible = IsVisible;
            IsVisible = true;
            if (_canvas != null) _canvas.enabled = true;
            if (_group != null) { _group.interactable = _blocksInput; _group.blocksRaycasts = _blocksInput; }
            StartFade(1f, disableAfter: false);
            if (!wasVisible) OnShow();
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            if (_group != null) { _group.interactable = false; _group.blocksRaycasts = false; }
            StartFade(0f, disableAfter: true);
            OnHide();
        }

        public void HideImmediate()
        {
            IsVisible = false;
            if (_group != null)
            {
                _group.alpha = 0f;
                _group.interactable = false;
                _group.blocksRaycasts = false;
            }
            if (_canvas != null) _canvas.enabled = false;
        }

        private void StartFade(float target, bool disableAfter)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine(target, disableAfter));
        }

        private IEnumerator FadeRoutine(float target, bool disableAfter)
        {
            if (_group == null)
            {
                if (disableAfter && _canvas != null) _canvas.enabled = false;
                yield break;
            }
            float start = _group.alpha;
            float t = 0f;
            while (t < _fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(start, target, t / _fadeDuration);
                yield return null;
            }
            _group.alpha = target;
            if (disableAfter && _canvas != null) _canvas.enabled = false;
            _fadeRoutine = null;
        }
    }
}
