using System;
using UnityEngine;

namespace TCC.Core
{
    /// <summary>
    /// Global publish/subscribe hub. This is the seam that keeps managers and
    /// gameplay decoupled: the simulation raises facts ("an egg was collected"),
    /// and whoever cares (economy, audio, UI) subscribes. Nobody reaches across
    /// systems by direct reference.
    ///
    /// Keep payloads primitive/value-ish so listeners never need to know the
    /// concrete gameplay types.
    /// </summary>
    public static class GameEvents
    {
        // ---- Flow ---------------------------------------------------------
        public static event Action<GameState> GameStateChanged;
        public static void RaiseGameStateChanged(GameState s) => GameStateChanged?.Invoke(s);

        // ---- Localization -------------------------------------------------
        public static event Action<Language> LanguageChanged;
        public static void RaiseLanguageChanged(Language l) => LanguageChanged?.Invoke(l);

        // ---- Economy ------------------------------------------------------
        public static event Action<int> MoneyChanged;             // new balance
        public static void RaiseMoneyChanged(int balance) => MoneyChanged?.Invoke(balance);

        /// <summary>Someone requests to spend money. bool result is delivered via callback.</summary>
        public static event Action<int, Action<bool>> SpendRequested;
        public static void RaiseSpendRequested(int amount, Action<bool> result)
            => SpendRequested?.Invoke(amount, result);

        /// <summary>Money earned from a passive/ambient source (drip, labor). Payload = coins.</summary>
        public static event Action<int> MoneyEarned;
        public static void RaiseMoneyEarned(int amount) => MoneyEarned?.Invoke(amount);

        public static event Action<int> FoodChanged;
        public static void RaiseFoodChanged(int amount) => FoodChanged?.Invoke(amount);

        public static event Action InventoryChanged;
        public static void RaiseInventoryChanged() => InventoryChanged?.Invoke();

        // ---- Simulation facts (drive audio/UI/economy) --------------------
        public static event Action<Vector2> CreatureBorn;
        public static void RaiseCreatureBorn(Vector2 pos) => CreatureBorn?.Invoke(pos);

        public static event Action<Vector2> CreatureDied;
        public static void RaiseCreatureDied(Vector2 pos) => CreatureDied?.Invoke(pos);

        public static event Action<Vector2> EggLaid;
        public static void RaiseEggLaid(Vector2 pos) => EggLaid?.Invoke(pos);

        /// <summary>An egg was dragged to the coin display and sold. Payload = coin value.</summary>
        public static event Action<int, Vector2> EggCollected;
        public static void RaiseEggCollected(int value, Vector2 pos) => EggCollected?.Invoke(value, pos);

        // ---- Population snapshot (UI) -------------------------------------
        public static event Action<int, int> PopulationChanged; // infants, adults
        public static void RaisePopulationChanged(int infants, int adults)
            => PopulationChanged?.Invoke(infants, adults);
    }
}
