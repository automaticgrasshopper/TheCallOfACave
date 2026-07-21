using UnityEngine;
using TCC.Core;
using TCC.Managers;

namespace TCC.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public class MedicalDoctor : MonoBehaviour
    {
        private SimulationManager _sim;
        private ColonyFacility _home;
        private ContaminationSource _target;
        private bool _dragging;
        private float _cleanTimer;
        private WorldStatusBars _progress;

        public void Init(SimulationManager sim, ColonyFacility home)
        {
            _sim = sim;
            _home = home;
            _progress = gameObject.AddComponent<WorldStatusBars>();
            _progress.Set(1f, 0f);
        }

        private void Update()
        {
            if (_dragging || _sim == null) return;
            if (_target == null) _target = _sim.ClosestContamination(transform.position);
            Vector2 destination = _target != null ? _target.Position : (_home != null ? _home.Center : (Vector2)transform.position);
            Vector2 delta = destination - (Vector2)transform.position;
            if (delta.sqrMagnitude > .48f * .48f)
            {
                transform.position = _sim.ClampWorld((Vector2)transform.position + delta.normalized * 1.05f * Time.deltaTime);
                GetComponent<SpriteRenderer>().flipX = delta.x < 0f;
                _cleanTimer = 0f;
            }
            else if (_target != null)
            {
                _cleanTimer += Time.deltaTime;
                _progress.Set(1f, _cleanTimer / _sim.Config.contaminationCleanSeconds);
                if (_cleanTimer >= _sim.Config.contaminationCleanSeconds)
                {
                    _target.Cleaned();
                    _target = null;
                    _cleanTimer = 0f;
                }
            }
        }

        private void OnMouseDown()
        {
            if (GameManager.Exists && GameManager.Instance.State != GameState.Playing) return;
            _dragging = true;
        }

        private void OnMouseDrag()
        {
            if (!_dragging || Camera.main == null) return;
            Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(p.x, p.y, 0f);
        }

        private void OnMouseUp()
        {
            _dragging = false;
            transform.position = _sim.ClampWorld(transform.position);
        }
    }
}
