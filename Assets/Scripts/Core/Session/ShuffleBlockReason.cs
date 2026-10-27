namespace BallSort.Core.Session
{
    /// <summary>Why the Shuffle booster cannot be used right now.</summary>
    public enum ShuffleBlockReason
    {
        /// <summary>Shuffle is available. Never returned until the shuffle algorithm exists.</summary>
        None = 0,

        /// <summary>The level is already won; there is nothing left to help with.</summary>
        LevelWon,

        /// <summary>The board is stuck; boosters are disabled until the player plays on or gives up.</summary>
        Stuck,

        /// <summary>
        /// Shuffle is not implemented yet: it needs the solver (roadmap step 4) to guarantee the
        /// shuffled board stays solvable.
        /// </summary>
        NotAvailable,

        /// <summary>The player has no Shuffle boosters left in their inventory.</summary>
        NoBoostersLeft,
    }
}
