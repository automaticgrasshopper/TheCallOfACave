using UnityEngine;
using TCC.Managers;

namespace TCC.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public class ContaminationSource : MonoBehaviour
    {
        private float _scan;
        public Vector2 Position => transform.position;

        private void Update()
        {
            transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 2.4f) * .035f);
            _scan -= Time.deltaTime;
            if (_scan > 0f || !SimulationManager.Exists) return;
            _scan = .35f;
            foreach (var creature in SimulationManager.Instance.Creatures)
                if (creature != null && ((Vector2)creature.transform.position - Position).sqrMagnitude < .72f * .72f)
                    creature.Infect();
        }

        public void Cleaned() => Destroy(gameObject);
    }
}
