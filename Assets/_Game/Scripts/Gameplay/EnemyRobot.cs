using UnityEngine;
using TCC.Managers;

namespace TCC.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public class EnemyRobot : MonoBehaviour
    {
        private SimulationManager _sim;
        private float _health;
        private float _attackTimer;
        private Creature _target;
        [SerializeField] private bool _heavy;
        public Vector2 Position => transform.position;
        public bool Alive => _health > 0f;
        public bool IsHeavy => _heavy;

        public void Init(SimulationManager sim)
        {
            _sim = sim;
            _health = _heavy ? sim.Config.heavyEnemyMaxHealth : sim.Config.enemyMaxHealth;
        }

        private void Update()
        {
            if (_sim == null || !Alive) return;
            if (_target == null || !_target.IsSoldier) _target = _sim.ClosestSoldier(Position);
            Vector2 destination = _target != null ? (Vector2)_target.transform.position : new Vector2(-8f, 0f);
            Vector2 delta = destination - Position;
            if (delta.sqrMagnitude > .7f * .7f)
            {
                float speed = _heavy ? .48f : .72f;
                transform.position = _sim.ClampWorld(Position + delta.normalized * speed * Time.deltaTime);
                GetComponent<SpriteRenderer>().flipX = delta.x < 0f;
            }
            else if (_target != null)
            {
                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0f)
                {
                    _attackTimer = _sim.Config.attackInterval;
                    float damage = _heavy ? _sim.Config.heavyEnemyDamage : _sim.Config.enemyDamage;
                    _target.TakeCombatDamage(damage, delta.normalized);
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (!Alive) return;
            _health -= amount;
            if (_health > 0f) return;
            _sim?.RemoveEnemy(this);
            Destroy(gameObject);
        }
    }
}
