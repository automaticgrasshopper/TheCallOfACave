using UnityEngine;
using TCC.Core;
using TCC.Data;

namespace TCC.Gameplay
{
    /// <summary>One-slot training bay. Dropping a normal adult here commits it to
    /// two colony-years of training; its remaining life is then halved on promotion.</summary>
    public class BarracksZone : MonoBehaviour
    {
        [SerializeField] private float _radius = 1.35f;
        [SerializeField] private float _duration = 10f;
        [SerializeField] private SpriteRenderer _barBack;
        [SerializeField] private SpriteRenderer _barFill;
        private Creature _trainee;
        private float _elapsed;

        public float Radius => _radius;
        public Vector2 Center => transform.position;
        public bool Contains(Vector2 world) => (world - Center).sqrMagnitude <= _radius * _radius;

        public void Configure(float radius, float duration)
        {
            _radius = radius;
            _duration = Mathf.Max(.1f, duration);
        }

        public bool TryTrain(Creature creature)
        {
            if (_trainee != null || creature == null || creature.Stage != CreatureStage.Adult) return false;
            _trainee = creature;
            _elapsed = 0f;
            creature.BeginSoldierTraining();
            if (TCC.UI.ToastView.Exists)
                TCC.UI.ToastView.Instance.Key(LocalizationTable.Keys.ToastTrainingStarted);
            return true;
        }

        private void Update()
        {
            if (_trainee == null) { SetBar(0f, false); return; }
            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / _duration);
            SetBar(progress, true);
            if (progress < 1f) return;

            var finished = _trainee;
            _trainee = null;
            finished.CompleteSoldierTraining();
            SetBar(0f, false);
            if (TCC.UI.ToastView.Exists)
                TCC.UI.ToastView.Instance.Key(LocalizationTable.Keys.ToastSoldierReady);
        }

        private void SetBar(float progress, bool visible)
        {
            if (_barBack == null) return;
            _barBack.enabled = visible; _barFill.enabled = visible;
            if (!visible) return;
            _barFill.transform.localScale = new Vector3(Mathf.Max(.01f, progress), .62f, 1f);
            _barFill.transform.localPosition = new Vector3(-.5f + progress * .5f, 0f, -.01f);
        }

    }
}
