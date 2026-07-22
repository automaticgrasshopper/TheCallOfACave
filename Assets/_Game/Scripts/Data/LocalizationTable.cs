using System;
using System.Collections.Generic;
using UnityEngine;
using TCC.Core;

namespace TCC.Data
{
    /// <summary>
    /// The single source of truth for on-screen text. Every localizable string is
    /// a key with one value per language. Built as an asset so translators (and
    /// the user) edit values without touching code, and so new languages are just
    /// a new column on <see cref="Entry"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "TCC/Localization Table", fileName = "LocalizationTable")]
    public class LocalizationTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string key;
            [TextArea] public string zh;
            [TextArea] public string en;

            public string Get(Language lang)
            {
                switch (lang)
                {
                    case Language.ChineseSimplified: return string.IsNullOrEmpty(zh) ? en : zh;
                    case Language.English: return string.IsNullOrEmpty(en) ? zh : en;
                    // Newly added non-Chinese locales must never leak Chinese while
                    // their column is still being populated.
                    default: return en;
                }
            }
        }

        public List<Entry> entries = new List<Entry>();

        private Dictionary<string, Entry> _lookup;

        public void BuildLookup()
        {
            _lookup = new Dictionary<string, Entry>(entries.Count);
            foreach (var e in entries)
            {
                if (!string.IsNullOrEmpty(e.key) && !_lookup.ContainsKey(e.key))
                    _lookup.Add(e.key, e);
            }
        }

        public string Get(string key, Language lang)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(key, out var e) ? e.Get(lang) : $"#{key}";
        }

        /// <summary>Well-known keys, so call sites reference constants not literals.</summary>
        public static class Keys
        {
            public const string GameTitle = "game.title";
            public const string Money = "hud.money";
            public const string Population = "hud.population";
            public const string Food = "hud.food";
            public const string BuyFood = "btn.buy_food";
            public const string HudBuy = "hud.buy";
            public const string HudBuyFood = "hud.buy_food";
            public const string HudInventory = "hud.inventory";
            public const string HudBuild = "hud.build";
            public const string HudCommand = "hud.command";
            public const string HudTabBuild = "hud.tab_build";
            public const string HudTabResearch = "hud.tab_research";
            public const string HudTabEquipment = "hud.tab_equipment";
            public const string HudResearchReserved = "hud.research_reserved";
            public const string HudResearchDoctor = "hud.research_doctor";
            public const string HudResearchFuture = "hud.research_future";
            public const string HudEquipmentReserved = "hud.equipment_reserved";
            public const string HudEquipmentArmor = "hud.equipment_armor";
            public const string HudEquipmentTool = "hud.equipment_tool";
            public const string HudEquipmentCore = "hud.equipment_core";
            public const string BuildFactory = "build.factory";
            public const string BuildBarracks = "build.barracks";
            public const string BuildHospital = "build.hospital";
            public const string BuildAcademy = "build.academy";
            public const string BuildLocked = "build.locked";
            public const string LanguageToggle = "btn.language";
            public const string Pause = "btn.pause";
            public const string Resume = "btn.resume";
            public const string HintFeed = "hint.feed";
            public const string GameOver = "state.gameover";
            public const string GameOverTitle = "gameover.title";
            public const string GameOverBody = "gameover.body";
            public const string GameOverRestart = "gameover.restart";
            public const string GameOverMenu = "gameover.menu";

            // Live status notifications
            public const string ToastEggLaid = "toast.egg_laid";
            public const string ToastEggSold = "toast.egg_sold";
            public const string ToastBugBorn = "toast.bug_born";
            public const string ToastBugDied = "toast.bug_died";
            public const string ToastBugBought = "toast.bug_bought";
            public const string ToastInsufficientFunds = "toast.insufficient_funds";
            public const string ToastFoodBought = "toast.food_bought";
            public const string ToastFoodUsed = "toast.food_used";
            public const string ToastNoFood = "toast.no_food";
            public const string ToastBuilt = "toast.built";
            public const string ToastUpgraded = "toast.upgraded";
            public const string ToastFacilityFull = "toast.facility_full";
            public const string ToastAdultOnly = "toast.adult_only";
            public const string ToastPartReady = "toast.part_ready";
            public const string ToastCargoSold = "toast.cargo_sold";
            public const string ToastDragFood = "toast.drag_food";
            public const string ToastEquipmentReady = "toast.equipment_ready";
            public const string ToastEliteEquipped = "toast.elite_equipped";
            public const string ToastEliteRequired = "toast.elite_required";
            public const string ToastHeavyInvasion = "toast.heavy_invasion";
            public const string ToastDragToBuild = "toast.drag_to_build";
            public const string ToastBuildBlocked = "toast.build_blocked";
            public const string ToastBuildLocked = "toast.build_locked";
            public const string ToastInfected = "toast.infected";
            public const string ToastCured = "toast.cured";
            public const string ToastInvasion = "toast.invasion";
            public const string ToastDoctorStarted = "toast.doctor_started";
            public const string ToastDoctorReady = "toast.doctor_ready";
            public const string ToastNeedAcademyWorker = "toast.need_academy_worker";

            // World-space site labels
            public const string ZoneNursery = "zone.nursery";
            public const string ZoneFactory = "zone.factory";
            public const string ZoneBarracks = "zone.barracks";
            public const string ZoneHospital = "zone.hospital";
            public const string ZoneAcademy = "zone.academy";
            public const string ToastTrainingStarted = "toast.training_started";
            public const string ToastSoldierReady = "toast.soldier_ready";

            // Menu
            public const string MenuStart = "menu.start";
            public const string MenuRestart = "menu.restart";
            public const string MenuContinue = "menu.continue";
            public const string MenuSettings = "menu.settings";
            public const string MenuQuit = "menu.quit";

            // Settings
            public const string SettingsTitle = "settings.title";
            public const string SettingsFullscreen = "settings.fullscreen";
            public const string SettingsResolution = "settings.resolution";
            public const string SettingsMusic = "settings.music";
            public const string SettingsSfx = "settings.sfx";
            public const string SettingsLanguage = "settings.language";
            public const string SettingsBack = "settings.back";

            // Alert
            public const string AlertConfirm = "alert.confirm";
            public const string AlertCancel = "alert.cancel";
            public const string AlertQuitTitle = "alert.quit_title";
            public const string AlertQuitMsg = "alert.quit_msg";
        }
    }
}
