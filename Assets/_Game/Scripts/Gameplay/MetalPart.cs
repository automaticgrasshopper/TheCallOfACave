using UnityEngine;
using TCC.Core;
using TCC.Managers;
using TCC.UI;
using TCC.Data;

namespace TCC.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public class MetalPart : MonoBehaviour
    {
        private int _value;
        private float _phase;

        public void Init(int value)
        {
            _value = value;
            _phase = Random.value * 6.28f;
        }

        private void Update()
        {
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 3f + _phase) * 8f);
        }

        private void OnMouseDown()
        {
            if (GameManager.Exists && GameManager.Instance.State != GameState.Playing) return;
            GameEvents.RaiseMoneyEarned(_value);
            Destroy(gameObject);
        }
    }
}
