namespace BallSort.Core.Session
{
    /// <summary>Why the Add-tube booster cannot be used right now.</summary>
    public enum AddTubeBlockReason
    {
        /// <summary>Add tube is available.</summary>
        None = 0,

        /// <summary>The level is already won; there is nothing left to help with.</summary>
        LevelWon,

        /// <summary>The board is stuck; boosters are disabled until the player plays on or gives up.</summary>
        Stuck,

        /// <summary>The layout is full: the last row holds six tubes and there are already three rows.</summary>
        NoRoom,

        /// <summary>The player has no Add-tube boosters left in their inventory.</summary>
        NoBoostersLeft,
    }
}
