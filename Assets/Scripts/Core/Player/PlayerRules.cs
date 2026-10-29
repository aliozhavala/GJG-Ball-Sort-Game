using System;

namespace BallSort.Core.Player
{
    /// <summary>
    /// A new player's starting values. The game reads them from its economy config; tests build their own.
    /// </summary>
    public sealed class PlayerRules
    {
        /// <param name="startCoins">Coins a new player starts with; not negative.</param>
        /// <param name="startLives">Lives a new player starts with; not negative.</param>
        /// <param name="startUndo">Undo boosters a new player starts with; not negative.</param>
        /// <param name="startShuffle">Shuffle boosters a new player starts with; not negative.</param>
        /// <param name="startAddTube">Add-tube boosters a new player starts with; not negative.</param>
        public PlayerRules(int startCoins, int startLives, int startUndo, int startShuffle, int startAddTube)
        {
            StartCoins = RequireNonNegative(startCoins, nameof(startCoins));
            StartLives = RequireNonNegative(startLives, nameof(startLives));
            StartUndo = RequireNonNegative(startUndo, nameof(startUndo));
            StartShuffle = RequireNonNegative(startShuffle, nameof(startShuffle));
            StartAddTube = RequireNonNegative(startAddTube, nameof(startAddTube));
        }

        public int StartCoins { get; }

        public int StartLives { get; }

        public int StartUndo { get; }

        public int StartShuffle { get; }

        public int StartAddTube { get; }

        private static int RequireNonNegative(int value, string name) =>
            value >= 0 ? value : throw new ArgumentOutOfRangeException(name, value, "A starting value cannot be negative.");
    }
}
