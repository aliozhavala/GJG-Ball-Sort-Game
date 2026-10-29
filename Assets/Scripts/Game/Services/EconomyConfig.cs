using System;
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
        [SerializeField, Min(0)] private int _hardReward = 40;
        [Tooltip("Coins granted for winning a super hard level.")]
        [SerializeField, Min(0)] private int _superHardReward = 60;
        [Tooltip("Coins spent to add one tube when stuck.")]
        [SerializeField, Min(0)] private int _playOnCost = 900;

        [Header("Boosters")]
        [Tooltip("Slots in each tube added by the Add-tube booster or Play on.")]
        [SerializeField, Min(1)] private int _addedTubeCapacity = 4;

        public int StartCoins => _startCoins;
        public int MaxLives => _maxLives;
        public TimeSpan LifeRefillInterval => TimeSpan.FromMinutes(_lifeRefillMinutes);
        public int StartUndo => _startUndo;
        public int StartShuffle => _startShuffle;
        public int StartAddTube => _startAddTube;
        public int PlayOnCost => _playOnCost;
        public int AddedTubeCapacity => _addedTubeCapacity;

        /// <summary>A new player's starting values and the lives rules, for the player data.</summary>
        public PlayerRules CreatePlayerRules() =>
            new PlayerRules(_startCoins, _maxLives, LifeRefillInterval, _startUndo, _startShuffle, _startAddTube);

        /// <summary>Coins granted for winning a level of <paramref name="tier"/>.</summary>
        public int GetWinReward(LevelTier tier) =>
            tier switch
            {
                LevelTier.Regular => _regularReward,
                LevelTier.Hard => _hardReward,
                LevelTier.SuperHard => _superHardReward,
                _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown level tier."),
            };
    }
}
