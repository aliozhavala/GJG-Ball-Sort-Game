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
        /// The Shuffle booster waits for a tube: eligible tubes are highlighted, the rest dimmed, and the
        /// HUD is locked. The next tap picks (an eligible tube is shuffled) or cancels (anything else,
        /// nothing spent); either way back to Playing.
        /// </summary>
        ShufflePicking,

        /// <summary>The level is won: input is locked while the celebration and Win panel play.</summary>
        Won,
    }
}
