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
        private float _structureHealth;
        private bool _placementPreview;
        private bool _placementValid;
        private SpriteRenderer _healthBack;
        private SpriteRenderer _healthFill;
        private FacilityInfoPanel _infoPanel;
        private static Sprite _statusPixel;

        public FacilityType Type => _type;
        public bool IsBuilt => _level > 0;
        public int Level => _level;
        public float Radius => _radius;
        public float ReservedHalfExtent => _reservedHalfExtent;
        public bool IsPlacementPreview => _placementPreview;
        public Vector2 Center => transform.position;
        public bool CanBeAttacked => IsBuilt && !_placementPreview;
        public float StructureHealth => Mathf.Max(0f, _structureHealth);
        public float MaxStructureHealth
        {
            get
            {
                var cfg = SimulationManager.Exists ? SimulationManager.Instance.Config
                    : FindObjectOfType<SimulationManager>()?.Config;
                if (cfg != null) return cfg.FacilityMaxHealth(_level);
                return _level >= 3 ? 300f : _level == 2 ? 120f : _level == 1 ? 100f : 0f;
            }
        }
        public float StructureHealthNormalized => MaxStructureHealth > 0f
            ? Mathf.Clamp01(StructureHealth / MaxStructureHealth) : 0f;
        public string InfoText
        {
            get
            {
                if (!LocalizationManager.Exists) return string.Empty;
                var loc = LocalizationManager.Instance;
                string typeKey = _type == FacilityType.Factory ? LocalizationTable.Keys.ZoneFactory
                    : _type == FacilityType.Barracks ? LocalizationTable.Keys.ZoneBarracks
                    : _type == FacilityType.Hospital ? LocalizationTable.Keys.ZoneHospital
                    : LocalizationTable.Keys.ZoneAcademy;
                string format = loc.Get(LocalizationTable.Keys.FacilityInfo).Replace("\\n", "\n");
                return string.Format(format, loc.Get(typeKey), _level,
                    Mathf.CeilToInt(StructureHealth), Mathf.RoundToInt(MaxStructureHealth));
            }
        }
        public int Capacity => _type == FacilityType.Academy
            ? new[] { 0, 2, 4, 6 }[_level]
            : new[] { 0, 3, 5, 10 }[_level];

        private void Start()
        {
            if (IsBuilt) ResetStructureHealth();
            EnsureHealthBar();
            var panelPrefab = Resources.Load<FacilityInfoPanel>("UI/FacilityInfoPanel");
            if (panelPrefab != null)
                _infoPanel = Instantiate(panelPrefab, transform);
            else
            {
                var panelObject = new GameObject("Facility Info Panel", typeof(FacilityInfoPanel));
                panelObject.transform.SetParent(transform, false);
                _infoPanel = panelObject.GetComponent<FacilityInfoPanel>();
            }
            _infoPanel.Init(this);
            RefreshHealthBar();
        }

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
            ResetStructureHealth();
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
            creature.transform.position = Center + Random.insideUnitCircle * (_radius * .32f);
            if (_type == FacilityType.Barracks)
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastTrainingStarted);
            return true;
        }

        public Vector2 ClampToInterior(Vector2 point)
        {
            Vector2 delta = point - Center;
            float safeRadius = Mathf.Max(.2f, _radius * .48f);
            return delta.sqrMagnitude > safeRadius * safeRadius
                ? Center + delta.normalized * safeRadius
                : point;
        }

        public void RemoveOccupant(Creature creature)
        {
            if (creature != null) _slots.RemoveAll(s => s.creature == creature);
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

        private void OnMouseEnter()
        {
            if (!_placementPreview && IsBuilt) _infoPanel?.SetVisible(true);
        }

        private void OnMouseExit() => _infoPanel?.SetVisible(false);

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
            ResetStructureHealth();
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
            ResetStructureHealth();
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
            if (_structureHealth < MaxStructureHealth &&
                !SimulationManager.Instance.IsFacilityThreatened(this))
            {
                _structureHealth = Mathf.Min(MaxStructureHealth, _structureHealth +
                    MaxStructureHealth * cfg.facilityRegenPercentPerSecond * Time.deltaTime);
            }
            RefreshHealthBar();
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

        public void TakeDamage(float amount)
        {
            if (!CanBeAttacked || amount <= 0f) return;
            _structureHealth -= amount;
            RefreshHealthBar();
            if (_structureHealth > 0f) return;

            _level--;
            if (_level <= 0)
            {
                _level = 0;
                _infoPanel?.SetVisible(false);
                foreach (var slot in _slots)
                {
                    if (slot.creature == null) continue;
                    slot.creature.EvictAsFreeAdult();
                    slot.creature.transform.position = Center + Random.insideUnitCircle * (_radius + .35f);
                }
                _slots.Clear();
                _academyTimer = -1f;
            }
            else
            {
                while (_slots.Count > Capacity)
                {
                    int last = _slots.Count - 1;
                    var creature = _slots[last].creature;
                    _slots.RemoveAt(last);
                    if (creature != null)
                    {
                        creature.EvictAsFreeAdult();
                        creature.transform.position = Center + Vector2.down * (_radius + .35f);
                    }
                }
            }
            ResetStructureHealth();
            RefreshVisual();
        }

        private void ResetStructureHealth()
        {
            _structureHealth = MaxStructureHealth;
            RefreshHealthBar();
        }

        private void EnsureHealthBar()
        {
            if (_healthFill != null) return;
            _healthBack = MakeHealthBarPart("Facility Health Back",
                new Color(.015f, .025f, .025f, .96f), 27);
            _healthFill = MakeHealthBarPart("Facility Health Fill",
                new Color(.27f, .8f, .38f, 1f), 28);
        }

        private SpriteRenderer MakeHealthBarPart(string name, Color color, int order)
        {
            var child = transform.Find(name);
            var go = child != null ? child.gameObject : new GameObject(name);
            if (child == null) go.transform.SetParent(transform, false);
            var renderer = go.GetComponent<SpriteRenderer>();
            // UnityEngine.Object can be a destroyed "fake null" after a domain reload;
            // explicit Unity null comparison is required instead of ?? here.
            if (renderer == null) renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = StatusPixel;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void RefreshHealthBar()
        {
            EnsureHealthBar();
            bool visible = IsBuilt && !_placementPreview;
            _healthBack.enabled = visible;
            _healthFill.enabled = visible;
            if (!visible) return;

            float value = StructureHealthNormalized;
            float width = Mathf.Max(1.5f, _radius * 1.25f);
            float y = -_radius - .2f;
            _healthBack.transform.localPosition = new Vector3(0f, y, 0f);
            _healthBack.transform.localScale = new Vector3(width, .12f, 1f);
            _healthFill.transform.localPosition = new Vector3(-width * .5f + width * value * .5f, y, -.01f);
            _healthFill.transform.localScale = new Vector3(Mathf.Max(.001f, width * value), .075f, 1f);
            _healthFill.color = value < .25f ? new Color(.92f, .2f, .14f, 1f)
                : value < .55f ? new Color(.95f, .68f, .16f, 1f)
                : new Color(.27f, .8f, .38f, 1f);
        }

        private static Sprite StatusPixel
        {
            get
            {
                if (_statusPixel != null) return _statusPixel;
                var texture = Texture2D.whiteTexture;
                _statusPixel = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(.5f, .5f), texture.width);
                _statusPixel.name = "Runtime Facility Status Pixel";
                return _statusPixel;
            }
        }

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
            if (Application.isPlaying) RefreshHealthBar();
        }
    }
}
