using System.Collections.Generic;
using UnityEngine;
using TCC.Core;

namespace TCC.Gameplay
{
    /// <summary>
    /// The labor circle in the top-left. Prime adults dropped inside it are "parked"
    /// and earn coins for as long as they stay; every second each working prime pays
    /// out <see cref="_incomePerSec"/>. The zone only tracks membership and money —
    /// the creature owns its own parked/working state. Capacity is soft-capped.
    /// </summary>
    public class LaborZone : MonoBehaviour
    {
        [SerializeField] private float _radius = 1.7f;
        [SerializeField] private int _capacity = 10;
        [SerializeField] private int _incomePerSec = 2;

        private readonly List<Creature> _workers = new List<Creature>(16);
        private float _accrued; // fractional coins waiting to be paid out

        public float Radius => _radius;
        public Vector2 Center => transform.position;

        public void Configure(int incomePerSec, int capacity, float radius)
        {
            _incomePerSec = incomePerSec;
            _capacity = capacity;
            _radius = radius;
        }

        public bool Contains(Vector2 world)
            => (world - Center).sqrMagnitude <= _radius * _radius;

        /// <summary>Try to add a creature to the working set. False if the circle is full.</summary>
        public bool TryPark(Creature c)
        {
            _workers.RemoveAll(w => w == null);
            if (_workers.Contains(c)) return true;
            if (_workers.Count >= _capacity) return false;
            _workers.Add(c);
            return true;
        }

        public void Unpark(Creature c) => _workers.Remove(c);

        private void Update()
        {
            int working = 0;
            for (int i = _workers.Count - 1; i >= 0; i--)
            {
                var w = _workers[i];
                if (w == null) { _workers.RemoveAt(i); continue; }
                if (w.IsWorking) working++;
            }
            if (working == 0) return;

            _accrued += working * _incomePerSec * Time.deltaTime;
            if (_accrued < 1f) return;

            int coins = Mathf.FloorToInt(_accrued);
            _accrued -= coins;
            GameEvents.RaiseMoneyEarned(coins);
        }
    }
}
