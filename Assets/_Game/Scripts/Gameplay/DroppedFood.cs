using UnityEngine;
using TCC.Managers;

namespace TCC.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public class DroppedFood : MonoBehaviour
    {
        private SimulationManager _sim;
        private ColonyFacility _facility;
        private bool _consumed;

        public Vector2 Position => transform.position;
        public ColonyFacility Facility => _facility != null && _facility.IsBuilt ? _facility : null;
        public bool Available => !_consumed;

        public void Init(SimulationManager sim, ColonyFacility facility)
        {
            _sim = sim;
            _facility = facility;
        }

        public bool CanBeClaimedBy(Creature creature)
        {
            if (_consumed || creature == null || !creature.CanEat) return false;
            return Facility != null
                ? creature.Facility == Facility
                : creature.Facility == null;
        }

        public bool TryConsume(Creature creature)
        {
            if (!CanBeClaimedBy(creature) || !creature.EatDroppedFood()) return false;
            _consumed = true;
            _sim?.RemoveFood(this);
            Destroy(gameObject);
            return true;
        }
    }
}
