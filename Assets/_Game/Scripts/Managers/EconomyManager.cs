using UnityEngine;
using TCC.Core;
using TCC.Data;

namespace TCC.Managers
{
    /// <summary>
    /// Owns the coin balance. It never knows what money is spent on — it only
    /// answers spend requests and credits income (a passive drip it runs itself,
    /// labor payouts, and sold eggs), all delivered through <see cref="GameEvents"/>.
    /// This keeps the wallet decoupled from what earns or costs money.
    /// </summary>
    public class EconomyManager : Singleton<EconomyManager>
    {
        [SerializeField] private EconomyConfig _config;

        public int Money { get; private set; }
        public int Food { get; private set; }
        public int BuyJuvenileCost => _config != null ? _config.buyJuvenileCost : 0;
        public int BuyFoodCost => _config != null ? _config.buyFoodCost : 0;
        public EconomyConfig Config => _config;

        protected override void OnAwake()
        {
            Money = _config != null ? _config.startMoney : 0;
        }

        private void OnEnable()
        {
            GameEvents.SpendRequested += OnSpendRequested;
            GameEvents.MoneyEarned += OnMoneyEarned;
        }

        private void OnDisable()
        {
            GameEvents.SpendRequested -= OnSpendRequested;
            GameEvents.MoneyEarned -= OnMoneyEarned;
        }

        private void Start()
        {
            // Broadcast the opening balance once every listener (UI) is subscribed.
            GameEvents.RaiseMoneyChanged(Money);
            GameEvents.RaiseFoodChanged(Food);
        }

        public bool CanAfford(int amount) => Money >= amount;

        public void TryBuyFood()
        {
            Spend(_config != null ? _config.buyFoodCost : 0, ok =>
            {
                if (!ok) { TCC.UI.ToastView.Instance?.Key(LocalizationTable.Keys.ToastInsufficientFunds); return; }
                Food++;
                GameEvents.RaiseFoodChanged(Food);
                TCC.UI.ToastView.Instance?.Key(LocalizationTable.Keys.ToastFoodBought);
            });
        }

        public bool TryConsumeFood(int amount)
        {
            if (amount <= 0) return true;
            if (Food < amount) return false;
            Food -= amount;
            GameEvents.RaiseFoodChanged(Food);
            return true;
        }

        public bool TrySpend(int coins)
        {
            if (coins < 0 || Money < coins) return false;
            Money -= coins;
            GameEvents.RaiseMoneyChanged(Money);
            return true;
        }

        private void Spend(int amount, System.Action<bool> result) => OnSpendRequested(amount, result);

        private void Add(int amount)
        {
            if (amount <= 0) return;
            Money += amount;
            GameEvents.RaiseMoneyChanged(Money);
        }

        private void OnSpendRequested(int amount, System.Action<bool> result)
        {
            if (Money < amount)
            {
                result?.Invoke(false);
                return;
            }
            Money -= amount;
            GameEvents.RaiseMoneyChanged(Money);
            result?.Invoke(true);
        }

        private void OnMoneyEarned(int amount) => Add(amount);
    }
}
