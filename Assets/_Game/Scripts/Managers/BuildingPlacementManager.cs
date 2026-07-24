using System;
using System.Collections.Generic;
using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Gameplay;
using TCC.Persistence;
using TCC.UI;
using TMPro;

namespace TCC.Managers
{
    /// <summary>Grid-based RTS placement with a linear facility unlock chain.</summary>
    public class BuildingPlacementManager : Singleton<BuildingPlacementManager>
    {
        [SerializeField] private ColonyFacility _factoryPrefab;
        [SerializeField] private ColonyFacility _barracksPrefab;
        [SerializeField] private ColonyFacility _hospitalPrefab;
        [SerializeField] private ColonyFacility _academyPrefab;
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private GameObject _gridOverlay;

        private readonly List<ColonyFacility> _placed = new List<ColonyFacility>(8);
        private ColonyFacility _preview;
        private FacilityType _previewType;
        private bool _previewValid;

        public event Action AvailabilityChanged;
        public bool IsPlacing => _preview != null;
        public int BuiltFacilityCount => _placed.FindAll(facility =>
            facility != null && facility.IsBuilt).Count;

        private void Start()
        {
            _placed.Clear();
            foreach (var facility in FindObjectsOfType<ColonyFacility>(true))
                if (facility != null && !facility.IsPlacementPreview) _placed.Add(facility);
            AlignGridOverlay();
            EnsureNurseryLabelReadable();
            SetGridVisible(false);
            AvailabilityChanged?.Invoke();
        }

        private void Update()
        {
            if (_preview == null) return;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) CancelPlacement();
        }

        public bool CanBuild(FacilityType type)
        {
            if (!IsUnlocked(type) || PrefabFor(type) == null) return false;
            _placed.RemoveAll(facility => facility == null || !facility.IsBuilt);
            foreach (var facility in _placed)
                if (facility != null && facility.Type == type) return false;
            return true;
        }

        public bool IsUnlocked(FacilityType type)
        {
            if (type == FacilityType.Factory) return true;
            if (type == FacilityType.Barracks) return HasBuilt(FacilityType.Factory);
            if (type == FacilityType.Hospital) return HasBuilt(FacilityType.Barracks);
            return HasBuilt(FacilityType.Hospital);
        }

        private bool HasBuilt(FacilityType type)
        {
            foreach (var facility in _placed)
                if (facility != null && facility.Type == type && facility.IsBuilt) return true;
            return false;
        }

        public int BuildCost(FacilityType type)
        {
            var cfg = EconomyManager.Exists ? EconomyManager.Instance.Config : null;
            if (cfg == null) return 0;
            return type == FacilityType.Barracks ? cfg.barracksBuildCost
                : type == FacilityType.Hospital ? cfg.hospitalBuildCost
                : type == FacilityType.Academy ? cfg.academyBuildCost : cfg.factoryBuildCost;
        }

        public bool BeginPlacement(FacilityType type, Vector2 screenPosition)
        {
            if (_preview != null || !CanBuild(type) || !EconomyManager.Exists ||
                !EconomyManager.Instance.CanAfford(BuildCost(type)) ||
                (GameManager.Exists && GameManager.Instance.State != GameState.Playing))
                return false;

            var prefab = PrefabFor(type);
            _preview = Instantiate(prefab, Vector3.zero, Quaternion.identity, _worldRoot);
            _previewType = type;
            _preview.BeginPlacementPreview();
            SetGridVisible(true);
            UpdatePlacement(screenPosition);
            return true;
        }

        public void UpdatePlacement(Vector2 screenPosition)
        {
            if (_preview == null || Camera.main == null || !SimulationManager.Exists) return;
            Vector3 screen = new Vector3(screenPosition.x, screenPosition.y, -Camera.main.transform.position.z);
            Vector2 world = Camera.main.ScreenToWorldPoint(screen);
            float grid = Mathf.Max(.1f, SimulationManager.Instance.Config.buildingGridSize);
            Vector2 snapped = new Vector2(Mathf.Round(world.x / grid) * grid,
                Mathf.Round(world.y / grid) * grid);
            _preview.transform.position = snapped;
            _previewValid = IsValidPlacement(snapped);
            _preview.SetPlacementValidity(_previewValid);
        }

        public bool FinishPlacement(Vector2 screenPosition)
        {
            if (_preview == null) return false;
            UpdatePlacement(screenPosition);
            if (!_previewValid)
            {
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastBuildBlocked);
                CancelPlacement();
                return false;
            }
            int cost = BuildCost(_previewType);
            if (!EconomyManager.Exists || !EconomyManager.Instance.TrySpend(cost))
            {
                ToastView.Instance?.Key(LocalizationTable.Keys.ToastInsufficientFunds);
                CancelPlacement();
                return false;
            }

            var built = _preview;
            _preview = null;
            built.CommitPlacement();
            _placed.Add(built);
            if (SimulationManager.Exists) SimulationManager.Instance.RegisterFacility(built);
            SetGridVisible(false);
            ToastView.Instance?.Key(LocalizationTable.Keys.ToastBuilt);
            AvailabilityChanged?.Invoke();
            GameEvents.RaiseSaveRequested("facility-built");
            return true;
        }

        public Dictionary<string, ColonyFacility> RestoreFacilities(
            IReadOnlyList<FacilitySnapshot> snapshots)
        {
            foreach (ColonyFacility facility in _placed)
            {
                if (facility == null) continue;
                facility.gameObject.SetActive(false);
                Destroy(facility.gameObject);
            }
            _placed.Clear();

            var restored = new Dictionary<string, ColonyFacility>(StringComparer.Ordinal);
            if (snapshots == null) return restored;

            foreach (FacilitySnapshot snapshot in snapshots)
            {
                ColonyFacility prefab = PrefabFor(snapshot.facilityType);
                if (prefab == null)
                    throw new InvalidOperationException(
                        $"Missing facility prefab for {snapshot.facilityType}.");

                ColonyFacility facility = Instantiate(
                    prefab,
                    snapshot.position.ToVector2(),
                    Quaternion.identity,
                    _worldRoot);
                facility.RestoreSnapshot(snapshot);
                _placed.Add(facility);
                restored.Add(snapshot.id, facility);
                if (SimulationManager.Exists)
                    SimulationManager.Instance.RegisterFacility(facility);
            }

            SetGridVisible(false);
            AvailabilityChanged?.Invoke();
            return restored;
        }

        public void CancelPlacement()
        {
            if (_preview != null) Destroy(_preview.gameObject);
            _preview = null;
            SetGridVisible(false);
        }

        public void RemoveFacility(ColonyFacility facility)
        {
            if (facility == null) return;
            bool removed = _placed.Remove(facility);
            if (removed) AvailabilityChanged?.Invoke();
        }

        private bool IsValidPlacement(Vector2 center)
        {
            var cfg = SimulationManager.Instance.Config;
            if (center.x < cfg.buildingAreaMin.x || center.y < cfg.buildingAreaMin.y ||
                center.x > cfg.buildingAreaMax.x || center.y > cfg.buildingAreaMax.y)
                return false;

            // Only the structure that exists at placement time blocks the open
            // nursery. The larger level-3 reservation is still used between
            // facilities so future upgrades can never collide.
            float nurseryHalf = Mathf.Max(.1f, cfg.facilityLevel1Radius);
            Vector2 nursery = SimulationManager.Instance.BirthCenterPosition;
            float closestX = Mathf.Clamp(nursery.x, center.x - nurseryHalf, center.x + nurseryHalf);
            float closestY = Mathf.Clamp(nursery.y, center.y - nurseryHalf, center.y + nurseryHalf);
            if ((new Vector2(closestX, closestY) - nursery).sqrMagnitude <
                Mathf.Pow(SimulationManager.Instance.BirthRadius + .35f, 2f)) return false;

            float half = cfg.buildingReservedHalfExtent;
            _placed.RemoveAll(facility => facility == null || !facility.IsBuilt);
            foreach (var facility in _placed)
            {
                if (facility == null || facility == _preview) continue;
                float combined = half + facility.ReservedHalfExtent;
                Vector2 delta = (Vector2)facility.transform.position - center;
                if (Mathf.Abs(delta.x) < combined && Mathf.Abs(delta.y) < combined) return false;
            }
            return true;
        }

        private ColonyFacility PrefabFor(FacilityType type)
            => type == FacilityType.Factory ? _factoryPrefab
                : type == FacilityType.Barracks ? _barracksPrefab
                : type == FacilityType.Hospital ? _hospitalPrefab
                : type == FacilityType.Academy ? _academyPrefab : null;

        private void SetGridVisible(bool visible)
        {
            if (_gridOverlay != null) _gridOverlay.SetActive(visible);
        }

        private void AlignGridOverlay()
        {
            if (_gridOverlay == null || !SimulationManager.Exists) return;
            var renderer = _gridOverlay.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null) return;

            var cfg = SimulationManager.Instance.Config;
            Vector2 size = cfg.buildingAreaMax - cfg.buildingAreaMin;
            Vector2 spriteSize = renderer.sprite.bounds.size;
            _gridOverlay.transform.position = new Vector3(
                (cfg.buildingAreaMin.x + cfg.buildingAreaMax.x) * .5f,
                (cfg.buildingAreaMin.y + cfg.buildingAreaMax.y) * .5f,
                _gridOverlay.transform.position.z);
            _gridOverlay.transform.localScale = new Vector3(
                size.x / Mathf.Max(.01f, spriteSize.x),
                size.y / Mathf.Max(.01f, spriteSize.y),
                1f);
        }

        private static void EnsureNurseryLabelReadable()
        {
            TMP_Text label = GameObject.Find("Nursery Label")?.GetComponent<TMP_Text>();
            if (label == null) return;
            label.transform.localScale = Vector3.one * .09f;
            label.fontSize = 32f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 32f;
            label.raycastTarget = false;
        }
    }
}
