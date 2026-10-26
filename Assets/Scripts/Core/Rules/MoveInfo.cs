namespace BallSort.Core.Rules
{
    /// <summary>
    /// A legal move, resolved down to exactly how many balls will travel.
    /// </summary>
    /// <remarks>
    /// Produced by <see cref="MoveRules"/> and consumed by <c>MoveCommand</c>, so the rules are
    /// evaluated once and the command never has to second-guess them.
    /// </remarks>
    public readonly struct MoveInfo
    {
        /// <summary>Index of the tube to take balls from.</summary>
        public int FromIndex { get; }

        /// <summary>Index of the tube to place balls into.</summary>
        public int ToIndex { get; }

        /// <summary>
        /// How many balls will move: <c>min(runLength, target.FreeSpace)</c>. Fewer than the run's
        /// length means a partial move.
        /// </summary>
        public int Count { get; }

        /// <summary>Shared colour of the moving balls.</summary>
        public int ColorId { get; }

        /// <summary>True when the target cannot take the whole run.</summary>
        public bool IsPartial { get; }

        public MoveInfo(int fromIndex, int toIndex, int count, int colorId, bool isPartial)
        {
            FromIndex = fromIndex;
            ToIndex = toIndex;
            Count = count;
            ColorId = colorId;
            IsPartial = isPartial;
        }

        public override string ToString() =>
            $"{Count}x color {ColorId}: tube {FromIndex} -> tube {ToIndex}{(IsPartial ? " (partial)" : string.Empty)}";
    }
}
