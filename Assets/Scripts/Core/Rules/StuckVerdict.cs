namespace BallSort.Core.Rules
{
    /// <summary>Why <see cref="StuckDetector"/> decided a position is or is not stuck.</summary>
    public enum StuckVerdict
    {
        /// <summary>The level is won: never stuck.</summary>
        Won,

        /// <summary>No legal move at all: stuck.</summary>
        NoLegalMove,

        /// <summary>
        /// A move leads somewhere the player can never come back from (a reveal, a completion, a key
        /// found, a cover opened, a run lifted off a different colour, or simply no way back): not stuck,
        /// even if it leads to a dead end.
        /// </summary>
        OneWayMove,

        /// <summary>
        /// More positions are reachable than the search looks at: not stuck. Real dead loops are a
        /// handful of positions.
        /// </summary>
        LimitReached,

        /// <summary>
        /// Every reachable position can get back to this one and none is a win: the player can only
        /// go round in circles. Stuck.
        /// </summary>
        Loop,
    }
}
