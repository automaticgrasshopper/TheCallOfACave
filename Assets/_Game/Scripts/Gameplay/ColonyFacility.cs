using System.Collections.Generic;
using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Managers;
using TCC.UI;
using TMPro;

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
        [SerializeField] private SpriteRenderer _reservedFootprint;
        [SerializeField] private SpriteRenderer _level2Decor;
        [SerializeField] private SpriteRenderer _level3Decor;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private float _reservedHalfExtent = 2.2f;
        [SerializeField] private float _maxVisualDiameter = 3.8f;

        private readonly List<Slot> _slots = new List<Slot>(10);
        private float _academyTimer = -1f;
        private bool _placementPreview;
        private bool _placementValid;

        public FacilityType Type => _type;
        public bool IsBuilt => _level > 0;
        public int Level => _level;
        public float Radius => _radius;
        public float ReservedHalfExtent => _reservedHalfExtent;
        public bool IsPlacementPreview => _placementPreview;
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

        public void ConfigurePresentation(SpriteRenderer footprint, TMP_Text label,
            SpriteRenderer level2Decor = null, SpriteRenderer level3Decor = null,
            float reservedHalfExtent = 2.2f, float maxVisualDiameter = 3.8f)
        {
            _reservedFootprint = footprint;
            _label = label;
            _level2Decor = level2Decor;
            _level3Decor = level3Decor;
            _reservedHalfExtent = reservedHalfExtent;
            _maxVisualDiameter = maxVisualDiameter;
            RefreshVisual();
        }

        public void BeginPlacementPreview()
        {
            _placementPreview = true;
            _placementValid = false;
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = false;
            RefreshVisual();
        }

        public void SetPlacementValidity(bool valid)
        {
            _placementValid = valid;
            RefreshVisual();
        }

        public void CommitPlacement()
        {
            _placementPreview = false;
            _level = 1;
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = true;
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
            creature.transform.position = Center + Random.insideUnitCircle * (_radius * .58f);
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
            if (_placementPreview) return;
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
            var eco = EconomyManager.Exists ? EconomyManager.Instance : null;
            if (eco == null || eco.Config == null) return;
            int cost = _type == FacilityType.Factory
                ? (_level == 1 ? eco.Config.factoryUpgradeLevel2 : eco.Config.factoryUpgradeLevel3)
                : _type == FacilityType.Barracks
                    ? (_level == 1 ? eco.Config.barracksUpgradeLevel2 : eco.Config.barracksUpgradeLevel3)
                    : _type == FacilityType.Hospital
                        ? (_level == 1 ? eco.Config.hospitalUpgradeLevel2 : eco.Config.hospitalUpgradeLevel3)
                        : (_level == 1 ? eco.Config.academyUpgradeLevel2 : eco.Config.academyUpgradeLevel3);
            if (!eco.TrySpend(cost))
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
                    float interval = cfg.FactoryInterval(_level);
                    if (slot.timer >= interval)
                    {
                        slot.timer -= interval;
                        SimulationManager.Instance.StoreFactoryProduct(_level);
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
                    slot.creature.HealCombat(Time.deltaTime * cfg.hospitalCombatHealPerSecond);
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
            var cfg = SimulationManager.Exists ? SimulationManager.Instance.Config
                : FindObjectOfType<SimulationManager>()?.Config;
            int displayLevel = _placementPreview ? 1 : Mathf.Max(1, _level);
            _radius = cfg != null ? cfg.FacilityRadius(displayLevel)
                : displayLevel >= 3 ? 1.9f : displayLevel == 2 ? 1.5f : 1.15f;
            if (cfg != null) _reservedHalfExtent = cfg.buildingReservedHalfExtent;

            Color accent = _type == FacilityType.Factory ? new Color(.88f, .55f, .2f, 1f)
                : _type == FacilityType.Barracks ? new Color(.24f, .75f, .78f, 1f)
                : _type == FacilityType.Hospital ? new Color(.34f, .86f, .54f, 1f)
                : new Color(.86f, .75f, .35f, 1f);
            if (_ring != null)
            {
                float visualScale = (_radius * 2f) / Mathf.Max(.1f, _maxVisualDiameter);
                _ring.transform.localScale = Vector3.one * visualScale;
                _ring.color = _placementPreview
                    ? (_placementValid ? new Color(.62f, 1f, .76f, .84f) : new Color(1f, .38f, .3f, .78f))
                    : new Color(1f, 1f, 1f, IsBuilt ? 1f : .28f);
            }
            if (_core != null)
            {
                _core.enabled = IsBuilt || _placementPreview;
                _core.color = new Color(accent.r, accent.g, accent.b,
                    _placementPreview ? .42f : .12f + displayLevel * .08f);
                _core.transform.localScale = Vector3.one * _radius;
            }
            if (_reservedFootprint != null)
            {
                _reservedFootprint.enabled = _placementPreview;
                _reservedFootprint.transform.localScale = new Vector3(
                    _reservedHalfExtent * 2f, _reservedHalfExtent * 2f, 1f);
                _reservedFootprint.color = _placementValid
                    ? new Color(.15f, .82f, .55f, .12f) : new Color(1f, .18f, .12f, .16f);
            }
            if (_level2Decor != null)
            {
                _level2Decor.enabled = !_placementPreview && IsBuilt && displayLevel >= 2;
                _level2Decor.transform.localScale = Vector3.one * _radius;
                _level2Decor.color = new Color(accent.r, accent.g, accent.b, .92f);
            }
            if (_level3Decor != null)
            {
                _level3Decor.enabled = !_placementPreview && IsBuilt && displayLevel >= 3;
                _level3Decor.transform.localScale = Vector3.one * _radius;
                _level3Decor.color = new Color(1f, .76f, .27f, .96f);
            }
            if (_label != null)
                _label.transform.localPosition = new Vector3(0f, _radius + .34f, 0f);
            if (_progressBack != null)
                _progressBack.transform.localPosition = new Vector3(0f, _radius + .14f, 0f);
            var collider = GetComponent<CircleCollider2D>();
            if (collider != null) collider.radius = _radius;
        }
    }
}
