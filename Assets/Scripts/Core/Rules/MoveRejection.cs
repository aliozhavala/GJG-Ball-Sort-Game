namespace BallSort.Core.Rules
{
    /// <summary>
    /// Why a move was refused. Lets the UI explain an illegal tap instead of only buzzing at it.
    /// </summary>
    public enum MoveRejection
    {
        /// <summary>The move is legal.</summary>
        None = 0,

        /// <summary>Source and target are the same tube.</summary>
        SameTube,

        /// <summary>The source tube holds no balls.</summary>
        SourceEmpty,

        /// <summary>
        /// The source's top ball is hidden, so there is no run to pick up. Unreachable in normal
        /// play — the loader reveals every top ball and each move reveals the next one — so this
        /// signals corrupt level data.
        /// </summary>
        SourceTopHidden,

        /// <summary>The source tube is finished; completed tubes are never a source.</summary>
        SourceCompleted,

        /// <summary>The source tube is locked.</summary>
        SourceLocked,

        /// <summary>The target tube is locked.</summary>
        TargetLocked,

        /// <summary>The target tube has no free slots. A completed tube is always full, so it lands here too.</summary>
        TargetFull,

        /// <summary>
        /// The target's top ball is hidden, so the player cannot see what they would be stacking
        /// onto. Unreachable in normal play for the same reason as <see cref="SourceTopHidden"/> —
        /// balls arriving on a tube are always revealed — so this too signals corrupt level data.
        /// </summary>
        TargetTopHidden,

        /// <summary>The target's top ball is a different colour from the run.</summary>
        ColorMismatch,
    }
}
