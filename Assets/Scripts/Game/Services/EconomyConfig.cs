using System;
using System.Collections.Generic;
using BallSort.Core.Levels;
using BallSort.Core.Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace BallSort.Game.Services
{
    /// <summary>
    /// Every economy and booster number in one place, so product can tune them without code changes.
    /// See docs/screens.md for what each one means in play.
    /// </summary>
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Ball Sort/Economy Config")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [Header("New player")]
        [SerializeField, Min(0)] private int _startCoins = 1000;
        [SerializeField, Min(0)] private int _startUndo = 99;
        [SerializeField, Min(0)] private int _startShuffle = 99;
        [SerializeField, Min(0)] private int _startAddTube = 99;

        [Header("Lives")]
        [Tooltip("Most lives a player can hold. A new player starts full.")]
        [FormerlySerializedAs("_startLives")]
        [SerializeField, Min(1)] private int _maxLives = 5;
        [Tooltip("Minutes for one life to come back while below the maximum.")]
        [SerializeField, Min(0.1f)] private float _lifeRefillMinutes = 30f;

        [Header("Rewards and prices")]
        [Tooltip("Coins granted for winning a regular level.")]
        [FormerlySerializedAs("_winReward")]
        [SerializeField, Min(0)] private int _regularReward = 20;
        [Tooltip("Coins granted for winning a hard level.")]
        [SerializeField, Min(0)] private int _hardReward = 60;
        [Tooltip("Coins granted for winning a super hard level.")]
        [SerializeField, Min(0)] private int _superHardReward = 100;
        [Tooltip("Coins spent to add one tube when stuck.")]
        [SerializeField, Min(0)] private int _playOnCost = 900;

        [Header("Boosters")]
        [Tooltip("Slots in each tube added by the Add-tube booster or Play on.")]
        [SerializeField, Min(1)] private int _addedTubeCapacity = 4;

        [Header("Buying with coins")]
        [Tooltip("Coins for one purchase of Undo boosters (Buy popup, at 0 boosters).")]
        [SerializeField, Min(0)] private int _undoPrice = 300;
        [SerializeField, Min(0)] private int _shufflePrice = 300;
        [SerializeField, Min(0)] private int _addTubePrice = 900;
        [Tooltip("Boosters granted by one purchase.")]
        [SerializeField, Min(1)] private int _boostersPerPurchase = 1;
        [Tooltip("Coins to refill the lives to the maximum (Buy hearts, on Home).")]
        [SerializeField, Min(0)] private int _livesRefillPrice = 900;

        [Header("Shop")]
        [Tooltip("The Shop page's coin bundles, top left to bottom right. Prices are display text only.")]
        [SerializeField] private CoinBundle[] _coinBundles =
        {
            new CoinBundle(1000, "99,99 TL"),
            new CoinBundle(5000, "399,99 TL"),
            new CoinBundle(10000, "799,99 TL"),
            new CoinBundle(25000, "1499,99 TL"),
            new CoinBundle(50000, "2999,99 TL"),
            new CoinBundle(100000, "4999,99 TL"),
        };

        public int StartCoins => _startCoins;
        public int MaxLives => _maxLives;
        public TimeSpan LifeRefillInterval => TimeSpan.FromMinutes(_lifeRefillMinutes);
        public int StartUndo => _startUndo;
        public int StartShuffle => _startShuffle;
        public int StartAddTube => _startAddTube;
        public int PlayOnCost => _playOnCost;
        public int AddedTubeCapacity => _addedTubeCapacity;
        public int BoostersPerPurchase => _boostersPerPurchase;
        public int LivesRefillPrice => _livesRefillPrice;
        public IReadOnlyList<CoinBundle> CoinBundles => _coinBundles;

        /// <summary>The win reward of each tier.</summary>
        public TierRewards WinRewards => new TierRewards(_regularReward, _hardReward, _superHardReward);

        /// <summary>Coins one purchase of <paramref name="type"/> boosters costs.</summary>
        public int GetBoosterPrice(BoosterType type) =>
            type switch
            {
                BoosterType.Undo => _undoPrice,
                BoosterType.Shuffle => _shufflePrice,
                BoosterType.AddTube => _addTubePrice,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown booster type."),
            };

        /// <summary>A new player's starting values and the lives rules, for the player data.</summary>
        public PlayerRules CreatePlayerRules() =>
            new PlayerRules(_startCoins, _maxLives, LifeRefillInterval, _startUndo, _startShuffle, _startAddTube);

        /// <summary>Coins granted for winning a level of <paramref name="tier"/>.</summary>
        public int GetWinReward(LevelTier tier) => WinRewards.For(tier);

        private void OnValidate()
        {
            for (int i = 0; i < _coinBundles.Length; i++)
            {
                if (_coinBundles[i] == null || string.IsNullOrWhiteSpace(_coinBundles[i].PriceLabel))
                {
                    Debug.LogWarning($"{name}: coin bundle {i + 1} has no price label.", this);
                }
            }
        }
    }
}
