using System.Collections.Generic;
using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Managers;
using TCC.UI;

namespace TCC.Gameplay
{
    public enum FacilityType { Factory, Barracks, Hospital, Academy }

    /// <summary>Buildable, upgradeable world-space colony facility.</summary>
    public class ColonyFacility : MonoBehaviour
    {
        private sealed class Slot
        {
            public Creature creature;
            public float timer;
        }

        [SerializeField] private FacilityType _type;
        [SerializeField] private float _radius = 1.25f;
        [SerializeField, Range(0, 3)] private int _level;
        [SerializeField] private SpriteRenderer _ring;
        [SerializeField] private SpriteRenderer _core;
        [SerializeField] private SpriteRenderer _progressBack;
        [SerializeField] private SpriteRenderer _progressFill;

        private readonly List<Slot> _slots = new List<Slot>(10);
        private float _academyTimer = -1f;

        public FacilityType Type => _type;
        public bool IsBuilt => _level > 0;
        public int Level => _level;
        public float Radius => _radius;
        public Vector2 Center => transform.position;
        public int Capacity => _type == FacilityType.Academy
            ? new[] { 0, 2, 4, 6 }[_level]
            : new[] { 0, 3, 5, 10 }[_level];

        public void Configure(FacilityType type, float radius, SpriteRenderer ring,
            SpriteRenderer core, SpriteRenderer progressBack, SpriteRenderer progressFill)
        {
            _type = type;
            _radius = radius;
            _ring = ring;
            _core = core;
            _progressBack = progressBack;
            _progressFill = progressFill;
            RefreshVisual();
        }

        public bool Contains(Vector2 point) => (point - Center).sqrMagnitude <= _radius * _radius;

        public bool TryAssign(Creature creature)
        {
            Cleanup();
            bool eligible = creature != null && (_type == FacilityType.Hospital
                ? creature.IsInfected || creature.IsSoldier
                : creature.IsFreeAdult);
            if (!IsBuilt || !eligible)
            {
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastAdultOnly);
                return false;
            }
            if (_slots.Count >= Capacity)
            {
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastFacilityFull);
                return false;
            }

            CreatureRole role = _type == FacilityType.Factory ? CreatureRole.FactoryWorker
                : _type == FacilityType.Barracks ? CreatureRole.BarracksTrainee
                : _type == FacilityType.Hospital ? CreatureRole.HospitalPatient
                : CreatureRole.AcademyWorker;
            if (_type == FacilityType.Hospital && !creature.IsInfected && !creature.IsSoldier)
                return false;

            _slots.Add(new Slot { creature = creature });
            creature.AssignTo(this, role);
            creature.transform.position = Center + Random.insideUnitCircle * (_radius * .42f);
            if (_type == FacilityType.Barracks)
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastTrainingStarted);
            return true;
        }

        public bool TryRelease(Creature creature)
        {
            if (_type == FacilityType.Factory || creature == null) return false;
            int index = _slots.FindIndex(s => s.creature == creature);
            if (index < 0) return false;
            _slots.RemoveAt(index);
            creature.ReleaseFromFacility();
            return true;
        }

        private void OnMouseDown()
        {
            if (GameManager.Exists && GameManager.Instance.State != GameState.Playing) return;
            if (!IsBuilt) { TryBuild(); return; }
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                TryUpgrade();
                return;
            }
            if (_type == FacilityType.Academy) TryStartDoctorBatch();
        }

        private void TryBuild()
        {
            var eco = EconomyManager.Exists ? EconomyManager.Instance : null;
            if (eco == null || eco.Config == null) return;
            int cost = _type == FacilityType.Factory ? eco.Config.factoryBuildCost
                : _type == FacilityType.Barracks ? eco.Config.barracksBuildCost
                : _type == FacilityType.Hospital ? eco.Config.hospitalBuildCost
                : eco.Config.academyBuildCost;
            if (!eco.TrySpend(cost)) { ToastView.Instance?.Key(LocalizationTable.Keys.ToastInsufficientFunds); return; }
            _level = 1;
            RefreshVisual();
            ToastView.Instance?.Key(LocalizationTable.Keys.ToastBuilt);
        }

        private void TryUpgrade()
        {
            if (_level >= 3) return;
            int cost;
            if (_type == FacilityType.Factory) cost = _level == 1 ? 600 : 1000;
            else if (_type == FacilityType.Academy) cost = _level == 1 ? 2000 : 3000;
            else cost = _level == 1 ? 300 : 500;
            if (!EconomyManager.Exists || !EconomyManager.Instance.TrySpend(cost))
            {
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastInsufficientFunds);
                return;
            }
            _level++;
            RefreshVisual();
            ToastView.Instance?.Key(LocalizationTable.Keys.ToastUpgraded);
        }

        private void TryStartDoctorBatch()
        {
            if (_academyTimer >= 0f) return;
            Cleanup();
            var eco = EconomyManager.Exists ? EconomyManager.Instance : null;
            int batch = Capacity;
            int coinCost = eco != null && eco.Config != null ? eco.Config.doctorTrainingCoins * batch : 0;
            int foodCost = eco != null && eco.Config != null ? eco.Config.doctorTrainingFood * batch : 0;
            if (_slots.Count == 0 || eco == null || eco.Config == null ||
                !eco.CanAfford(coinCost) || eco.Food < foodCost)
            {
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastNeedAcademyWorker);
                return;
            }
            eco.TrySpend(coinCost);
            eco.TryConsumeFood(foodCost);
            _academyTimer = 0f;
            ToastView.Instance?.Key(LocalizationTable.Keys.ToastDoctorStarted);
        }

        private void Update()
        {
            if (!IsBuilt || !SimulationManager.Exists) { SetProgress(0f, false); return; }
            Cleanup();
            var cfg = SimulationManager.Instance.Config;
            float bestProgress = 0f;
            bool progressVisible = false;

            for (int i = _slots.Count - 1; i >= 0; i--)
            {
                var slot = _slots[i];
                if (slot.creature == null) { _slots.RemoveAt(i); continue; }
                if (_type == FacilityType.Factory)
                {
                    float efficiency = slot.creature.IsCritical ? .5f : 1f;
                    slot.timer += Time.deltaTime * efficiency;
                    if (slot.timer >= cfg.factoryPartInterval)
                    {
                        slot.timer -= cfg.factoryPartInterval;
                        SimulationManager.Instance.SpawnMetalPart(Center + new Vector2(_radius + .35f, Random.Range(-.7f, .7f)));
                    }
                }
                else if (_type == FacilityType.Barracks)
                {
                    slot.timer += Time.deltaTime;
                    progressVisible = true;
                    bestProgress = Mathf.Max(bestProgress, slot.timer / cfg.soldierTrainingSeconds);
                    if (slot.timer >= cfg.soldierTrainingSeconds)
                    {
                        var creature = slot.creature;
                        _slots.RemoveAt(i);
                        creature.CompleteFacilitySoldierTraining();
                        creature.transform.position = Center + Vector2.right * (_radius + .4f);
                        ToastView.Instance?.Key(LocalizationTable.Keys.ToastSoldierReady);
                    }
                }
                else if (_type == FacilityType.Hospital)
                {
                    slot.creature.HealCombat(Time.deltaTime * 8f);
                    if (slot.creature.IsInfected)
                    {
                        slot.timer += Time.deltaTime;
                        progressVisible = true;
                        bestProgress = Mathf.Max(bestProgress, slot.timer / cfg.hospitalTreatmentSeconds);
                        if (slot.timer >= cfg.hospitalTreatmentSeconds)
                        {
                            slot.creature.Cure();
                            slot.timer = 0f;
                            ToastView.Instance?.Key(LocalizationTable.Keys.ToastCured);
                        }
                    }
                }
            }

            if (_type == FacilityType.Academy && _academyTimer >= 0f)
            {
                _academyTimer += Time.deltaTime;
                progressVisible = true;
                bestProgress = _academyTimer / cfg.academyTrainingSeconds;
                if (_academyTimer >= cfg.academyTrainingSeconds)
                {
                    _academyTimer = -1f;
                    for (int i = 0; i < Capacity; i++)
                        SimulationManager.Instance.SpawnDoctor(Center + Random.insideUnitCircle * .45f, this);
                    ToastView.Instance?.Key(LocalizationTable.Keys.ToastDoctorReady);
                }
            }
            SetProgress(bestProgress, progressVisible);
        }

        private void SetProgress(float progress, bool visible)
        {
            if (_progressBack == null || _progressFill == null) return;
            _progressBack.enabled = visible;
            _progressFill.enabled = visible;
            if (!visible) return;
            progress = Mathf.Clamp01(progress);
            _progressFill.transform.localScale = new Vector3(Mathf.Max(.01f, progress), .55f, 1f);
            _progressFill.transform.localPosition = new Vector3(-.5f + progress * .5f, 0f, -.01f);
        }

        private void Cleanup() => _slots.RemoveAll(s => s.creature == null);

        private void RefreshVisual()
        {
            if (_ring == null) _ring = GetComponent<SpriteRenderer>();
            float alpha = IsBuilt ? .72f : .2f;
            Color tint = _type == FacilityType.Factory ? new Color(.78f, .47f, .2f, alpha)
                : _type == FacilityType.Barracks ? new Color(.24f, .64f, .68f, alpha)
                : _type == FacilityType.Hospital ? new Color(.32f, .72f, .55f, alpha)
                : new Color(.7f, .72f, .52f, alpha);
            if (_ring != null) _ring.color = tint;
            if (_core != null)
            {
                _core.color = new Color(tint.r, tint.g, tint.b, IsBuilt ? .82f : .12f);
                _core.transform.localScale = Vector3.one * (.18f + _level * .1f);
            }
        }
    }
}
