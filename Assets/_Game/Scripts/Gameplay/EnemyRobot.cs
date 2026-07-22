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
        private Creature _creatureTarget;
        private ColonyFacility _facilityTarget;
        [SerializeField] private bool _heavy;
        public Vector2 Position => transform.position;
        public bool Alive => _health > 0f;
        public bool IsHeavy => _heavy;
        public bool IsTargeting(ColonyFacility facility)
            => Alive && facility != null && _facilityTarget == facility;

        public void Init(SimulationManager sim)
        {
            _sim = sim;
            _health = _heavy ? sim.Config.heavyEnemyMaxHealth : sim.Config.enemyMaxHealth;
        }

        private void Update()
        {
            if (_sim == null || !Alive) return;
            SelectNearestTarget();
            Vector2 destination = _creatureTarget != null ? _creatureTarget.Position
                : _facilityTarget != null ? _facilityTarget.Center : new Vector2(-8f, 0f);
            Vector2 delta = destination - Position;
            float reach = _facilityTarget != null ? Mathf.Max(.7f, _facilityTarget.Radius * .72f) : .7f;
            if (delta.sqrMagnitude > reach * reach)
            {
                float speed = _heavy ? .48f : .72f;
                transform.position = _sim.ClampWorld(Position + delta.normalized * speed * Time.deltaTime);
                GetComponent<SpriteRenderer>().flipX = delta.x < 0f;
            }
            else if (_creatureTarget != null || _facilityTarget != null)
            {
                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0f)
                {
                    _attackTimer = _sim.Config.attackInterval;
                    if (_creatureTarget != null)
                    {
                        float damage = _heavy ? _sim.Config.heavyEnemyDamage : _sim.Config.enemyDamage;
                        _creatureTarget.TakeCombatDamage(damage, delta.normalized);
                    }
                    else
                    {
                        int min = _heavy ? _sim.Config.heavyEnemyFacilityDamageMin
                            : _sim.Config.enemyFacilityDamageMin;
                        int max = _heavy ? _sim.Config.heavyEnemyFacilityDamageMax
                            : _sim.Config.enemyFacilityDamageMax;
                        _facilityTarget.TakeDamage(Random.Range(min, max + 1));
                    }
                }
            }
        }

        private void SelectNearestTarget()
        {
            var creature = _sim.ClosestCreature(Position);
            var facility = _sim.ClosestFacility(Position);
            float creatureDistance = creature != null ? (creature.Position - Position).sqrMagnitude : float.MaxValue;
            float facilityDistance = facility != null
                ? Mathf.Max(0f, (facility.Center - Position).magnitude - facility.Radius)
                : float.MaxValue;
            facilityDistance *= facilityDistance;
            _creatureTarget = creatureDistance <= facilityDistance ? creature : null;
            _facilityTarget = _creatureTarget == null ? facility : null;
        }

        public void TakeDamage(float amount)
        {
            if (!Alive) return;
            _health -= amount;
            if (_health > 0f) return;
            _sim?.SpawnEnemyPart(Position, _heavy);
            _sim?.RemoveEnemy(this);
            Destroy(gameObject);
        }
    }
}
