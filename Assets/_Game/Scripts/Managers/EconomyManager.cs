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
        public int EggSellValue => _config != null ? _config.eggSellValue : 0;
        public int BuyJuvenileCost => _config != null ? _config.buyJuvenileCost : 0;

        private float _passiveTimer;

        protected override void OnAwake()
        {
            Money = _config != null ? _config.startMoney : 0;
        }

        private void OnEnable()
        {
            GameEvents.SpendRequested += OnSpendRequested;
            GameEvents.EggCollected += OnEggCollected;
            GameEvents.MoneyEarned += OnMoneyEarned;
        }

        private void OnDisable()
        {
            GameEvents.SpendRequested -= OnSpendRequested;
            GameEvents.EggCollected -= OnEggCollected;
            GameEvents.MoneyEarned -= OnMoneyEarned;
        }

        private void Start()
        {
            // Broadcast the opening balance once every listener (UI) is subscribed.
            GameEvents.RaiseMoneyChanged(Money);
        }

        private void Update()
        {
            // Passive drip. deltaTime is 0 while paused/at menu (timeScale 0), so the
            // income naturally halts when the game isn't running.
            if (_config == null || _config.passiveIntervalSeconds <= 0f) return;
            _passiveTimer += Time.deltaTime;
            if (_passiveTimer >= _config.passiveIntervalSeconds)
            {
                _passiveTimer -= _config.passiveIntervalSeconds;
                Add(_config.passiveIncome);
            }
        }

        public bool CanAfford(int amount) => Money >= amount;

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

        private void OnEggCollected(int value, Vector2 _) => Add(value);
        private void OnMoneyEarned(int amount) => Add(amount);
    }
}
