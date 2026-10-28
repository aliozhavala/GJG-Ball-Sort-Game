namespace BallSort.Core.Session
{
    /// <summary>Why the Shuffle booster cannot be used, or why a shuffle was not applied.</summary>
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
        /// No valid arrangement was found within the attempt limit (docs/mechanics.md, Shuffle).
        /// Known only after searching; nothing was changed or spent.
        /// </summary>
        NoValidShuffle,

        /// <summary>
        /// The board changed between starting a shuffle search and applying it, so its result no
        /// longer fits. Nothing was changed or spent.
        /// </summary>
        BoardChanged,
    }
}
