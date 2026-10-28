namespace BallSort.Core.Solver
{
    /// <summary>One position in a solver search, linked to the position it was reached from.</summary>
    /// <remarks>
    /// Kept small: the frontier of an optimal search can hold millions. The board is stored packed;
    /// the move is just its tube pair and ball count, and the full <c>MoveInfo</c> list is rebuilt
    /// by replaying the path once a solution is found.
    /// </remarks>
    internal sealed class SearchNode
    {
        public SearchNode(
            byte[] packed,
            SearchNode parent,
            int fromIndex,
            int toIndex,
            int count,
            int depth,
            int estimate,
            int priority,
            bool gainedInformation,
            int sequence)
        {
            Packed = packed;
            Parent = parent;
            FromIndex = (short)fromIndex;
            ToIndex = (short)toIndex;
            Count = (short)count;
            Depth = depth;
            Estimate = estimate;
            Priority = priority;
            GainedInformation = gainedInformation;
            Sequence = sequence;
        }

        /// <summary>The position, in board order (see <see cref="BoardEncoder"/>).</summary>
        public byte[] Packed { get; }

        /// <summary>The position this one was reached from; null for the start.</summary>
        public SearchNode Parent { get; }

        public short FromIndex { get; }

        public short ToIndex { get; }

        public short Count { get; }

        /// <summary>Moves from the start (g).</summary>
        public int Depth { get; }

        /// <summary>The heuristic's lower bound on the moves still needed (h).</summary>
        public int Estimate { get; }

        /// <summary>Heap order: g + h for A*, g + w·h for the fast search.</summary>
        public int Priority { get; }

        /// <summary>
        /// True when the move that led here revealed a ball, found a key or opened a cover. Such a
        /// move cannot be cancelled by moving the balls straight back, so its reversal is not pruned.
        /// </summary>
        public bool GainedInformation { get; }

        /// <summary>Creation order; the final tie-break, so searches are deterministic.</summary>
        public int Sequence { get; }

        /// <summary>Set when a shorter path to the same position was found; the heap then skips this node.</summary>
        public bool IsStale { get; set; }
    }
}
