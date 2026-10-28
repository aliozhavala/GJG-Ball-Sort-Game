namespace BallSort.Game.Flow
{
    /// <summary>States of one visit to the Level scene. See <see cref="LevelFlow"/>.</summary>
    public enum LevelState
    {
        /// <summary>The level is being loaded; nothing is interactive yet.</summary>
        Loading,

        /// <summary>Normal play: taps move balls, boosters work.</summary>
        Playing,

        /// <summary>No legal move: boosters are swapped for Give up / Play on; taps still select.</summary>
        Stuck,

        /// <summary>
        /// A shuffle is being searched for off the main thread: taps and the HUD are locked so the
        /// board it started from stays current. Returns to Playing when it is applied or refused.
        /// </summary>
        Shuffling,

        /// <summary>The level is won: input is locked while the celebration and Win panel play.</summary>
        Won,
    }
}
