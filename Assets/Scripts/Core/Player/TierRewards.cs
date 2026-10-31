using System;
using BallSort.Core.Levels;

namespace BallSort.Core.Player
{
    /// <summary>Coins a win pays, by the won level's tier (docs/screens.md, Persistent player data).</summary>
    public readonly struct TierRewards
    {
        public int Regular { get; }

        public int Hard { get; }

        public int SuperHard { get; }

        /// <exception cref="ArgumentOutOfRangeException">If any reward is negative.</exception>
        public TierRewards(int regular, int hard, int superHard)
        {
            Regular = RequireNonNegative(regular, nameof(regular));
            Hard = RequireNonNegative(hard, nameof(hard));
            SuperHard = RequireNonNegative(superHard, nameof(superHard));
        }

        /// <summary>The reward for winning a level of <paramref name="tier"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">For an unknown tier.</exception>
        public int For(LevelTier tier) =>
            tier switch
            {
                LevelTier.Regular => Regular,
                LevelTier.Hard => Hard,
                LevelTier.SuperHard => SuperHard,
                _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown level tier."),
            };

        private static int RequireNonNegative(int value, string paramName) =>
            value >= 0 ? value : throw new ArgumentOutOfRangeException(paramName, value, "A reward cannot be negative.");
    }
}
