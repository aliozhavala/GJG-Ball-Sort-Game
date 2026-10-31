namespace BallSort.Core.Session
{
    /// <summary>Why the Shuffle booster cannot be used, or why a tube was not shuffled.</summary>
    public enum ShuffleBlockReason
    {
        /// <summary>Shuffle is available.</summary>
        None = 0,

        /// <summary>The level is already won; there is nothing left to help with.</summary>
        LevelWon,

        /// <summary>The board is stuck; boosters are disabled until the player plays on or gives up.</summary>
        Stuck,

        /// <summary>The player has no Shuffle boosters left in their inventory.</summary>
        NoBoostersLeft,

        /// <summary>
        /// No tube can be shuffled: every tube is covered, clay, completed, or holds no two balls that
        /// look different (docs/mechanics.md, Shuffle).
        /// </summary>
        NoEligibleTube,

        /// <summary>The tube asked for cannot be shuffled, though another one can. Nothing was changed or spent.</summary>
        TubeNotEligible,
    }
}
