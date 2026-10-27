using UnityEngine;

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
        [SerializeField, Min(0)] private int _startLives = 5;
        [SerializeField, Min(0)] private int _startUndo = 99;
        [SerializeField, Min(0)] private int _startShuffle = 99;
        [SerializeField, Min(0)] private int _startAddTube = 99;

        [Header("Rewards and prices")]
        [Tooltip("Coins granted for winning a level.")]
        [SerializeField, Min(0)] private int _winReward = 20;
        [Tooltip("Coins spent to add one tube when stuck.")]
        [SerializeField, Min(0)] private int _playOnCost = 900;

        [Header("Boosters")]
        [Tooltip("Slots in each tube added by the Add-tube booster or Play on.")]
        [SerializeField, Min(1)] private int _addedTubeCapacity = 4;

        public int StartCoins => _startCoins;
        public int StartLives => _startLives;
        public int StartUndo => _startUndo;
        public int StartShuffle => _startShuffle;
        public int StartAddTube => _startAddTube;
        public int WinReward => _winReward;
        public int PlayOnCost => _playOnCost;
        public int AddedTubeCapacity => _addedTubeCapacity;
    }
}
