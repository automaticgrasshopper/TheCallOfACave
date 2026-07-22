using System;
using System.Collections.Generic;
using UnityEngine;
using TCC.Core;
using TCC.Data;
using TCC.Gameplay;
using TCC.UI;

namespace TCC.Managers
{
    /// <summary>Grid-based RTS placement for the three player-built facilities.</summary>
    public class BuildingPlacementManager : Singleton<BuildingPlacementManager>
    {
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

        private void Start()
        {
            _placed.Clear();
            foreach (var facility in FindObjectsOfType<ColonyFacility>(true))
                if (facility != null && !facility.IsPlacementPreview) _placed.Add(facility);
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
            if (type == FacilityType.Factory || PrefabFor(type) == null) return false;
            foreach (var facility in _placed)
                if (facility != null && facility.Type == type) return false;
            return true;
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
            return true;
        }

        public void CancelPlacement()
        {
            if (_preview != null) Destroy(_preview.gameObject);
            _preview = null;
            SetGridVisible(false);
        }

        private bool IsValidPlacement(Vector2 center)
        {
            var cfg = SimulationManager.Instance.Config;
            float half = cfg.buildingReservedHalfExtent;
            if (center.x - half < cfg.buildingAreaMin.x || center.y - half < cfg.buildingAreaMin.y ||
                center.x + half > cfg.buildingAreaMax.x || center.y + half > cfg.buildingAreaMax.y)
                return false;

            Vector2 nursery = SimulationManager.Instance.BirthCenterPosition;
            float closestX = Mathf.Clamp(nursery.x, center.x - half, center.x + half);
            float closestY = Mathf.Clamp(nursery.y, center.y - half, center.y + half);
            if ((new Vector2(closestX, closestY) - nursery).sqrMagnitude <
                Mathf.Pow(SimulationManager.Instance.BirthRadius + .35f, 2f)) return false;

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
            => type == FacilityType.Barracks ? _barracksPrefab
                : type == FacilityType.Hospital ? _hospitalPrefab
                : type == FacilityType.Academy ? _academyPrefab : null;

        private void SetGridVisible(bool visible)
        {
            if (_gridOverlay != null) _gridOverlay.SetActive(visible);
        }
    }
}
