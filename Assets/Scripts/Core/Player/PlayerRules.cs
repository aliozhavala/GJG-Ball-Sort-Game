using System;

namespace BallSort.Core.Player
{
    /// <summary>
    /// A new player's starting values and the lives rules. The game reads them from its economy
    /// config; tests build their own.
    /// </summary>
    public sealed class PlayerRules
    {
        /// <param name="startCoins">Coins a new player starts with; not negative.</param>
        /// <param name="maxLives">Most lives a player can hold; at least one. A new player starts full.</param>
        /// <param name="lifeRefillInterval">Time for one life to come back while below the maximum; positive.</param>
        /// <param name="startUndo">Undo boosters a new player starts with; not negative.</param>
        /// <param name="startShuffle">Shuffle boosters a new player starts with; not negative.</param>
        /// <param name="startAddTube">Add-tube boosters a new player starts with; not negative.</param>
        public PlayerRules(
            int startCoins, int maxLives, TimeSpan lifeRefillInterval, int startUndo, int startShuffle, int startAddTube)
        {
            if (maxLives < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLives), maxLives, "A player must be able to hold a life.");
            }

            if (lifeRefillInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lifeRefillInterval), lifeRefillInterval, "The refill interval must be positive.");
            }

            StartCoins = RequireNonNegative(startCoins, nameof(startCoins));
            MaxLives = maxLives;
            LifeRefillInterval = lifeRefillInterval;
            StartUndo = RequireNonNegative(startUndo, nameof(startUndo));
            StartShuffle = RequireNonNegative(startShuffle, nameof(startShuffle));
            StartAddTube = RequireNonNegative(startAddTube, nameof(startAddTube));
        }

        public int StartCoins { get; }

        /// <summary>Most lives a player can hold; a new player starts with this many.</summary>
        public int MaxLives { get; }

        /// <summary>Time for one life to come back while below <see cref="MaxLives"/>.</summary>
        public TimeSpan LifeRefillInterval { get; }

        public int StartUndo { get; }

        public int StartShuffle { get; }

        public int StartAddTube { get; }

        private static int RequireNonNegative(int value, string name) =>
            value >= 0 ? value : throw new ArgumentOutOfRangeException(name, value, "A starting value cannot be negative.");
    }
}
