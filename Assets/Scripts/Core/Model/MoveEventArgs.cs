namespace BallSort.Core.Model
{
    /// <summary>
    /// Describes a move, for both <see cref="BoardState.MoveApplied"/> and <see cref="BoardState.MoveUndone"/>.
    /// </summary>
    /// <remarks>
    /// The indices always describe the move as it was originally made. On
    /// <see cref="BoardState.MoveUndone"/> the balls therefore travelled from <see cref="ToIndex"/>
    /// back to <see cref="FromIndex"/>.
    /// </remarks>
    public readonly struct MoveEventArgs
    {
        /// <summary>Index of the tube the balls were taken from.</summary>
        public int FromIndex { get; }

        /// <summary>Index of the tube the balls were placed into.</summary>
        public int ToIndex { get; }

        /// <summary>How many balls moved. May be fewer than the run's length (partial move).</summary>
        public int Count { get; }

        /// <summary>Shared colour of the moved balls.</summary>
        public int ColorId { get; }

        public MoveEventArgs(int fromIndex, int toIndex, int count, int colorId)
        {
            FromIndex = fromIndex;
            ToIndex = toIndex;
            Count = count;
            ColorId = colorId;
        }

        public override string ToString() => $"{Count}x color {ColorId}: tube {FromIndex} -> tube {ToIndex}";
    }
}
